using System.Text.Json;
using Aerotech.Automation1.DotNet;

namespace MofCoordinateDemo.Automation1;

/// <summary>
/// Uses the official Automation1 .NET API to connect directly to a remote controller.
/// No custom TCP gateway or server-side helper application is involved.
/// </summary>
public sealed class Automation1DirectClient : IAsyncDisposable
{
    private readonly Automation1ConnectionOptions _options;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private Controller? _controller;
    private AeroScriptPackage? _activePackage;
    private Automation1DirectStatus? _lastStatus;
    private Automation1JobAudit? _audit;
    private DateTimeOffset? _runRequestedAtUtc;
    private bool _disposed;

    public Automation1ConnectionInfo? LastConnectionInfo { get; private set; }

    public Automation1DirectClient(Automation1ConnectionOptions options)
    {
        _options = options;
        ValidateOptions(options);
    }

    public Task<Automation1ConnectionInfo> ConnectAsync(CancellationToken cancellationToken) =>
        ExecuteLockedAsync(ConnectCore, cancellationToken);

    public Task<Automation1ConnectionInfo> HealthCheckAsync(CancellationToken cancellationToken) =>
        ConnectAsync(cancellationToken);

    public Task<Automation1DirectStatus> UploadAsync(
        AeroScriptPackage package,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        return ExecuteLockedAsync(() => UploadCore(package), cancellationToken);
    }

