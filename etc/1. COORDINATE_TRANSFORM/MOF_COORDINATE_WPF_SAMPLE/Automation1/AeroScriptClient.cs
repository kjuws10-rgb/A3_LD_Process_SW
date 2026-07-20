using System.IO;
using System.Net.Sockets;

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
        await using var stream = client.GetStream();
        await AeroScriptProtocol.WriteAsync(stream, request, cancellationToken);
        var response = await AeroScriptProtocol.ReadAsync<ScriptServerResponse>(stream, cancellationToken);

        if (response.ProtocolVersion != ScriptServerRequest.CurrentProtocolVersion)
        {
            throw new InvalidDataException($"Unsupported response protocol version: {response.ProtocolVersion}.");
        }

        return response;
    }
}
