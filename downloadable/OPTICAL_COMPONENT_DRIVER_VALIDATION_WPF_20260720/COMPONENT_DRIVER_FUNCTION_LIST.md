# 광학 컴포넌트 드라이버 기능 목록

## 1. 공통 기능

| 구분 | 기능 | 구현 내용 |
| --- | --- | --- |
| Profile | 장비별 연결·이론·운용 정보 | 연결 방식, 기본 endpoint, 속도, 종단문자, 주의사항을 한 객체로 관리 |
| Command Catalog | 기능과 protocol 매핑 | 표시명, 분류, 명령 template, parameter 범위, 응답 종류, 위험도를 관리 |
| Protocol | 명령 생성·응답 판정 | InvariantCulture 숫자 형식, 범위 검사, echo/CR/LF 정규화, 응답 type 검사 |
| Transport | 통신 매체 분리 | Serial, TCP, Simulator를 동일 interface로 교체 가능 |
| Driver | 실행·상태·trace | 안전 잠금 검사, 송수신 시간, 성공/실패, 최신 상태 값을 기록 |
| Simulator | 장비 없는 검증 | 정상 응답, 위치 변경, timeout 1회, invalid 응답 1회 주입 |
| WPF | 통합 검증 화면 | 장비 선택, 명령 목록, parameter, status, 전체 검증, guide, trace |
| Safety | 위험 명령 차단 | Hardware write, motion area, beam path/interlock, operator 확인 |

## 2. Talon Laser

연결 기준: RS-232, 115200 bps, 8-N-1, flow control 없음, TX `<CR>`, RX `<CR>` 또는 `<LF>`.

| 기능 | 예제 명령 | 응답/설명 |
| --- | --- | --- |
| 식별 조회 | `*IDN?` | 제조사, 모델, serial, software version |
| 시스템 상태 | `?F` | 현재 event string |
| Event history | `?FH` | 세미콜론 구분 최근 16개 event code |
| Status byte | `*STB?` | emission, shutter, gate, fault, motor 등 bit 상태 |
| 출력 power | `?P` | 내부 power monitor, W |
| Diode current | `?C1`, `?CS1`, `?DCL1` | 실측, 명령값, factory limit |
| 온도 | `?T1`, `?TT`, `?CT` | diode, tower, chassis temperature |
| PRF/Q mode | `?Q`, `QMODE?` | repetition rate와 Q-switch mode |
| Shutter/Gate | `?SHT`, `?G`, `?GEXT` | beam path와 pulse gate 상태 |
| Harmonic | `?SHG`, `?SAUTO`, `?MTR:TSPOT` | SHG oven, autotune, THG spot |
| 출력 차단 | `OFF` | emission과 current를 낮추는 안전 동작 |

Set/출력 명령은 매뉴얼상 응답이 정의되지 않은 경우 응답을 기다리지 않습니다. 실제 출력은 안전 확인 네 조건이 충족되어야 합니다.

## 3. CONEX-AGP Attenuator

연결 기준: USB virtual COM, 921600 bps, 8-N-1, Xon/Xoff, `<CR><LF>`, address 기본 1.

| 기능 | 예제 명령 | 설명 |
| --- | --- | --- |
| Revision | `1VE?` | controller revision |
| 현재/목표 위치 | `1TP?`, `1TH?` | encoder current/target position |
| 오류/상태 | `1TS?` | positioner error와 controller state |
| Home | `1OR` | reference search |
| 절대/상대 이동 | `1PA12.345`, `1PR-0.500` | user unit 기준 이동 |
| Stop | `1ST` | motion 정지 |
| Reset | `1RS` | controller reset |
| Error 확인 | `1TE?`, `1TB?` | 최근 command error와 문자열 |

실제 운전 전 negative/positive software limit와 user unit 설정을 확인해야 합니다. 위치 값만 맞아도 감쇠 방향이 반대일 수 있으므로 power meter로 회전 방향을 교차 확인해야 합니다.

## 4. Motorized Beam Expander

연결 기준: RS-232, 9600 bps, 8-N-1, stop bit 1 또는 2. Motor 1/2 범위 0~4500 step, 약 4.15 um/step.