    public Task<Automation1DirectStatus> RunAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => RunCore(jobId), cancellationToken);

    public Task<Automation1DirectStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => GetStatusCore(jobId), cancellationToken);

    public Task<Automation1DirectStatus> StopAsync(string jobId, CancellationToken cancellationToken) =>
        ExecuteLockedAsync(() => StopCore(jobId), cancellationToken);

    private async Task<T> ExecuteLockedAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(operation, cancellationToken);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private Automation1ConnectionInfo ConnectCore()
    {
        ThrowIfDisposed();
        DisconnectCore();
        _controller = _options.Mode switch
        {
            Automation1ConnectionMode.NoAuthentication =>
                Controller.Connect(_options.Host, _options.Port),
            Automation1ConnectionMode.UserPassword =>
                Controller.Connect(_options.Host, _options.Port, _options.UserName, _options.Password),
            Automation1ConnectionMode.SecureCertificate =>
                Controller.ConnectSecure(_options.Host, _options.ExpectedCertificate),
            Automation1ConnectionMode.SecureUserPassword =>
                Controller.ConnectSecure(
                    _options.Host,
                    _options.UserName,
                    _options.Password,
                    _options.ExpectedCertificate),
            _ => throw new ArgumentOutOfRangeException(nameof(_options.Mode))
        };

        if (!_controller.IsRunning && _options.StartControllerIfStopped)
        {
            _controller.Start();
        }

        var isRunning = _controller.IsRunning;
        var taskCount = isRunning ? _controller.Runtime.Tasks.Count : 0;
        var host = _controller.Information.Host ?? _options.Host;
        var port = _controller.Information.Port;
        var encrypted = _controller.Information.IsConnectionEncrypted;
        var apiVersion = typeof(Controller).Assembly.GetName().Version?.ToString() ?? "unknown";
        LastConnectionInfo = new Automation1ConnectionInfo(
            host,
            port,
            isRunning,
            encrypted,
            taskCount,
            apiVersion,
            $"Direct Automation1 connection ready: {host}:{port}, Running={isRunning}, " +
            $"Encrypted={encrypted}, Tasks={taskCount}, API={apiVersion}");
        return LastConnectionInfo;
    }

    private Automation1DirectStatus UploadCore(AeroScriptPackage package)
    {
        var controller = EnsureRunningController();
        ValidateTask(controller, package.TaskIndex);
        controller.Files.WriteText(package.ControllerFileName, package.ScriptText);

        _activePackage = package;
        _runRequestedAtUtc = null;
        var controllerDirectory = package.ControllerFileName.LastIndexOf('/') is var slashIndex && slashIndex >= 0
            ? package.ControllerFileName[..(slashIndex + 1)]
            : "";
        var auditFile =
            $"{controllerDirectory}mof_job_{package.CreatedAtUtc:yyyyMMdd_HHmmss}_{package.JobId[..8]}.json";
        _audit = Automation1JobAudit.Create(package, _options.Host, _options.Port, auditFile);
        _lastStatus = CreateStatus(
            package,
            Automation1DirectState.Uploaded,
            "ProgramReady",
            $"Controller.Files.WriteText completed: {package.ControllerFileName}",
            "",
            auditFile);
        AppendAuditEvent(_lastStatus);
        return _lastStatus;
    }

    private Automation1DirectStatus RunCore(string jobId)
    {
        var package = EnsureActivePackage(jobId);
        var controller = EnsureRunningController();
        var task = controller.Runtime.Tasks[package.TaskIndex];
        var before = task.Status;
        if (IsBusyTaskState(before.TaskState))
        {
            throw new InvalidOperationException(
                $"Task {package.TaskIndex} is already busy: {before.TaskState}.");
        }

        _runRequestedAtUtc = DateTimeOffset.UtcNow;
        task.Program.Run(package.ControllerFileName);
        return RefreshTaskStatus(controller, package);
    }

    private Automation1DirectStatus GetStatusCore(string jobId)
    {
        var package = EnsureActivePackage(jobId);
        if (_runRequestedAtUtc is null)
        {
            return _lastStatus
                   ?? throw new InvalidOperationException("No uploaded script status is available.");
        }

        return RefreshTaskStatus(EnsureRunningController(), package);
    }

    private Automation1DirectStatus StopCore(string jobId)
    {
        var package = EnsureActivePackage(jobId);
        var controller = EnsureRunningController();
        var task = controller.Runtime.Tasks[package.TaskIndex];
        if (IsBusyTaskState(task.Status.TaskState))
        {
            task.Program.Stop(5000);
        }

        _runRequestedAtUtc = null;
        var stopped = CreateStatus(
            package,
            Automation1DirectState.Stopped,
            task.Status.TaskState.ToString(),
            $"Task {package.TaskIndex} Program.Stop completed.",
            "",
            _audit?.ControllerAuditFileName ?? "");
        UpdateStatusAndAudit(stopped);
        return stopped;
    }

    private Automation1DirectStatus RefreshTaskStatus(Controller controller, AeroScriptPackage package)
    {
        var snapshot = controller.Runtime.Tasks[package.TaskIndex].Status;
        var taskState = snapshot.TaskState.ToString();
        var error = snapshot.Error?.ToString() ?? "";
        var inRunTransition = _runRequestedAtUtc is not null &&
                              DateTimeOffset.UtcNow - _runRequestedAtUtc.Value < TimeSpan.FromSeconds(2);
        var state = snapshot.TaskState switch
        {
            TaskState.ProgramComplete => Automation1DirectState.Completed,
            TaskState.Error => Automation1DirectState.Failed,
            TaskState.ProgramRunning or TaskState.ProgramFeedhold or TaskState.ProgramPaused =>
                Automation1DirectState.Running,
            _ when inRunTransition => Automation1DirectState.Running,
            _ => Automation1DirectState.Failed
        };
        var message = state switch
        {
            Automation1DirectState.Completed =>
                $"Task {package.TaskIndex} reached ProgramComplete.",
            Automation1DirectState.Failed when snapshot.TaskState == TaskState.Error =>
                $"Task {package.TaskIndex} error: {error}",
            Automation1DirectState.Failed =>
                $"Task {package.TaskIndex} changed to unexpected state {taskState} before completion.",
            _ =>
                $"Task {package.TaskIndex} state={taskState}, source={snapshot.AeroScriptSourceFileName}"
        };
        var status = CreateStatus(
            package,
            state,
            taskState,
            message,
            error,
            _audit?.ControllerAuditFileName ?? "");
        UpdateStatusAndAudit(status);
        return status;
    }

    private void UpdateStatusAndAudit(Automation1DirectStatus status)
    {
        var changed = _lastStatus is null ||
                      _lastStatus.State != status.State ||
                      !_lastStatus.TaskState.Equals(status.TaskState, StringComparison.Ordinal) ||
                      !_lastStatus.Message.Equals(status.Message, StringComparison.Ordinal);
        _lastStatus = status;
        if (changed)
        {
            AppendAuditEvent(status);
        }
    }

    private void AppendAuditEvent(Automation1DirectStatus status)
    {
        var controller = _controller ?? throw new InvalidOperationException("Automation1 is not connected.");
        var audit = _audit ?? throw new InvalidOperationException("The job audit has not been created.");
        audit.UpdatedAtUtc = status.UpdatedAtUtc;
        audit.FinalState = status.State.ToString();
        audit.FinalTaskState = status.TaskState;
        audit.FinalMessage = status.Message;
        audit.FinalError = status.Error;
        audit.Events.Add(new Automation1AuditEvent(
            status.UpdatedAtUtc,
            status.State.ToString(),
            status.TaskState,
            status.Message,
            status.Error));
        controller.Files.WriteText(
            audit.ControllerAuditFileName,
            JsonSerializer.Serialize(audit, AuditJsonOptions));
    }

    private Controller EnsureRunningController()
    {
        ThrowIfDisposed();
        var controller = _controller
                         ?? throw new InvalidOperationException("Connect directly to the Automation1 controller first.");
        if (!controller.IsRunning)
        {
            throw new InvalidOperationException("The Automation1 controller is not running.");
        }

        return controller;
    }

    private AeroScriptPackage EnsureActivePackage(string jobId)
    {
        var package = _activePackage
                      ?? throw new InvalidOperationException("Write a script to the Controller File System first.");
        if (!package.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The requested job does not match the uploaded job.");
        }

        return package;
    }

    private static void ValidateTask(Controller controller, int taskIndex)
    {
        var count = controller.Runtime.Tasks.Count;
        if (taskIndex <= 0 || taskIndex > count)
        {
            throw new InvalidOperationException(
                $"Task index {taskIndex} is outside the controller task range 1..{count}.");
        }
    }

    private static bool IsBusyTaskState(TaskState state) =>
        state is TaskState.ProgramRunning or
            TaskState.ProgramFeedhold or
            TaskState.ProgramPaused or
            TaskState.QueueRunning or
            TaskState.QueuePaused;

    private static Automation1DirectStatus CreateStatus(
        AeroScriptPackage package,
        Automation1DirectState state,
        string taskState,
        string message,
        string error,
        string auditFile) =>
        new(
            package.JobId,
            state,
            taskState,
            message,
            error,
            package.ControllerFileName,
            auditFile,
            package.TaskIndex,
            DateTimeOffset.UtcNow);

    private static void ValidateOptions(Automation1ConnectionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Host);
        if (options.Port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Port));
        }

        if ((options.Mode is Automation1ConnectionMode.UserPassword or
             Automation1ConnectionMode.SecureUserPassword) &&
            (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrEmpty(options.Password)))
        {
            throw new InvalidOperationException("User name and password are required for authenticated connections.");
        }

        if ((options.Mode is Automation1ConnectionMode.SecureCertificate or
             Automation1ConnectionMode.SecureUserPassword) &&
            string.IsNullOrWhiteSpace(options.ExpectedCertificate))
        {
            throw new InvalidOperationException("The expected controller certificate is required for secure connections.");
        }
    }

    private void DisconnectCore()
    {
        if (_controller is null)
        {
            return;
        }

        try
        {
            _controller.Disconnect();
        }
        finally
        {
            _controller = null;
            _activePackage = null;
            _lastStatus = null;
            _audit = null;
            _runRequestedAtUtc = null;
            LastConnectionInfo = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _operationGate.WaitAsync();
        try
        {
            DisconnectCore();
            _disposed = true;
        }
        finally
        {
            _operationGate.Release();
            _operationGate.Dispose();
        }
    }

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        WriteIndented = true
    };
}

