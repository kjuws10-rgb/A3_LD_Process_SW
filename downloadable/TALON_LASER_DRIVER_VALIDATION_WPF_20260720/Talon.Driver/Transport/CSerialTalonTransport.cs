using System.IO.Ports;
using System.Text;

namespace Talon.Driver;

public sealed class CSerialTalonTransport(
    string portName,
    int baudRate = 115200,
    int timeoutMs = 1500) : ITalonTransport
{
    private readonly SemaphoreSlim _exchangeLock = new(1, 1);
    private SerialPort? _serialPort;

    public EN_TALON_CONNECTION_STATE ConnectionState { get; private set; } = EN_TALON_CONNECTION_STATE.Offline;
    public string Endpoint => $"{portName}:{baudRate}:N:8:1";

    public Task Connect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionState = EN_TALON_CONNECTION_STATE.Connecting;
        ClosePort();

        try
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                Encoding = Encoding.ASCII,
                ReadTimeout = timeoutMs,
                WriteTimeout = timeoutMs,
                DtrEnable = false,
                RtsEnable = false
            };
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            ConnectionState = EN_TALON_CONNECTION_STATE.Online;
        }
        catch
        {
            ClosePort();
            ConnectionState = EN_TALON_CONNECTION_STATE.Fault;
            throw;
        }

        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClosePort();
        ConnectionState = EN_TALON_CONNECTION_STATE.Offline;
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(
        string command,
        bool expectResponse,
        CancellationToken cancellationToken = default)
    {
        await _exchangeLock.WaitAsync(cancellationToken);
        try
        {
            if (_serialPort is null || !_serialPort.IsOpen)
            {
                await Connect(cancellationToken);
            }

            var port = _serialPort ?? throw new InvalidOperationException("Serial port가 열리지 않았습니다.");
            port.DiscardInBuffer();
            port.Write(command + CTalonProtocol.CommandTerminator);

            // Talon set/action commands do not return an acknowledgement in the manual examples.
            if (!expectResponse)
            {
                await port.BaseStream.FlushAsync(cancellationToken);
                ConnectionState = EN_TALON_CONNECTION_STATE.Online;
                return "SENT";
            }

            // Talon replies may terminate with CR, LF, or CR/LF depending on ECHO setting.
            var buffer = new byte[1];
            var response = new StringBuilder();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(timeoutMs);

            while (true)
            {
                var count = await port.BaseStream.ReadAsync(buffer, timeout.Token);
                if (count == 0)
                {
                    continue;
                }

                if (buffer[0] is 0x0D or 0x0A)
                {
                    if (response.Length > 0)
                    {
                        break;
                    }
                    continue;
                }

                response.Append((char)buffer[0]);
            }

            ConnectionState = EN_TALON_CONNECTION_STATE.Online;
            return response.ToString().Trim();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ConnectionState = EN_TALON_CONNECTION_STATE.Fault;
            throw new TimeoutException($"Talon 응답 제한시간 {timeoutMs} ms를 초과했습니다.");
        }
        finally
        {
            _exchangeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Disconnect();
        _exchangeLock.Dispose();
    }

    private void ClosePort()
    {
        if (_serialPort is null)
        {
            return;
        }

        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        finally
        {
            _serialPort.Dispose();
            _serialPort = null;
        }
    }
}
