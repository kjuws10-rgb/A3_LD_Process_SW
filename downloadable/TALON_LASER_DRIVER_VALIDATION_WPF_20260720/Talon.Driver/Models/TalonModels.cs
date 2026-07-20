using System.Collections.ObjectModel;

namespace Talon.Driver;

public enum EN_TALON_COMMAND
{
    QueryIdentity,
    QueryBaudRate,
    SetBaudRate,
    QuerySystemStatus,
    QueryStatusHistory,
    QueryStatusByte,
    QueryDiodeEmission,
    TurnEmissionOn,
    TurnEmissionOff,
    QueryDiodeCurrent,
    QueryCommandedCurrent,
    QueryDiodeCurrentLimit,
    SetDiodeCurrent,
    QueryRepetitionRate,
    SetRepetitionRate,
    QueryExternalPrf,
    SetExternalPrf,
    QueryQMode,
    SetQMode,
    QueryOutputPower,
    QueryDiodeTemperature,
    QueryTowerTemperature,
    QueryChassisTemperature,
    QueryWarmupTime,
    QueryShutter,
    SetShutter,
    QueryGate,
    SetGate,
    QueryExternalGate,
    SetExternalGate,
    QueryShg,
    SetShg,
    QueryShgAutotune,
    SetShgAutotune,
    QueryThgSpot,
    SetThgSpot,
    QueryThgSpotHours,
    QueryHeadHours,
    SaveConfiguration
}

public enum EN_TALON_RESPONSE_KIND
{
    Acknowledgement,
    Integer,
    FloatingPoint,
    Boolean,
    OpenClosed,
    Text,
    CsvIntegers,
    Identity,
    StatusByte
}

public enum EN_TALON_RISK_LEVEL
{
    ReadOnly,
    Configuration,
    LaserOutput,
    Persistent
}

public enum EN_TALON_CONNECTION_STATE
{
    Offline,
    Connecting,
    Online,
    Simulation,
    Fault
}

public sealed record ST_TALON_COMMAND_SPEC(
    EN_TALON_COMMAND Command,
    string DisplayName,
    string Category,
    string QueryText,
    string SetTemplate,
    EN_TALON_RESPONSE_KIND ResponseKind,
    EN_TALON_RISK_LEVEL RiskLevel,
    double? Minimum,
    double? Maximum,
    string Unit,
    string Description,
    bool RequiresParameter = false);

public sealed record ST_TALON_STATUS_BITS(
    int RawValue,
    bool Emission,
    bool ShutterOpen,
    bool GateOpen,
    bool ShgWarming,
    bool ExternalGate,
    bool SystemFault,
    bool ShgAutotune,
    bool ThgAutotune,
    bool MotorMoving)
{
    public static ST_TALON_STATUS_BITS FromRaw(int raw)
    {
        return new ST_TALON_STATUS_BITS(
            raw,
            IsSet(raw, 0),
            IsSet(raw, 1),
            IsSet(raw, 2),
            IsSet(raw, 3),
            IsSet(raw, 4),
            IsSet(raw, 5),
            IsSet(raw, 6),
            IsSet(raw, 7),
            IsSet(raw, 9));
    }

    private static bool IsSet(int value, int bit) => (value & (1 << bit)) != 0;
}

public sealed record ST_TALON_STATUS(
    string Identity,
    string SystemStatus,
    IReadOnlyList<int> StatusHistory,
    ST_TALON_STATUS_BITS StatusBits,
    int BaudRate,
    bool Emission,
    bool ShutterOpen,
    bool GateOpen,
    bool ExternalGateEnabled,
    double DiodeCurrentA,
    double CommandedCurrentA,
    double DiodeCurrentLimitA,
    int RepetitionRateHz,
    int ExternalPrfHz,
    int QMode,
    double OutputPowerW,
    double DiodeTemperatureC,
    double TowerTemperatureC,
    double ChassisTemperatureC,
    int WarmupRemainingSeconds,
    int ThgSpot,
    double ThgSpotHours,
    string LastError,
    DateTimeOffset UpdatedAt)
{
    public static ST_TALON_STATUS Empty { get; } = new(
        "", "Unknown", [], ST_TALON_STATUS_BITS.FromRaw(0), 115200,
        false, false, false, false, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, "", DateTimeOffset.MinValue);
}

public sealed record ST_TALON_TRANSACTION(
    DateTimeOffset Timestamp,
    string Direction,
    string Command,
    string Response,
    bool Success,
    TimeSpan Elapsed,
    string Message);

public sealed record ST_TALON_VALIDATION_ITEM(
    string Name,
    string Command,
    bool Passed,
    string Actual,
    string Expected,
    string Detail);

public sealed record ST_TALON_SAFETY_CONTEXT(
    bool HardwareOutputUnlocked,
    bool AreaInterlockConfirmed,
    bool BeamPathConfirmed,
    bool OperatorConfirmed)
{
    public bool AllowsLaserOutput =>
        HardwareOutputUnlocked &&
        AreaInterlockConfirmed &&
        BeamPathConfirmed &&
        OperatorConfirmed;
}

public sealed class CTalonObservableLog : ObservableCollection<ST_TALON_TRANSACTION>
{
    public void AddBounded(ST_TALON_TRANSACTION transaction, int capacity = 1000)
    {
        Insert(0, transaction);

        while (Count > capacity)
        {
            RemoveAt(Count - 1);
        }
    }
}
