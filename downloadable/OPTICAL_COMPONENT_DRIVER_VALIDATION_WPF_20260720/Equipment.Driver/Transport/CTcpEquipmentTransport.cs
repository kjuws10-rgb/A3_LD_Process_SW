using System.Net.Sockets;
using System.Text;

namespace Equipment.Driver;

public sealed class CTcpEquipmentTransport(string host, int port, int timeoutMs) : IEquipmentTransport
{
    private TcpClient? _client;
    public EN_EQUIPMENT_CONNECTION ConnectionState { get; private set; }
    public string Endpoint => $"{host}:{port}";

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        ConnectionState = EN_EQUIPMENT_CONNECTION.Connecting;
        _client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        await _client.ConnectAsync(host, port, timeout.Token);
        ConnectionState = EN_EQUIPMENT_CONNECTION.Online;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        _client?.Dispose(); _client = null;
        ConnectionState = EN_EQUIPMENT_CONNECTION.Offline;
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default)
    {
        if (_client is null || !_client.Connected) await Connect(cancellationToken);
        var stream = _client!.GetStream();
        var bytes = Encoding.ASCII.GetBytes(command);
        await stream.WriteAsync(bytes, cancellationToken);
        if (!expectResponse) return "SENT";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        do
        {
            var read = await stream.ReadAsync(buffer, timeout.Token);
            if (read == 0) break;
            builder.Append(Encoding.ASCII.GetString(buffer, 0, read));
        } while (!builder.ToString().Contains("EndOfAPI", StringComparison.Ordinal));
        return builder.ToString().Trim();
    }

    public async ValueTask DisposeAsync() => await Disconnect();
}
