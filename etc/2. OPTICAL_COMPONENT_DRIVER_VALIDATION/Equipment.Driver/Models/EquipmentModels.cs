using System.Collections.ObjectModel;

namespace Equipment.Driver;

public enum EN_EQUIPMENT_TYPE
{
    TalonLaser,
    ConexAgpAttenuator,
    MotorizedBeamExpander,
    PowerMaxMeter,
    XpsController,
    Picomotor
}

public enum EN_EQUIPMENT_TRANSPORT { Serial, Tcp, VendorApi }
public enum EN_EQUIPMENT_RISK { ReadOnly, Configuration, Motion, LaserOutput, Persistent }
public enum EN_EQUIPMENT_RESPONSE { Text, Integer, FloatingPoint, Acknowledgement, XpsResult }
public enum EN_EQUIPMENT_CONNECTION { Offline, Connecting, Online, Simulation, Fault }

public sealed record ST_EQUIPMENT_PROFILE(
    EN_EQUIPMENT_TYPE Type,
    string DisplayName,
    string Role,
    string Manual,
    EN_EQUIPMENT_TRANSPORT Transport,
    string DefaultEndpoint,
    int DefaultBaudRate,
    int DefaultTcpPort,
    string TxTerminator,
    string Theory,
    string Operation,
    string ImportantNotes,
    string VerificationLimit);

public sealed record ST_EQUIPMENT_COMMAND_SPEC(
    EN_EQUIPMENT_TYPE Equipment,
    string Id,
    string DisplayName,
    string Category,
    string Template,
    bool ExpectsResponse,
    EN_EQUIPMENT_RESPONSE ResponseKind,
    EN_EQUIPMENT_RISK Risk,
    double? Minimum,
    double? Maximum,
    string Unit,
    string Description,
    double? DefaultParameter = null)
{
    public bool RequiresParameter => Template.Contains("{0", StringComparison.Ordinal);
}

public sealed record ST_EQUIPMENT_SAFETY(
    bool HardwareWriteUnlocked,
    bool MotionAreaClear,
    bool LaserPathSafe,
    bool OperatorConfirmed)
{
    public bool Allows(EN_EQUIPMENT_RISK risk) => risk switch
    {
        EN_EQUIPMENT_RISK.ReadOnly => true,
        EN_EQUIPMENT_RISK.LaserOutput => HardwareWriteUnlocked && LaserPathSafe && OperatorConfirmed,
        _ => HardwareWriteUnlocked && MotionAreaClear && OperatorConfirmed
    };
}

public sealed record ST_EQUIPMENT_TRANSACTION(
    DateTimeOffset Timestamp,
    string Equipment,
    string Command,
    string Response,
    bool Success,
    double ElapsedMs,
    string Message);

public sealed record ST_EQUIPMENT_VALIDATION(
    string Equipment,
    string Item,
    string Command,
    bool Passed,
    string Actual,
    string Expected,
    string Detail);

public sealed record ST_EQUIPMENT_STATUS_FIELD(string Name, string Value, string Unit, DateTimeOffset UpdatedAt);

public sealed class CEquipmentTrace : ObservableCollection<ST_EQUIPMENT_TRANSACTION>
{
    public void AddBounded(ST_EQUIPMENT_TRANSACTION item, int capacity = 1500)
    {
        Insert(0, item);
        while (Count > capacity) RemoveAt(Count - 1);
    }
}
