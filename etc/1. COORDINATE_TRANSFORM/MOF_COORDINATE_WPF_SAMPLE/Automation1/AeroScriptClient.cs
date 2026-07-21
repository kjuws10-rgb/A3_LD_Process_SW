using System.IO;
using System.Net.Sockets;
using System.Text.Json;

namespace MofCoordinateDemo.Automation1;

public sealed class AeroScriptClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _apiKey;

    public AeroScriptClient(string host, int port, string apiKey)
    {
        _host = string.IsNullOrWhiteSpace(host) ? throw new ArgumentException("Server host is required.", nameof(host)) : host;
        _port = port is > 0 and <= 65535 ? port : throw new ArgumentOutOfRangeException(nameof(port));
        if (_port == AeroScriptEndpointRules.Automation1ControllerPort)
        {
            throw new ArgumentException(
                $"TCP {_port}은 Automation1 Controller native endpoint이며 AeroScript Gateway protocol port가 아닙니다. " +
                $"Server PC에서 Automation1Server를 실행하고 TCP {AeroScriptEndpointRules.DefaultGatewayPort}으로 접속하십시오.",
                nameof(port));
        }
        _apiKey = apiKey ?? "";
    }

    public Task<ScriptServerResponse> UploadAsync(AeroScriptPackage package, CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Upload(_apiKey, package), cancellationToken);

    public Task<ScriptServerResponse> HealthCheckAsync(CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Health(_apiKey), cancellationToken);

    public Task<ScriptServerResponse> RunAsync(string jobId, CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Run(_apiKey, jobId), cancellationToken);

    public Task<ScriptServerResponse> GetStatusAsync(string jobId, CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Status(_apiKey, jobId), cancellationToken);

    private async Task<ScriptServerResponse> SendAsync(ScriptServerRequest request, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(_host, _port, cancellationToken);
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException(
                $"{_host}:{_port} 연결 실패({ex.SocketErrorCode}). Ping 성공은 TCP {_port} 포트가 열린 것을 의미하지 않습니다. " +
                "Server PC에서 이 예제의 Automation1Server를 0.0.0.0:46100으로 실행하고 Windows 방화벽 인바운드 규칙을 확인하십시오. " +
                "MDK 설치와 라이선스 인증만으로 이 사용자 정의 TCP Server가 자동 실행되지는 않습니다.",
                ex);
        }
        ScriptServerResponse response;
        try
        {
            await using var stream = client.GetStream();
            await AeroScriptProtocol.WriteAsync(stream, request, cancellationToken);
            response = await AeroScriptProtocol.ReadAsync<ScriptServerResponse>(stream, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or SocketException or JsonException)
        {
            throw new InvalidOperationException(
                $"{_host}:{_port} TCP 접속은 성공했지만 AeroScript Gateway protocol v{ScriptServerRequest.CurrentProtocolVersion} 응답을 받지 못했습니다. " +
                $"다른 서비스 또는 Automation1 Controller native endpoint에 접속했거나, Server PC의 Automation1Server가 구버전일 수 있습니다. " +
                $"Client는 Gateway TCP {AeroScriptEndpointRules.DefaultGatewayPort}을 사용하고 Client/Server를 같은 배포본으로 업데이트하십시오.",
                ex);
        }

        if (response.ProtocolVersion != ScriptServerRequest.CurrentProtocolVersion)
        {
            throw new InvalidOperationException(
                $"Client protocol v{ScriptServerRequest.CurrentProtocolVersion}과 Server protocol v{response.ProtocolVersion}이 다릅니다. " +
                "Client와 Server를 같은 downloadable 배포본으로 업데이트하십시오.");
        }

        return response;
    }
}
