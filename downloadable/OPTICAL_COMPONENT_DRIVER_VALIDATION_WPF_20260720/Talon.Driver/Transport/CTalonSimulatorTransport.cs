using System.Globalization;

namespace Talon.Driver;

public sealed class CTalonSimulatorTransport : ITalonTransport
{
    private readonly Dictionary<string, string> _state = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BAUDRATE"] = "115200",
        ["C1"] = "0.00",
        ["CS1"] = "5.80",
        ["DCL1"] = "6.50",
        ["Q"] = "100000",
        ["EPRF"] = "100000",
        ["QMODE"] = "0",
        ["D"] = "0",
        ["SHT"] = "0",
        ["G"] = "CLOSED",
        ["GEXT"] = "1",
        ["SHG"] = "29100",
        ["SAUTO"] = "0",
        ["MTR:TSPOT"] = "2"
    };

    public EN_TALON_CONNECTION_STATE ConnectionState { get; private set; } = EN_TALON_CONNECTION_STATE.Offline;
    public string Endpoint => "SIM:TALON-355";
    public bool InjectTimeoutOnce { get; set; }
    public bool InjectInvalidResponseOnce { get; set; }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionState = EN_TALON_CONNECTION_STATE.Simulation;
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionState = EN_TALON_CONNECTION_STATE.Offline;
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(
        string command,
        bool expectResponse,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ConnectionState == EN_TALON_CONNECTION_STATE.Offline)
        {
            await Connect(cancellationToken);
        }

        await Task.Delay(35, cancellationToken);

        if (InjectTimeoutOnce)
        {
            InjectTimeoutOnce = false;
            throw new TimeoutException("SIM: 요청된 단발성 Timeout입니다.");
        }

        if (InjectInvalidResponseOnce)
        {
            InjectInvalidResponseOnce = false;
            return "INVALID_RESPONSE";
        }

        var normalized = command.Trim().ToUpperInvariant();
        if (normalized.StartsWith('?') || normalized.EndsWith('?'))
        {
            return Query(normalized);
        }

        if (normalized == "ON")
        {
            _state["D"] = "1";
            return expectResponse ? "OK" : "SENT";
        }

        if (normalized == "OFF")
        {
            _state["D"] = "0";
            _state["C1"] = "0.00";
            return expectResponse ? "OK" : "SENT";
        }

        if (normalized == "SAVE")
        {
            return expectResponse ? "OK" : "SENT";
        }

        var separator = normalized.IndexOf(':');
        if (separator <= 0)
        {
            return "ERR";
        }

        var key = normalized[..separator];
        var value = normalized[(separator + 1)..];
        if (key == "MTR" && normalized.StartsWith("MTR:TSPOT:", StringComparison.Ordinal))
        {
            key = "MTR:TSPOT";
            value = normalized["MTR:TSPOT:".Length..];
        }

        _state[key] = key switch
        {
            "G" => value == "1" ? "OPEN" : "CLOSED",
            "C1" => double.Parse(value, CultureInfo.InvariantCulture).ToString("F2", CultureInfo.InvariantCulture),
            _ => value
        };
        return expectResponse ? "OK" : "SENT";
    }

    public ValueTask DisposeAsync()
    {
        ConnectionState = EN_TALON_CONNECTION_STATE.Offline;
        return ValueTask.CompletedTask;
    }

    private string Query(string command)
    {
        return command switch
        {
            "*IDN?" => "Spectra Physics,Talon 355,SIM-0001,0100-01.02.0003",
            "?F" => "SYSTEM READY",
            "?FH" => "000;000;000;000;000;000;000;000;000;000;000;000;000;000;000;000",
            "*STB?" => BuildStatusByte().ToString(CultureInfo.InvariantCulture),
            "?P" => _state["D"] == "1" ? "10.250" : "0.000",
            "?T" or "?T1" => "23.25",
            "?TT" => "23.10",
            "?CT" => "24.54",
            "?WARMUPTIME" => "0",
            "?MTR:THR" => "128.5",
            "?HEADHRS" => "1175.20",
            "QMODE?" => _state["QMODE"],
            _ when command.StartsWith('?') && _state.TryGetValue(command[1..], out var value) => value,
            _ => "ERR"
        };
    }

    private int BuildStatusByte()
    {
        var value = 0;
        if (_state["D"] == "1") value |= 1 << 0;
        if (_state["SHT"] == "1") value |= 1 << 1;
        if (_state["G"] == "OPEN") value |= 1 << 2;
        if (_state["GEXT"] == "1") value |= 1 << 4;
        if (_state["SAUTO"] == "1") value |= 1 << 6;
        return value;
    }
}
