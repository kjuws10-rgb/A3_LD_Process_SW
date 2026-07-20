using System.Globalization;

namespace Equipment.Driver;

public sealed class CSimulatorEquipmentTransport(ST_EQUIPMENT_PROFILE profile) : IEquipmentTransport
{
    private readonly Dictionary<string, double> _position = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attenuator"] = 12.5, ["motor1"] = 1600, ["motor2"] = 1750,
        ["xps"] = 25.0, ["picomotor"] = 1000
    };

    public EN_EQUIPMENT_CONNECTION ConnectionState { get; private set; }
    public string Endpoint => $"SIM:{profile.DisplayName}";
    public bool InjectTimeoutOnce { get; set; }
    public bool InjectInvalidResponseOnce { get; set; }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionState = EN_EQUIPMENT_CONNECTION.Simulation;
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        ConnectionState = EN_EQUIPMENT_CONNECTION.Offline;
        return Task.CompletedTask;
    }

    public async Task<string> Exchange(string command, bool expectResponse, CancellationToken cancellationToken = default)
    {
        if (ConnectionState == EN_EQUIPMENT_CONNECTION.Offline) await Connect(cancellationToken);
        await Task.Delay(20, cancellationToken);
        if (InjectTimeoutOnce) { InjectTimeoutOnce = false; throw new TimeoutException("요청된 단발성 simulator timeout입니다."); }
        if (InjectInvalidResponseOnce) { InjectInvalidResponseOnce = false; return "INVALID"; }

        ApplyMotion(command);
        return BuildResponse(command, expectResponse);
    }

    public ValueTask DisposeAsync()
    {
        ConnectionState = EN_EQUIPMENT_CONNECTION.Offline;
        return ValueTask.CompletedTask;
    }

    private string BuildResponse(string command, bool expectResponse)
    {
        if (!expectResponse) return "SENT";
        return profile.Type switch
        {
            EN_EQUIPMENT_TYPE.TalonLaser => command switch
            {
                "*IDN?" => "Spectra Physics,Talon 355,SIM-0001,0100-01.02.0003",
                "?F" => "SYSTEM READY", "?FH" => "000;000;000;000;000;000;000;000;000;000;000;000;000;000;000;000",
                "?P" => "0.000", "?T1" => "23.25", "?C1" => "0.00", _ => "OK"
            },
            EN_EQUIPMENT_TYPE.ConexAgpAttenuator => command switch
            {
                "1VE?" => "1VE CONEX-AGP SIM 1.0", "1TP?" => $"1TP{F(_position["attenuator"])}",
                "1TH?" => $"1TH{F(_position["attenuator"])}", "1TS?" => "1TS000032", _ => "OK"
            },
            EN_EQUIPMENT_TYPE.MotorizedBeamExpander => command switch
            {
                "#7:" => $"$7:{F0(_position["motor1"])}", "#8:" => $"$8:{F0(_position["motor2"])}", _ => "!"
            },
            EN_EQUIPMENT_TYPE.PowerMaxMeter => command switch
            {
                "*ind" => "PowerMax-USB/RS SIM", "sn?" => "PM-SIM-0001", "wv?" => "3.55E-7",
                "pw?" => "1.204E+0", "pos" => "0.031,-0.024", _ => "OK"
            },
            EN_EQUIPMENT_TYPE.XpsController => command switch
            {
                var x when x.StartsWith("FirmwareVersionGet", StringComparison.Ordinal) => "0,XPS-SIM-1.0,EndOfAPI",
                var x when x.StartsWith("ControllerStatusGet", StringComparison.Ordinal) => "0,0,EndOfAPI",
                var x when x.StartsWith("GroupStatusGet", StringComparison.Ordinal) => "0,12,EndOfAPI",
                var x when x.StartsWith("GroupPositionCurrentGet", StringComparison.Ordinal) => $"0,{F6(_position["xps"])},EndOfAPI",
                _ => "0,EndOfAPI"
            },
            EN_EQUIPMENT_TYPE.Picomotor => command switch
            {
                var x when x.Contains("DiscoverDevices", StringComparison.Ordinal) => "USB:8742-SIM-0001",
                var x when x.Contains("IdentifyInstrument", StringComparison.Ordinal) => "8742,SIM-0001,2.0.2,2026-07-20",
                var x when x.Contains("GetPosition", StringComparison.Ordinal) => F0(_position["picomotor"]),
                var x when x.Contains("GetMotionDone", StringComparison.Ordinal) => "1",
                var x when x.Contains("GetError", StringComparison.Ordinal) => "0,NO ERROR", _ => "OK"
            },
            _ => "OK"
        };
    }

    private void ApplyMotion(string command)
    {
        if (TryRead(command, "1PA", out var pa)) _position["attenuator"] = pa;
        if (TryRead(command, "1PR", out var pr)) _position["attenuator"] += pr;
        if (TryRead(command, "#1:", out var m1)) _position["motor1"] = m1;
        if (TryRead(command, "#2:", out var m2)) _position["motor2"] = m2;
        if (TryBetween(command, "GroupMoveAbsolute(Group1,", ")", out var xa)) _position["xps"] = xa;
        if (TryBetween(command, "GroupMoveRelative(Group1,", ")", out var xr)) _position["xps"] += xr;
        if (TryBetween(command, "CmdLib.RelativeMove(deviceKey,address,1,", ")", out var pm)) _position["picomotor"] += pm;
    }

    private static bool TryRead(string text, string prefix, out double value) =>
        double.TryParse(text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? text[prefix.Length..] : "", NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    private static bool TryBetween(string text, string prefix, string suffix, out double value) =>
        double.TryParse(text.StartsWith(prefix, StringComparison.Ordinal) && text.EndsWith(suffix, StringComparison.Ordinal) ? text[prefix.Length..^suffix.Length] : "", NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    private static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);
    private static string F0(double value) => value.ToString("F0", CultureInfo.InvariantCulture);
    private static string F6(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
}
