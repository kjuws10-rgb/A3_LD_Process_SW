using System.IO.Ports;

namespace Equipment.Driver;

public sealed class CSerialEquipmentTransport(ST_EQUIPMENT_PROFILE profile, string portName, int baudRate, int timeoutMs) : IEquipmentTransport
{
    private SerialPort? _port;
    public EN_EQUIPMENT_CONNECTION ConnectionState { get; private set; }
    public string Endpoint => $"{portName}@{baudRate}";

    public Task Connect(CancellationToken cancellationToken = default)
    {
        ConnectionState = EN_EQUIPMENT_CONNECTION.Connecting;
        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = timeoutMs, WriteTimeout = timeoutMs,
            Handshake = profile.Type == EN_EQUIPMENT_TYPE.ConexAgpAttenuator ? Handshake.XOnXOff : Handshake.None
        };
        _port.Open();
        ConnectionState = EN_EQUIPMENT_CONNECTION.Online;
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        _port?.Close(); _port?.Dispose(); _port = null;
        ConnectionState = EN_EQUIPMENT_CONNECTION.Offline;
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default)
    {
        if (_port is null || !_port.IsOpen) await Connect(cancellationToken);
        return await Task.Run(() =>
        {
            _port!.DiscardInBuffer();
            _port.Write(command + profile.TxTerminator);
            if (!expectResponse) return "SENT";

            var buffer = new List<char>();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ch = (char)_port.ReadChar();
                if (ch is '\r' or '\n') { if (buffer.Count > 0) break; continue; }
                buffer.Add(ch);
                if (ch == '!' || buffer.Count > 8 && new string(buffer.ToArray()).Contains("EndOfAPI", StringComparison.Ordinal)) break;
            }
            return new string(buffer.ToArray());
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync() => await Disconnect();
}
