using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace MofCoordinateDemo.Automation1;

public sealed class AeroScriptServer : IAsyncDisposable
{
    private readonly ScriptServerOptions _options;
    private readonly IAutomation1Runtime _runtime;
    private readonly ConcurrentDictionary<string, ServerJob> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _lastStatusLogKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _taskExecutionGate = new(1, 1);
    private TcpListener? _listener;

    public AeroScriptServer(ScriptServerOptions options, IAutomation1Runtime runtime)
    {
        _options = options;
        _runtime = runtime;
        Directory.CreateDirectory(_options.SpoolDirectory);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var address = IPAddress.TryParse(_options.BindAddress, out var parsed)
            ? parsed
            : (await Dns.GetHostAddressesAsync(_options.BindAddress, cancellationToken)).First();

        _listener = new TcpListener(address, _options.Port);
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal server shutdown.
        }
        finally
        {
            _listener.Stop();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();
        _taskExecutionGate.Dispose();
        await _runtime.DisposeAsync();
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
    {
        var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        using (client)
        await using (var stream = client.GetStream())
        {
            ScriptServerRequest? request = null;
            ScriptServerResponse response;
            try
            {
                request = await AeroScriptProtocol.ReadAsync<ScriptServerRequest>(stream, serverCancellationToken);
                response = await DispatchAsync(request, serverCancellationToken);
            }
            catch (Exception ex)
            {
                request ??= new ScriptServerRequest(
                    ScriptServerRequest.CurrentProtocolVersion,
                    ScriptRequestType.GetStatus,
                    "invalid",
                    "",
                    null,
                    null);
                response = ScriptServerResponse.Error(request, "SERVER_ERROR", ex.Message);
            }

            var jobState = response.Job?.State.ToString() ?? "-";
            if (ShouldLogResponse(request, response))
            {
                Console.WriteLine(
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} " +
                    $"Remote={remoteEndpoint}, Request={request.RequestType}, Job={request.JobId ?? "-"}, " +
                    $"Success={response.Success}, State={jobState}, Code={response.ErrorCode}, Message={response.Message}");
            }
            await AeroScriptProtocol.WriteAsync(stream, response, serverCancellationToken);
        }
    }

    private bool ShouldLogResponse(ScriptServerRequest request, ScriptServerResponse response)
    {
        if (request.RequestType != ScriptRequestType.GetStatus || response.Job is null)
        {
            return true;
        }

        var key = $"{response.Success}|{response.Job.State}|{response.Job.Message}|{response.ErrorCode}";
        if (_lastStatusLogKeys.TryGetValue(response.Job.JobId, out var previous) &&
            previous.Equals(key, StringComparison.Ordinal))
        {
            return false;
        }

        _lastStatusLogKeys[response.Job.JobId] = key;
        return true;
    }

    private async Task<ScriptServerResponse> DispatchAsync(
        ScriptServerRequest request,
        CancellationToken serverCancellationToken)
    {
        if (request.ProtocolVersion != ScriptServerRequest.CurrentProtocolVersion)
        {
            return ScriptServerResponse.Error(request, "PROTOCOL_VERSION", "지원하지 않는 프로토콜 버전입니다.");
        }

        if (!ApiKeyMatches(request.ApiKey))
        {
            return ScriptServerResponse.Error(request, "UNAUTHORIZED", "API key가 일치하지 않습니다.");
        }

        return request.RequestType switch
        {
            ScriptRequestType.HealthCheck => await HealthCheckAsync(request, serverCancellationToken),
            ScriptRequestType.UploadScript => await UploadAsync(request, serverCancellationToken),
            ScriptRequestType.RunScript => Run(request, serverCancellationToken),
            ScriptRequestType.GetStatus => GetStatus(request),
            _ => ScriptServerResponse.Error(request, "UNKNOWN_REQUEST", "알 수 없는 요청입니다.")
        };
    }

