namespace Talon.Driver;

public static class CTalonCommandCatalog
{
    private static readonly IReadOnlyDictionary<EN_TALON_COMMAND, ST_TALON_COMMAND_SPEC> Specs =
        CreateSpecs().ToDictionary(spec => spec.Command);

    public static IReadOnlyList<ST_TALON_COMMAND_SPEC> All { get; } =
        Specs.Values.OrderBy(spec => spec.Category).ThenBy(spec => spec.DisplayName).ToArray();

    public static ST_TALON_COMMAND_SPEC Get(EN_TALON_COMMAND command) => Specs[command];

    private static IEnumerable<ST_TALON_COMMAND_SPEC> CreateSpecs()
    {
        yield return Read(EN_TALON_COMMAND.QueryIdentity, "장비 식별", "상태", "*IDN?", EN_TALON_RESPONSE_KIND.Identity, "제조사, 모델, 시리얼, 소프트웨어 버전을 조회합니다.");
        yield return Read(EN_TALON_COMMAND.QueryBaudRate, "통신 속도", "통신", "?BAUDRATE", EN_TALON_RESPONSE_KIND.Integer, "현재 RS-232 Baud Rate를 조회합니다.", "bps");
        yield return Set(EN_TALON_COMMAND.SetBaudRate, "통신 속도 설정", "통신", "BAUDRATE:{0}", 9600, 115200, "bps", "허용값은 9600, 19200, 38400, 57600, 115200입니다.");
        yield return Read(EN_TALON_COMMAND.QuerySystemStatus, "현재 시스템 상태", "상태", "?F", EN_TALON_RESPONSE_KIND.Text, "현재 Event String을 조회합니다.");
        yield return Read(EN_TALON_COMMAND.QueryStatusHistory, "상태 이력 16개", "상태", "?FH", EN_TALON_RESPONSE_KIND.CsvIntegers, "최신 순서의 세미콜론 구분 Event Code 이력을 조회합니다.");
        yield return Read(EN_TALON_COMMAND.QueryStatusByte, "상태 비트", "상태", "*STB?", EN_TALON_RESPONSE_KIND.StatusByte, "Emission, shutter, gate, fault, autotune, motor 상태 비트를 조회합니다.");
        yield return Read(EN_TALON_COMMAND.QueryDiodeEmission, "Emission 상태", "상태", "?D", EN_TALON_RESPONSE_KIND.Boolean, "레이저 다이오드 Emission 상태를 조회합니다.");
        yield return Action(EN_TALON_COMMAND.TurnEmissionOn, "Emission ON", "출력", "ON", EN_TALON_RISK_LEVEL.LaserOutput, "Event 00/System Ready이고 모든 안전조건이 만족될 때만 실행해야 합니다.");
        yield return Action(EN_TALON_COMMAND.TurnEmissionOff, "Emission OFF", "출력", "OFF", EN_TALON_RISK_LEVEL.LaserOutput, "다이오드 전류와 Emission을 끕니다.");
        yield return Read(EN_TALON_COMMAND.QueryDiodeCurrent, "실측 다이오드 전류", "다이오드", "?C1", EN_TALON_RESPONSE_KIND.FloatingPoint, "현재 측정된 다이오드 전류입니다.", "A");
        yield return Read(EN_TALON_COMMAND.QueryCommandedCurrent, "설정 다이오드 전류", "다이오드", "?CS1", EN_TALON_RESPONSE_KIND.FloatingPoint, "마지막으로 명령한 다이오드 전류입니다.", "A");
        yield return Read(EN_TALON_COMMAND.QueryDiodeCurrentLimit, "다이오드 전류 제한", "다이오드", "?DCL1", EN_TALON_RESPONSE_KIND.FloatingPoint, "공장 설정 다이오드 전류 상한입니다.", "A");
        yield return Set(EN_TALON_COMMAND.SetDiodeCurrent, "다이오드 전류 설정", "다이오드", "C1:{0:F2}", 0, 100, "A", "실제 상한은 ?DCL1 결과를 사용해야 합니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryRepetitionRate, "Q-Switch 반복률", "펄스", "?Q", EN_TALON_RESPONSE_KIND.Integer, "내부 Q-Switch PRF를 조회합니다.", "Hz");
        yield return Set(EN_TALON_COMMAND.SetRepetitionRate, "Q-Switch 반복률 설정", "펄스", "Q:{0}", 0, 2000000, "Hz", "0은 외부 Trigger 입력을 활성화합니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryExternalPrf, "EPRF", "펄스", "?EPRF", EN_TALON_RESPONSE_KIND.Integer, "Energy limiting이 시작되는 외부 PRF 하한을 조회합니다.", "Hz");
        yield return Set(EN_TALON_COMMAND.SetExternalPrf, "EPRF 설정", "펄스", "EPRF:{0}", 0, 500000, "Hz", "모델별 최솟값을 확인해야 하며 SAVE 전까지 재부팅 후 복원되지 않습니다.");
        yield return Read(EN_TALON_COMMAND.QueryQMode, "Q Mode", "펄스", "QMODE?", EN_TALON_RESPONSE_KIND.Integer, "0=Normal, 1=CW, 2=특수 Timing 조건입니다.");
        yield return Set(EN_TALON_COMMAND.SetQMode, "Q Mode 설정", "펄스", "QMODE:{0}", 0, 2, "mode", "QMODE 2는 최대 50 kHz 조건을 지켜야 합니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryOutputPower, "내부 출력 Power", "상태", "?P", EN_TALON_RESPONSE_KIND.FloatingPoint, "내부 Power Monitor가 측정한 레이저 출력입니다.", "W");
        yield return Read(EN_TALON_COMMAND.QueryDiodeTemperature, "다이오드 온도", "온도", "?T1", EN_TALON_RESPONSE_KIND.FloatingPoint, "다이오드 실제 온도입니다.", "°C");
        yield return Read(EN_TALON_COMMAND.QueryTowerTemperature, "Tower 온도", "온도", "?TT", EN_TALON_RESPONSE_KIND.FloatingPoint, "Crystal tower 온도이며 Event 36과 연관됩니다.", "°C");
        yield return Read(EN_TALON_COMMAND.QueryChassisTemperature, "Chassis 온도", "온도", "?CT", EN_TALON_RESPONSE_KIND.FloatingPoint, "레이저 Head chassis 온도입니다.", "°C");
        yield return Read(EN_TALON_COMMAND.QueryWarmupTime, "Warm-up 잔여시간", "온도", "?WARMUPTIME", EN_TALON_RESPONSE_KIND.Integer, "Harmonic crystal warm-up 잔여시간입니다.", "s");
        yield return Read(EN_TALON_COMMAND.QueryShutter, "Shutter 상태", "출력", "?SHT", EN_TALON_RESPONSE_KIND.Boolean, "현재 shutter 위치를 조회합니다.");
        yield return Set(EN_TALON_COMMAND.SetShutter, "Shutter 열기/닫기", "출력", "SHT:{0}", 0, 1, "0/1", "0=Closed, 1=Open입니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryGate, "Gate 상태", "출력", "?G", EN_TALON_RESPONSE_KIND.OpenClosed, "현재 serial gate 상태를 조회합니다.");
        yield return Set(EN_TALON_COMMAND.SetGate, "Gate 열기/닫기", "출력", "G:{0}", 0, 1, "0/1", "Serial gate 0은 외부 gate 설정보다 우선하여 pulse를 차단합니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryExternalGate, "외부 Gate Enable", "출력", "?GEXT", EN_TALON_RESPONSE_KIND.Boolean, "Analog port pin 7 외부 gate 사용 여부를 조회합니다.");
        yield return Set(EN_TALON_COMMAND.SetExternalGate, "외부 Gate Enable 설정", "출력", "GEXT:{0}", 0, 1, "0/1", "0=Disable, 1=Enable입니다.", EN_TALON_RISK_LEVEL.LaserOutput);
        yield return Read(EN_TALON_COMMAND.QueryShg, "SHG 현재값", "Harmonic", "?SHG", EN_TALON_RESPONSE_KIND.Integer, "SHG oven 현재 readback count입니다.", "count");
        yield return Set(EN_TALON_COMMAND.SetShg, "SHG 설정", "Harmonic", "SHG:{0}", 20000, 65535, "count", "자동 저장되는 crystal oven 설정입니다.", EN_TALON_RISK_LEVEL.Configuration);
        yield return Read(EN_TALON_COMMAND.QueryShgAutotune, "SHG Autotune", "Harmonic", "?SAUTO", EN_TALON_RESPONSE_KIND.Boolean, "SHG autotune 진행 상태를 조회합니다.");
        yield return Set(EN_TALON_COMMAND.SetShgAutotune, "SHG Autotune 시작/중지", "Harmonic", "SAUTO:{0}", 0, 1, "0/1", "완료 시 설정이 자동 저장되며 빈번한 사용을 피해야 합니다.", EN_TALON_RISK_LEVEL.Configuration);
        yield return Read(EN_TALON_COMMAND.QueryThgSpot, "THG Spot", "Harmonic", "?MTR:TSPOT", EN_TALON_RESPONSE_KIND.Integer, "현재 THG crystal spot 위치입니다.");
        yield return Set(EN_TALON_COMMAND.SetThgSpot, "THG Spot 이동", "Harmonic", "MTR:TSPOT:{0}", 1, 15, "spot", "이동 중 전원을 제거하면 손상될 수 있습니다.", EN_TALON_RISK_LEVEL.Persistent);
        yield return Read(EN_TALON_COMMAND.QueryThgSpotHours, "현재 THG Spot 시간", "Harmonic", "?MTR:THR", EN_TALON_RESPONSE_KIND.FloatingPoint, "현재 spot 누적 사용시간입니다.", "h");
        yield return Read(EN_TALON_COMMAND.QueryHeadHours, "Laser Head 시간", "상태", "?HEADHRS", EN_TALON_RESPONSE_KIND.FloatingPoint, "Crystal oven이 켜진 누적 Head 시간입니다.", "h");
        yield return Action(EN_TALON_COMMAND.SaveConfiguration, "설정 저장", "통신", "SAVE", EN_TALON_RISK_LEVEL.Persistent, "Q, EPRF, C1, Baudrate, QMODE를 flash 기본값으로 저장합니다. 빈번한 실행을 피해야 합니다.");
    }

    private static ST_TALON_COMMAND_SPEC Read(EN_TALON_COMMAND command, string name, string category, string query, EN_TALON_RESPONSE_KIND kind, string description, string unit = "") =>
        new(command, name, category, query, "", kind, EN_TALON_RISK_LEVEL.ReadOnly, null, null, unit, description);

    private static ST_TALON_COMMAND_SPEC Set(EN_TALON_COMMAND command, string name, string category, string template, double min, double max, string unit, string description, EN_TALON_RISK_LEVEL risk = EN_TALON_RISK_LEVEL.Configuration) =>
        new(command, name, category, "", template, EN_TALON_RESPONSE_KIND.Acknowledgement, risk, min, max, unit, description, true);

    private static ST_TALON_COMMAND_SPEC Action(EN_TALON_COMMAND command, string name, string category, string text, EN_TALON_RISK_LEVEL risk, string description) =>
        new(command, name, category, "", text, EN_TALON_RESPONSE_KIND.Acknowledgement, risk, null, null, "", description);
}