| 기능 | 명령 | 설명 |
| --- | --- | --- |
| Software reset | `#0:` | register reset, 이후 초기화 필요 |
| 초기화 | `#I:` | reference 탐색과 optical/mechanical offset load |
| Motor 1 이동 | `#1:2300` | divergence 관련 lens group 이동 |
| Motor 2 이동 | `#2:2100` | magnification 관련 lens group 이동 |
| Motor 1 위치 | `#7:` | `$7:value` 형식 step counter |
| Motor 2 위치 | `#8:` | `$8:value` 형식 step counter |
| Hall sensor | `G1:`, `G2:` | 각 motor Hall AD value |
| Optical offset | `G3:`, `G4:` | flash/EEPROM offset |

전원 인가 때 register가 비어 있으므로 항상 초기화가 필요합니다. 각 이동 명령 뒤 `!` ACK를 받은 후 다음 축 명령을 보내야 합니다. 배율과 divergence는 단순 step 자체가 아니라 장비별 calibration table을 통해 변환해야 합니다.

## 5. PowerMax USB/RS Power Meter

연결 기준: RS-232 legacy ASCII, 115200 bps, 8-N-1, `<CR>`.

| 기능 | 명령 | 설명 |
| --- | --- | --- |
| Hardware 설명 | `*ind` | meter/sensor 식별 |
| Serial | `sn?` | serial number |
| Power | `pw?` | W 단위 측정값 |
| Wavelength 조회/설정 | `wv?`, `wv 3.55E-7` | meter 단위 wavelength correction |
| Beam position | `pos` | 지원 sensor의 X,Y 위치 |
| Streaming 시작/정지 | `dst`, `dsp` | 연속 측정 data stream |
| Reset | `*rst` | power-on 상태로 operational parameter reset |
| Range/온도/정보 | `rmi`, `rmx`, `tmp`, `v?` | sensor range, thermistor, firmware |

355 nm는 `3.55E-7 m`로 전송해야 합니다. 측정값에는 sensor calibration, wavelength correction, zero, range, saturation 상태가 함께 영향을 줍니다.

## 6. XPS Motion Controller

연결 기준: Ethernet TCP/IP, command socket 예제 port 5001, `FunctionName(arguments)` API.

| 기능 | API | 설명 |
| --- | --- | --- |
| Firmware | `FirmwareVersionGet(char *)` | controller firmware |
| Controller status | `ControllerStatusGet(int *)` | 전체 controller 상태 |
| Group initialize | `GroupInitialize(Group1)` | group state 초기화 |
| Home | `GroupHomeSearch(Group1)` | group reference search |
| Group status | `GroupStatusGet(Group1,int *)` | group state code |
| Current position | `GroupPositionCurrentGet(Group1,1,double *)` | 현재 축 위치 |
| 절대/상대 이동 | `GroupMoveAbsolute`, `GroupMoveRelative` | group trajectory 실행 |
| Abort/Kill | `GroupMoveAbort`, `GroupKill` | motion 중단 또는 group kill |
| Status 문자열 | `GroupStatusStringGet` | status code 설명 조회 |
| Error 문자열 | `ErrorStringGet` | 음수 error code 해석 |

응답 첫 필드 `0`은 성공이며 음수는 error입니다. `Initialize → Home → Ready → Move` 상태 순서를 지키고, 호출 성공과 실제 이동 완료는 구분하여 position/status를 계속 poll해야 합니다.

## 7. New Focus Picomotor

첨부 자료 기준 연결: 제조사 `CmdLib.dll`, USB/Ethernet master 검색, RS-485 slave address.

| 기능 | CmdLib 동작 | 설명 |
| --- | --- | --- |
| 다중/단일 검색 | constructor, `DiscoverDevices` | device key 목록 확보 |
| Open/Close | `Open`, `Close` | master 통신 lifecycle |
| 식별 | `IdentifyInstrument`, `GetModelSerial` | model, serial, firmware |
| Address 탐색 | `GetMasterDeviceAddress`, `GetDeviceAddresses` | master/slave 구분 |
| 위치 | `GetPosition` | motor 1~4 position |
| 상대 이동 | `RelativeMove` | 지정 step/count 이동 |
| Motion 완료 | `GetMotionDone` | polling 종료 조건 |
| Error | `GetError` | master/slave error 확인 |
| Closed loop | `Get/SetCLEnabledSetting` | 8743 closed-loop enable |
| 정리 | `Shutdown` | USB/Ethernet background task 종료 |

첨부 Programming Samples 문서는 원시 command를 정의하지 않는다고 명시합니다. 따라서 이 예제는 CmdLib logical operation과 simulator를 제공하며, 실제 hardware adapter에는 배포 승인된 `CmdLib.dll`과 해당 모델 User Manual이 필요합니다.