    private async Task<ScriptServerResponse> HealthCheckAsync(
        ScriptServerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var runtimeHealth = await _runtime.CheckHealthAsync(cancellationToken);
            return ScriptServerResponse.Ok(
                request,
                $"Gateway ready. Bind={_options.BindAddress}:{_options.Port}, ModePolicy={_options.ModePolicy}, " +
                $"MaxScriptBytes={_options.MaxScriptBytes}. {runtimeHealth}",
                null);
        }
        catch (Exception ex)
        {
            return ScriptServerResponse.Error(
                request,
                "RUNTIME_NOT_READY",
                $"Gateway는 연결되었지만 Automation1 runtime 점검에 실패했습니다: {ex.Message}");
        }
    }

    private async Task<ScriptServerResponse> UploadAsync(
        ScriptServerRequest request,
        CancellationToken cancellationToken)
    {
        var package = request.Package;
        if (package is null || package.JobId != request.JobId)
        {
            return ScriptServerResponse.Error(request, "INVALID_PACKAGE", "Job ID 또는 script package가 올바르지 않습니다.");
        }

        if (!Guid.TryParseExact(package.JobId, "N", out _))
        {
            return ScriptServerResponse.Error(request, "INVALID_JOB_ID", "Job ID 형식이 올바르지 않습니다.");
        }

        if (package.TaskIndex <= 0 || !IsSafeControllerFileName(package.ControllerFileName))
        {
            return ScriptServerResponse.Error(request, "INVALID_TARGET", "Task index 또는 controller file 이름이 올바르지 않습니다.");
        }

        if (!IsGenerationModeAllowed(package.GenerationMode))
        {
            return ScriptServerResponse.Error(
                request,
                "MODE_POLICY_REJECTED",
                $"Server policy {_options.ModePolicy}에서 {package.GenerationMode} Job은 허용되지 않습니다.");
        }

        var scriptBytes = Encoding.UTF8.GetBytes(package.ScriptText);
        if (scriptBytes.Length == 0 || scriptBytes.Length > _options.MaxScriptBytes)
        {
            return ScriptServerResponse.Error(request, "SCRIPT_SIZE", "Script 크기가 허용 범위를 벗어났습니다.");
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(scriptBytes));
        if (!actualHash.Equals(package.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return ScriptServerResponse.Error(request, "HASH_MISMATCH", "전송된 Script의 SHA-256이 일치하지 않습니다.");
        }

        var spoolPath = Path.Combine(_options.SpoolDirectory, $"{package.JobId}.ascript");
        await File.WriteAllTextAsync(spoolPath, package.ScriptText, new UTF8Encoding(false), cancellationToken);

        var job = new ServerJob(package, ScriptJobState.Uploaded, "Server PC에 script 저장 완료");
        if (!_jobs.TryAdd(package.JobId, job))
        {
            return ScriptServerResponse.Error(request, "DUPLICATE_JOB", "동일한 Job ID가 이미 존재합니다.");
        }

        return ScriptServerResponse.Ok(request, "Script upload 완료", job.Snapshot());
    }

    private ScriptServerResponse Run(ScriptServerRequest request, CancellationToken serverCancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.JobId) || !_jobs.TryGetValue(request.JobId, out var job))
        {
            return ScriptServerResponse.Error(request, "JOB_NOT_FOUND", "실행할 Job을 찾을 수 없습니다.");
        }

        if (!job.TryQueue())
        {
            return ScriptServerResponse.Error(request, "INVALID_JOB_STATE", "현재 상태에서는 Job을 실행할 수 없습니다.", job.Snapshot());
        }

        _ = ExecuteJobAsync(job, serverCancellationToken);
        return ScriptServerResponse.Ok(request, "실행 명령 수신 완료", job.Snapshot());
    }

    private ScriptServerResponse GetStatus(ScriptServerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobId) || !_jobs.TryGetValue(request.JobId, out var job))
        {
            return ScriptServerResponse.Error(request, "JOB_NOT_FOUND", "조회할 Job을 찾을 수 없습니다.");
        }

        return ScriptServerResponse.Ok(request, "Job 상태 조회 완료", job.Snapshot());
    }

    private async Task ExecuteJobAsync(ServerJob job, CancellationToken serverCancellationToken)
    {
        var gateEntered = false;
        try
        {
            await _taskExecutionGate.WaitAsync(serverCancellationToken);
            gateEntered = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
            timeout.CancelAfter(_options.ExecutionTimeout);

            await _runtime.ExecuteAsync(
                job.Package,
                (state, message) =>
                {
                    job.Update(state, message);
                    return ValueTask.CompletedTask;
                },
                timeout.Token);

            job.Update(ScriptJobState.Completed, "Automation1 AeroScript 실행 완료");
        }
        catch (OperationCanceledException)
        {
            job.Update(ScriptJobState.Failed, "실행 제한시간 초과 또는 서버 종료로 작업이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            job.Update(ScriptJobState.Failed, ex.Message);
        }
        finally
        {
            if (gateEntered)
            {
                _taskExecutionGate.Release();
            }
        }
    }

    private bool ApiKeyMatches(string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(_options.ApiKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied ?? "");
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private bool IsGenerationModeAllowed(AeroScriptGenerationMode mode) => _options.ModePolicy switch
    {
        AeroScriptModePolicy.Any => true,
        AeroScriptModePolicy.VirtualOnly => mode == AeroScriptGenerationMode.VirtualWaitSimulation,
        AeroScriptModePolicy.HardwareOnly => mode == AeroScriptGenerationMode.HardwareCoordinateProgram,
        _ => false
    };

    private static bool IsSafeControllerFileName(string fileName)
    {
        return fileName.EndsWith(".ascript", StringComparison.OrdinalIgnoreCase) &&
               !fileName.Contains("..", StringComparison.Ordinal) &&
               !fileName.Contains('\\') &&
               fileName.All(character => !char.IsControl(character));
    }

    private sealed class ServerJob
    {
        private readonly object _sync = new();
        private ScriptJobState _state;
        private string _message;
        private DateTimeOffset _updatedAtUtc;

        public ServerJob(AeroScriptPackage package, ScriptJobState state, string message)
        {
            Package = package;
            _state = state;
            _message = message;
            _updatedAtUtc = DateTimeOffset.UtcNow;
        }

        public AeroScriptPackage Package { get; }

        public bool TryQueue()
        {
            lock (_sync)
            {
                if (_state != ScriptJobState.Uploaded)
                {
                    return false;
                }

                _state = ScriptJobState.Queued;
                _message = "Automation1 실행 대기";
                _updatedAtUtc = DateTimeOffset.UtcNow;
                return true;
            }
        }

        public void Update(ScriptJobState state, string message)
        {
            lock (_sync)
            {
                _state = state;
                _message = message;
                _updatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        public ScriptJobStatus Snapshot()
        {
            lock (_sync)
            {
                return new ScriptJobStatus(
                    Package.JobId,
                    _state,
                    _message,
                    Package.ControllerFileName,
                    Package.TaskIndex,
                    Package.GenerationMode,
                    Package.Sha256,
                    _updatedAtUtc);
            }
        }
    }
}