public sealed class Automation1JobAudit
{
    public required string JobId { get; init; }
    public required string ControllerHost { get; init; }
    public required int ControllerPort { get; init; }
    public required string ControllerFileName { get; init; }
    public required string ControllerAuditFileName { get; init; }
    public required int TaskIndex { get; init; }
    public required int TargetCount { get; init; }
    public required string GenerationMode { get; init; }
    public required string Sha256 { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string FinalState { get; set; } = "";
    public string FinalTaskState { get; set; } = "";
    public string FinalMessage { get; set; } = "";
    public string FinalError { get; set; } = "";
    public List<Automation1AuditEvent> Events { get; } = new();

    public static Automation1JobAudit Create(
        AeroScriptPackage package,
        string controllerHost,
        int controllerPort,
        string auditFile) =>
        new()
        {
            JobId = package.JobId,
            ControllerHost = controllerHost,
            ControllerPort = controllerPort,
            ControllerFileName = package.ControllerFileName,
            ControllerAuditFileName = auditFile,
            TaskIndex = package.TaskIndex,
            TargetCount = package.TargetCount,
            GenerationMode = package.GenerationMode.ToString(),
            Sha256 = package.Sha256,
            CreatedAtUtc = package.CreatedAtUtc,
            UpdatedAtUtc = package.CreatedAtUtc
        };
}

public sealed record Automation1AuditEvent(
    DateTimeOffset TimestampUtc,
    string State,
    string TaskState,
    string Message,
    string Error);
