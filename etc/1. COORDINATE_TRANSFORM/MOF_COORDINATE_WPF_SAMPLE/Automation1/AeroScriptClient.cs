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

    public Task<ScriptServerResponse> RunAsync(string jobId, CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Run(_apiKey, jobId), cancellationToken);

    public Task<ScriptServerResponse> GetStatusAsync(string jobId, CancellationToken cancellationToken) =>
        SendAsync(ScriptServerRequest.Status(_apiKey, jobId), cancellationToken);

    private async Task<ScriptServerResponse> SendAsync(ScriptServerRequest request, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port, cancellationToken);
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
