using System.Diagnostics;
using System.Globalization;

namespace Talon.Driver;

public sealed class CTalonDriver(ITalonTransport transport) : IAsyncDisposable
{
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public event EventHandler<ST_TALON_TRANSACTION>? TransactionCompleted;
    public ST_TALON_STATUS Status { get; private set; } = ST_TALON_STATUS.Empty;
    public EN_TALON_CONNECTION_STATE ConnectionState => transport.ConnectionState;
    public string Endpoint => transport.Endpoint;
    public bool IsSimulation => transport is CTalonSimulatorTransport;

    public Task Connect(CancellationToken cancellationToken = default) => transport.Connect(cancellationToken);
    public Task Disconnect(CancellationToken cancellationToken = default) => transport.Disconnect(cancellationToken);

    public async Task<string> Execute(
        EN_TALON_COMMAND command,
        double? parameter = null,
        ST_TALON_SAFETY_CONTEXT? safety = null,
        CancellationToken cancellationToken = default)
    {
        var spec = CTalonCommandCatalog.Get(command);
        var text = CTalonProtocol.Build(command, parameter);
        ValidateSafety(spec, command, parameter, safety);

        await _commandLock.WaitAsync(cancellationToken);
        var timer = Stopwatch.StartNew();
        try
        {
            var expectResponse = !string.IsNullOrWhiteSpace(spec.QueryText);
            var raw = await transport.Exchange(text, expectResponse, cancellationToken);
            var response = expectResponse ? CTalonProtocol.NormalizeResponse(text, raw) : raw;

            if (expectResponse && (string.IsNullOrWhiteSpace(response) || response.Equals("ERR", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"Talon이 유효하지 않은 응답을 반환했습니다: '{response}'");
            }

            Status = Apply(command, parameter, response, Status) with
            {
                LastError = "",
                UpdatedAt = DateTimeOffset.Now
            };
            Publish(text, response, true, timer.Elapsed, "정상");
            return response;
        }
        catch (Exception ex)
        {
            Status = Status with { LastError = ex.Message, UpdatedAt = DateTimeOffset.Now };
            Publish(text, "", false, timer.Elapsed, ex.Message);
            throw;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<ST_TALON_STATUS> PollSafeStatus(CancellationToken cancellationToken = default)
    {
        EN_TALON_COMMAND[] commands =
        [
            EN_TALON_COMMAND.QueryIdentity,
            EN_TALON_COMMAND.QuerySystemStatus,
            EN_TALON_COMMAND.QueryStatusHistory,
            EN_TALON_COMMAND.QueryStatusByte,
            EN_TALON_COMMAND.QueryDiodeEmission,
            EN_TALON_COMMAND.QueryDiodeCurrent,
            EN_TALON_COMMAND.QueryCommandedCurrent,
            EN_TALON_COMMAND.QueryDiodeCurrentLimit,
            EN_TALON_COMMAND.QueryRepetitionRate,
            EN_TALON_COMMAND.QueryExternalPrf,
            EN_TALON_COMMAND.QueryQMode,
            EN_TALON_COMMAND.QueryOutputPower,
            EN_TALON_COMMAND.QueryDiodeTemperature,
            EN_TALON_COMMAND.QueryTowerTemperature,
            EN_TALON_COMMAND.QueryChassisTemperature,
            EN_TALON_COMMAND.QueryWarmupTime,
            EN_TALON_COMMAND.QueryShutter,
            EN_TALON_COMMAND.QueryGate,
            EN_TALON_COMMAND.QueryExternalGate,
            EN_TALON_COMMAND.QueryThgSpot,
            EN_TALON_COMMAND.QueryThgSpotHours
        ];

        foreach (var command in commands)
        {
            await Execute(command, cancellationToken: cancellationToken);
        }

        return Status;
    }

    public async Task<IReadOnlyList<ST_TALON_VALIDATION_ITEM>> RunReadOnlyValidation(
        CancellationToken cancellationToken = default)
    {
        var results = new List<ST_TALON_VALIDATION_ITEM>();
        EN_TALON_COMMAND[] commands =
        [
            EN_TALON_COMMAND.QueryIdentity,
            EN_TALON_COMMAND.QueryBaudRate,
            EN_TALON_COMMAND.QuerySystemStatus,
            EN_TALON_COMMAND.QueryStatusHistory,
            EN_TALON_COMMAND.QueryStatusByte,
            EN_TALON_COMMAND.QueryDiodeCurrentLimit,
            EN_TALON_COMMAND.QueryDiodeTemperature,
            EN_TALON_COMMAND.QueryTowerTemperature,
            EN_TALON_COMMAND.QueryChassisTemperature,
            EN_TALON_COMMAND.QueryWarmupTime,
            EN_TALON_COMMAND.QueryShutter,
            EN_TALON_COMMAND.QueryGate,
            EN_TALON_COMMAND.QueryThgSpot,
            EN_TALON_COMMAND.QueryThgSpotHours
        ];

        foreach (var command in commands)
        {
            var spec = CTalonCommandCatalog.Get(command);
            var text = CTalonProtocol.Build(command);
            try
            {
                var response = await Execute(command, cancellationToken: cancellationToken);
                results.Add(new ST_TALON_VALIDATION_ITEM(
                    spec.DisplayName,
                    text,
                    true,
                    response,
                    Expected(spec.ResponseKind),
                    "응답 수신 및 형식 해석 정상"));
            }
            catch (Exception ex)
            {
                results.Add(new ST_TALON_VALIDATION_ITEM(
                    spec.DisplayName,
                    text,
                    false,
                    ex.Message,
                    Expected(spec.ResponseKind),
                    "통신 또는 응답 형식 확인 필요"));
            }
        }

        return results;
    }

    public async ValueTask DisposeAsync()
    {
        await transport.DisposeAsync();
        _commandLock.Dispose();
    }

    private static ST_TALON_STATUS Apply(
        EN_TALON_COMMAND command,
        double? parameter,
        string response,
        ST_TALON_STATUS current)
    {
        return command switch
        {
            EN_TALON_COMMAND.QueryIdentity => current with { Identity = response },
            EN_TALON_COMMAND.QueryBaudRate => current with { BaudRate = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.SetBaudRate => current with { BaudRate = (int)(parameter ?? current.BaudRate) },
            EN_TALON_COMMAND.QuerySystemStatus => current with { SystemStatus = response },
            EN_TALON_COMMAND.QueryStatusHistory => current with { StatusHistory = CTalonProtocol.ReadStatusHistory(response) },
            EN_TALON_COMMAND.QueryStatusByte => current with { StatusBits = ST_TALON_STATUS_BITS.FromRaw(CTalonProtocol.ReadInt(response)) },
            EN_TALON_COMMAND.QueryDiodeEmission => current with { Emission = CTalonProtocol.ReadBoolean(response) },
            EN_TALON_COMMAND.TurnEmissionOn => current with { Emission = true },
            EN_TALON_COMMAND.TurnEmissionOff => current with { Emission = false, DiodeCurrentA = 0 },
            EN_TALON_COMMAND.QueryDiodeCurrent => current with { DiodeCurrentA = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryCommandedCurrent => current with { CommandedCurrentA = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryDiodeCurrentLimit => current with { DiodeCurrentLimitA = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.SetDiodeCurrent => current with { CommandedCurrentA = parameter ?? current.CommandedCurrentA },
            EN_TALON_COMMAND.QueryRepetitionRate => current with { RepetitionRateHz = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.SetRepetitionRate => current with { RepetitionRateHz = (int)(parameter ?? current.RepetitionRateHz) },
            EN_TALON_COMMAND.QueryExternalPrf => current with { ExternalPrfHz = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.SetExternalPrf => current with { ExternalPrfHz = (int)(parameter ?? current.ExternalPrfHz) },
            EN_TALON_COMMAND.QueryQMode => current with { QMode = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.SetQMode => current with { QMode = (int)(parameter ?? current.QMode) },
            EN_TALON_COMMAND.QueryOutputPower => current with { OutputPowerW = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryDiodeTemperature => current with { DiodeTemperatureC = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryTowerTemperature => current with { TowerTemperatureC = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryChassisTemperature => current with { ChassisTemperatureC = CTalonProtocol.ReadDouble(response) },
            EN_TALON_COMMAND.QueryWarmupTime => current with { WarmupRemainingSeconds = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.QueryShutter => current with { ShutterOpen = CTalonProtocol.ReadBoolean(response) },
            EN_TALON_COMMAND.SetShutter => current with { ShutterOpen = parameter > 0 },
            EN_TALON_COMMAND.QueryGate => current with { GateOpen = CTalonProtocol.ReadBoolean(response) },
            EN_TALON_COMMAND.SetGate => current with { GateOpen = parameter > 0 },
            EN_TALON_COMMAND.QueryExternalGate => current with { ExternalGateEnabled = CTalonProtocol.ReadBoolean(response) },
            EN_TALON_COMMAND.SetExternalGate => current with { ExternalGateEnabled = parameter > 0 },
            EN_TALON_COMMAND.QueryThgSpot => current with { ThgSpot = CTalonProtocol.ReadInt(response) },
            EN_TALON_COMMAND.SetThgSpot => current with { ThgSpot = (int)(parameter ?? current.ThgSpot) },
            EN_TALON_COMMAND.QueryThgSpotHours => current with { ThgSpotHours = CTalonProtocol.ReadDouble(response) },
            _ => current
        };
    }

    private void ValidateSafety(
        ST_TALON_COMMAND_SPEC spec,
        EN_TALON_COMMAND command,
        double? parameter,
        ST_TALON_SAFETY_CONTEXT? safety)
    {
        if (IsSimulation || spec.RiskLevel == EN_TALON_RISK_LEVEL.ReadOnly || IsOutputReducingCommand(command, parameter))
        {
            return;
        }

        if (spec.RiskLevel == EN_TALON_RISK_LEVEL.LaserOutput && safety?.AllowsLaserOutput != true)
        {
            throw new InvalidOperationException("Hardware 출력 명령은 안전 잠금, Area Interlock, Beam Path, 작업자 확인이 모두 필요합니다.");
        }

        if (spec.RiskLevel is EN_TALON_RISK_LEVEL.Configuration or EN_TALON_RISK_LEVEL.Persistent &&
            safety?.HardwareOutputUnlocked != true)
        {
            throw new InvalidOperationException("Hardware 설정 변경 명령은 설정 잠금 해제가 필요합니다.");
        }

        if (command == EN_TALON_COMMAND.TurnEmissionOn &&
            !Status.SystemStatus.Contains("READY", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Emission ON 전에 ?F 응답이 SYSTEM READY인지 확인해야 합니다.");
        }
    }

    private static bool IsOutputReducingCommand(EN_TALON_COMMAND command, double? parameter)
    {
        return command == EN_TALON_COMMAND.TurnEmissionOff ||
            command == EN_TALON_COMMAND.SetShutter && parameter <= 0 ||
            command == EN_TALON_COMMAND.SetGate && parameter <= 0 ||
            command == EN_TALON_COMMAND.SetExternalGate && parameter <= 0 ||
            command == EN_TALON_COMMAND.SetDiodeCurrent && parameter <= 0;
    }

    private static string Expected(EN_TALON_RESPONSE_KIND kind) => kind switch
    {
        EN_TALON_RESPONSE_KIND.Integer => "정수",
        EN_TALON_RESPONSE_KIND.FloatingPoint => "실수",
        EN_TALON_RESPONSE_KIND.Boolean => "0/1",
        EN_TALON_RESPONSE_KIND.OpenClosed => "OPEN/CLOSED",
        EN_TALON_RESPONSE_KIND.CsvIntegers => "세미콜론 구분 Event Code",
        EN_TALON_RESPONSE_KIND.Identity => "제조사,모델,시리얼,버전",
        EN_TALON_RESPONSE_KIND.StatusByte => "상태 비트 정수",
        _ => "ASCII 응답"
    };

    private void Publish(string command, string response, bool success, TimeSpan elapsed, string message)
    {
        TransactionCompleted?.Invoke(this, new ST_TALON_TRANSACTION(
            DateTimeOffset.Now,
            "TX/RX",
            command,
            response,
            success,
            elapsed,
            message));
    }
}
