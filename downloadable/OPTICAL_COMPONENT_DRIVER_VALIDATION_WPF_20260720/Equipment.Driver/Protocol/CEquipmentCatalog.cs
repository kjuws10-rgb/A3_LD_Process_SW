namespace Equipment.Driver;

public static class CEquipmentCatalog
{
    private static readonly IReadOnlyDictionary<EN_EQUIPMENT_TYPE, ST_EQUIPMENT_PROFILE> Profiles = CreateProfiles();
    private static readonly IReadOnlyList<ST_EQUIPMENT_COMMAND_SPEC> Commands = CreateCommands().ToArray();

    public static IReadOnlyList<ST_EQUIPMENT_PROFILE> AllProfiles => Profiles.Values.ToArray();
    public static ST_EQUIPMENT_PROFILE GetProfile(EN_EQUIPMENT_TYPE type) => Profiles[type];
    public static IReadOnlyList<ST_EQUIPMENT_COMMAND_SPEC> GetCommands(EN_EQUIPMENT_TYPE type) =>
        Commands.Where(item => item.Equipment == type).ToArray();

    private static IReadOnlyDictionary<EN_EQUIPMENT_TYPE, ST_EQUIPMENT_PROFILE> CreateProfiles()
    {
        return new[]
        {
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.TalonLaser, "Talon Laser", "355 nm 레이저 광원과 Q-switch, shutter, gate, harmonic 상태 제어", "Talon Users Manual Rev. C 90065281", EN_EQUIPMENT_TRANSPORT.Serial, "COM1", 115200, 0, "\r", "펌프 다이오드 에너지를 고체 레이저 매질에 저장한 뒤 Q-switch로 짧은 펄스를 만들고, SHG/THG 결정으로 355 nm를 생성합니다.", "연결 → 식별/상태/온도 조회 → interlock와 beam path 확인 → 낮은 출력부터 설정 → gate/shutter/emission 순서로 운전 → 종료 시 출력 차단", "Set/출력 명령은 응답을 기다리지 않는 항목이 있습니다. CR 전송, CR/LF 응답을 사용하며 Event history는 세미콜론 구분 16개입니다.", "시뮬레이터와 read-only protocol을 검증합니다. 실제 광출력 검증은 LSO 통제와 계측기가 필요합니다."),
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "CONEX-AGP Attenuator", "감쇠기 회전축 위치를 바꾸어 레이저 전달 에너지를 조절", "CONEX-AGP Controller Documentation EDH0263En1014", EN_EQUIPMENT_TRANSPORT.Serial, "COM3", 921600, 0, "\r\n", "편광 소자 또는 감쇠 소자의 각도를 바꾸면 통과 광량이 변합니다. Encoder feedback과 PI loop가 목표 위치를 유지합니다.", "USB virtual COM 연결 → VE/TS/TP 조회 → OR home → software limit 확인 → PA/PR 이동 → TP/TS polling → ST 정지", "921600 bps, 8-N-1, Xon/Xoff, CRLF입니다. 명령은 controller address(기본 1) + 2문자 mnemonic 형식입니다.", "시뮬레이터 및 ASCII 형식을 검증합니다. 실제 회전 방향, 단위, software limit는 장비 configuration과 대조해야 합니다."),
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "Motorized Beam Expander", "두 렌즈군 위치로 배율과 divergence를 조절", "manual motorized beam expander", EN_EQUIPMENT_TRANSPORT.Serial, "COM4", 9600, 0, "", "두 렌즈군의 상대 위치가 beam diameter와 divergence를 결정합니다. Motor 1/2는 0~4500 step이며 1 step은 약 4.15 um입니다.", "전원 인가 → #I: 초기화 → #7:/#8: 위치 확인 → 한 축씩 #1:/#2: 이동하고 각 ACK(!) 대기 → 최종 위치 재조회", "9600 bps, 8-N-1, stop bit 1 또는 2입니다. 전원 인가 때마다 initialization이 필요하며 다음 명령 전 ACK를 기다려야 합니다.", "위치/ACK/범위를 검증합니다. 광학 배율-두 모터 step 변환표는 장비별 calibration table을 사용해야 합니다."),
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.PowerMaxMeter, "PowerMax USB/RS Power Meter", "가공 전후 레이저 power와 beam position 계측", "PowerMax-USB_RS User Manual 1169780 Rev.AD", EN_EQUIPMENT_TRANSPORT.Serial, "COM6", 115200, 0, "\r", "Sensor가 입사 광에너지를 전기 신호로 변환하고 calibration, wavelength correction, range를 적용해 W 단위 값을 계산합니다.", "식별/serial 조회 → wavelength 설정 → streaming 또는 단발 pw? 측정 → range/over-range 확인 → 평균·최소·최대 기록 → streaming 중지", "Legacy LaserPAD/SSIM ASCII는 CR 종단입니다. Wavelength 명령은 meter 단위 값을 사용하므로 nm↔m 변환을 명확히 해야 합니다.", "명령/응답과 통계 누적을 검증합니다. 정확도 검증에는 교정 유효기간이 남은 실제 sensor와 기준 광원이 필요합니다."),
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.XpsController, "XPS Motion Controller", "Stage/축 group의 초기화, home, 위치 이동과 상태 감시", "XPS Unified Programmer's Manual EDH0373En1046", EN_EQUIPMENT_TRANSPORT.Tcp, "192.168.254.254", 0, 5001, "", "XPS는 여러 positioner를 group으로 묶고 group state machine에 따라 initialize, home, ready, moving, fault 상태를 관리합니다.", "TCP 5001 연결 → Firmware/Controller status → GroupInitialize → GroupHomeSearch → GroupStatusGet → absolute/relative move → current position poll → abort/kill", "API 문자열은 FunctionName(arguments) 형식이며 결과 첫 필드는 error code입니다. 0만 성공이며 상태 전이 순서를 지켜야 합니다.", "TCP framing과 API result를 검증합니다. Group1 이름, 축 수, stage database, travel limit는 실제 XPS 설정에 맞게 바꿔야 합니다."),
            new ST_EQUIPMENT_PROFILE(EN_EQUIPMENT_TYPE.Picomotor, "New Focus Picomotor", "미세 광축 정렬용 1~4축 open/closed-loop Picomotor 제어", "Picomotor Programming Samples User's Manual 2.0.2", EN_EQUIPMENT_TRANSPORT.VendorApi, "CmdLib.dll", 0, 0, "", "Piezo actuator의 반복 미세 충격으로 나사를 조금씩 회전시켜 광학 마운트를 정렬합니다. 8742는 open-loop, 8743은 closed-loop 기능을 포함합니다.", "CmdLib 장비 검색 → device key 확보 → Open → model/serial 및 address 조회 → position zero → relative move → error/motion done/position polling → Stop → Close → Shutdown", "첨부 자료는 명령 사전이 아니라 CmdLib.dll programming sample입니다. USB/Ethernet master와 RS-485 slave는 device key + address로 구분합니다.", "Simulator와 reflection 기반 CmdLib adapter를 제공합니다. 실제 hardware는 제조사 CmdLib.dll, 해당 모델 User Manual, 배포 권한을 준비해야 합니다.")
        }.ToDictionary(item => item.Type);
    }

    private static IEnumerable<ST_EQUIPMENT_COMMAND_SPEC> CreateCommands()
    {
        // Talon Rev.C: query는 응답을 받고 대부분의 set/action은 응답을 기다리지 않는다.
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "id", "장비 식별", "상태", "*IDN?", EN_EQUIPMENT_RESPONSE.Text, "제조사, 모델, serial, firmware 조회");
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "status", "시스템 상태", "상태", "?F", EN_EQUIPMENT_RESPONSE.Text, "현재 event string 조회");
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "history", "Event 이력", "상태", "?FH", EN_EQUIPMENT_RESPONSE.Text, "최근 event code 16개 조회");
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "power", "출력 Power", "계측", "?P", EN_EQUIPMENT_RESPONSE.FloatingPoint, "내부 power monitor 값", "W");
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "temperature", "Diode 온도", "상태", "?T1", EN_EQUIPMENT_RESPONSE.FloatingPoint, "Diode temperature", "degC");
        yield return Read(EN_EQUIPMENT_TYPE.TalonLaser, "current", "Diode 전류", "상태", "?C1", EN_EQUIPMENT_RESPONSE.FloatingPoint, "실측 diode current", "A");
        yield return Set(EN_EQUIPMENT_TYPE.TalonLaser, "set-current", "Diode 전류 설정", "출력", "C1:{0:F2}", EN_EQUIPMENT_RISK.LaserOutput, 0, 100, "A", "실제 상한은 ?DCL1 결과 사용", 1);
        yield return Action(EN_EQUIPMENT_TYPE.TalonLaser, "emission-off", "Emission OFF", "출력", "OFF", EN_EQUIPMENT_RISK.LaserOutput, "레이저 출력을 차단", false);

        // CONEX-AGP: address 1을 예제 기본값으로 사용한다.
        yield return Read(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "version", "Controller 정보", "상태", "1VE?", EN_EQUIPMENT_RESPONSE.Text, "revision 정보 조회");
        yield return Read(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "position", "현재 위치", "위치", "1TP?", EN_EQUIPMENT_RESPONSE.FloatingPoint, "encoder current position");
        yield return Read(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "target", "목표 위치", "위치", "1TH?", EN_EQUIPMENT_RESPONSE.FloatingPoint, "target position");
        yield return Read(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "state", "오류/상태", "상태", "1TS?", EN_EQUIPMENT_RESPONSE.Text, "positioner error와 controller state");
        yield return Set(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "move-abs", "절대 이동", "동작", "1PA{0:F3}", EN_EQUIPMENT_RISK.Motion, -1_000_000, 1_000_000, "user unit", "software limit 안에서 절대 이동", 0);
        yield return Set(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "move-rel", "상대 이동", "동작", "1PR{0:F3}", EN_EQUIPMENT_RISK.Motion, -100_000, 100_000, "user unit", "현재 위치 기준 상대 이동", 0);
        yield return Action(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "home", "Home 검색", "동작", "1OR", EN_EQUIPMENT_RISK.Motion, "reference 검색", false);
        yield return Action(EN_EQUIPMENT_TYPE.ConexAgpAttenuator, "stop", "정지", "안전", "1ST", EN_EQUIPMENT_RISK.Configuration, "즉시 motion 정지", false);

        yield return Action(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "initialize", "두 Motor 초기화", "초기화", "#I:", EN_EQUIPMENT_RISK.Motion, "reference 및 optical zero offset load", true);
        yield return Read(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "motor1", "Motor 1 위치", "위치", "#7:", EN_EQUIPMENT_RESPONSE.Integer, "Motor 1 step counter", "step");
        yield return Read(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "motor2", "Motor 2 위치", "위치", "#8:", EN_EQUIPMENT_RESPONSE.Integer, "Motor 2 step counter", "step");
        yield return Set(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "move1", "Motor 1 이동", "동작", "#1:{0}", EN_EQUIPMENT_RISK.Motion, 0, 4500, "step", "divergence 축 이동 후 ! ACK 대기", 1600);
        yield return Set(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "move2", "Motor 2 이동", "동작", "#2:{0}", EN_EQUIPMENT_RISK.Motion, 0, 4500, "step", "magnification 축 이동 후 ! ACK 대기", 1600);
        yield return Action(EN_EQUIPMENT_TYPE.MotorizedBeamExpander, "reset", "Software Reset", "초기화", "#0:", EN_EQUIPMENT_RISK.Persistent, "register reset 후 재초기화 필요", true);

        yield return Read(EN_EQUIPMENT_TYPE.PowerMaxMeter, "identity", "Hardware 설명", "상태", "*ind", EN_EQUIPMENT_RESPONSE.Text, "sensor hardware description");
        yield return Read(EN_EQUIPMENT_TYPE.PowerMaxMeter, "serial", "Serial 번호", "상태", "sn?", EN_EQUIPMENT_RESPONSE.Text, "sensor serial number");
        yield return Read(EN_EQUIPMENT_TYPE.PowerMaxMeter, "wavelength", "Wavelength", "설정", "wv?", EN_EQUIPMENT_RESPONSE.FloatingPoint, "현재 wavelength correction 값", "m");
        yield return Read(EN_EQUIPMENT_TYPE.PowerMaxMeter, "power", "Power 읽기", "계측", "pw?", EN_EQUIPMENT_RESPONSE.FloatingPoint, "현재 optical power", "W");
        yield return Read(EN_EQUIPMENT_TYPE.PowerMaxMeter, "position", "Beam 위치", "계측", "pos", EN_EQUIPMENT_RESPONSE.Text, "position sensing detector의 X,Y", "mm");
        yield return Set(EN_EQUIPMENT_TYPE.PowerMaxMeter, "set-wavelength", "Wavelength 설정", "설정", "wv {0:0.########E+0}", EN_EQUIPMENT_RISK.Configuration, 1e-9, 20e-6, "m", "예: 355 nm = 3.55E-7 m", 3.55e-7);
        yield return Action(EN_EQUIPMENT_TYPE.PowerMaxMeter, "stream-start", "Streaming 시작", "계측", "dst", EN_EQUIPMENT_RISK.Configuration, "연속 data streaming 시작", false);
        yield return Action(EN_EQUIPMENT_TYPE.PowerMaxMeter, "stream-stop", "Streaming 중지", "계측", "dsp", EN_EQUIPMENT_RISK.Configuration, "연속 data streaming 중지", false);

        yield return Read(EN_EQUIPMENT_TYPE.XpsController, "firmware", "Firmware", "상태", "FirmwareVersionGet(char *)", EN_EQUIPMENT_RESPONSE.XpsResult, "XPS firmware version");
        yield return Read(EN_EQUIPMENT_TYPE.XpsController, "controller-status", "Controller 상태", "상태", "ControllerStatusGet(int *)", EN_EQUIPMENT_RESPONSE.XpsResult, "controller status code");
        yield return Read(EN_EQUIPMENT_TYPE.XpsController, "group-status", "Group1 상태", "상태", "GroupStatusGet(Group1,int *)", EN_EQUIPMENT_RESPONSE.XpsResult, "group status code");
        yield return Read(EN_EQUIPMENT_TYPE.XpsController, "position", "Group1 위치", "위치", "GroupPositionCurrentGet(Group1,1,double *)", EN_EQUIPMENT_RESPONSE.XpsResult, "axis current position");
        yield return Action(EN_EQUIPMENT_TYPE.XpsController, "initialize", "Group1 초기화", "동작", "GroupInitialize(Group1)", EN_EQUIPMENT_RISK.Motion, "NOTINIT에서 초기화", true);
        yield return Action(EN_EQUIPMENT_TYPE.XpsController, "home", "Group1 Home", "동작", "GroupHomeSearch(Group1)", EN_EQUIPMENT_RISK.Motion, "initialized group home search", true);
        yield return Set(EN_EQUIPMENT_TYPE.XpsController, "move-abs", "절대 이동", "동작", "GroupMoveAbsolute(Group1,{0:F6})", EN_EQUIPMENT_RISK.Motion, -1_000_000, 1_000_000, "stage unit", "Group1 absolute move", 0);
        yield return Set(EN_EQUIPMENT_TYPE.XpsController, "move-rel", "상대 이동", "동작", "GroupMoveRelative(Group1,{0:F6})", EN_EQUIPMENT_RISK.Motion, -100_000, 100_000, "stage unit", "Group1 relative move", 0);
        yield return Action(EN_EQUIPMENT_TYPE.XpsController, "abort", "Move Abort", "안전", "GroupMoveAbort(Group1)", EN_EQUIPMENT_RISK.Configuration, "진행 중 motion 중단", true);

        // 첨부 Picomotor 자료는 CmdLib API sample이므로 logical operation으로 모델링한다.
        yield return Read(EN_EQUIPMENT_TYPE.Picomotor, "discover", "장비 검색", "Lifecycle", "CmdLib.DiscoverDevices()", EN_EQUIPMENT_RESPONSE.Text, "USB/Ethernet master 검색과 device key 반환");
        yield return Read(EN_EQUIPMENT_TYPE.Picomotor, "identify", "장비 식별", "상태", "CmdLib.IdentifyInstrument(deviceKey)", EN_EQUIPMENT_RESPONSE.Text, "model, serial, firmware, date");
        yield return Read(EN_EQUIPMENT_TYPE.Picomotor, "position", "Motor 1 위치", "위치", "CmdLib.GetPosition(deviceKey,address,1)", EN_EQUIPMENT_RESPONSE.Integer, "selected motor position", "step/count");
        yield return Read(EN_EQUIPMENT_TYPE.Picomotor, "motion-done", "Motion 완료", "상태", "CmdLib.GetMotionDone(deviceKey,address,1)", EN_EQUIPMENT_RESPONSE.Integer, "motion done polling");
        yield return Read(EN_EQUIPMENT_TYPE.Picomotor, "error", "Error 조회", "상태", "CmdLib.GetError(deviceKey,address)", EN_EQUIPMENT_RESPONSE.Text, "master/slave error 조회");
        yield return Set(EN_EQUIPMENT_TYPE.Picomotor, "relative-move", "상대 이동", "동작", "CmdLib.RelativeMove(deviceKey,address,1,{0})", EN_EQUIPMENT_RISK.Motion, -2_000_000_000, 2_000_000_000, "step/count", "Motor 1 relative move", 100);
        yield return Action(EN_EQUIPMENT_TYPE.Picomotor, "stop", "Motion 정지", "안전", "CmdLib.StopMotion(deviceKey,address,1)", EN_EQUIPMENT_RISK.Configuration, "selected motor 즉시 정지", true);
    }

    private static ST_EQUIPMENT_COMMAND_SPEC Read(EN_EQUIPMENT_TYPE e, string id, string name, string category, string text, EN_EQUIPMENT_RESPONSE kind, string description, string unit = "") =>
        new(e, id, name, category, text, true, kind, EN_EQUIPMENT_RISK.ReadOnly, null, null, unit, description);

    private static ST_EQUIPMENT_COMMAND_SPEC Set(EN_EQUIPMENT_TYPE e, string id, string name, string category, string text, EN_EQUIPMENT_RISK risk, double min, double max, string unit, string description, double value) =>
        new(e, id, name, category, text, false, EN_EQUIPMENT_RESPONSE.Acknowledgement, risk, min, max, unit, description, value);

    private static ST_EQUIPMENT_COMMAND_SPEC Action(EN_EQUIPMENT_TYPE e, string id, string name, string category, string text, EN_EQUIPMENT_RISK risk, string description, bool expectsResponse) =>
        new(e, id, name, category, text, expectsResponse, expectsResponse ? EN_EQUIPMENT_RESPONSE.Acknowledgement : EN_EQUIPMENT_RESPONSE.Acknowledgement, risk, null, null, "", description);
}
