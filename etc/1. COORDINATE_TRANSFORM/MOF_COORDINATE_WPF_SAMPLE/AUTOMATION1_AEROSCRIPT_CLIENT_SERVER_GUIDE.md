# Automation1 AeroScript Client/Server 연동 가이드

## 1. 적용 목적

이 샘플은 **A1 상위 프로그램(Client PC)** 과 **스캐너에 직접 연결된 Server PC**의 책임을 분리한다.

- Client PC: Recipe, Review 보정값, Cell/Scanner/DOE 조건과 좌표 변환 결과를 이용해 AeroScript를 생성한다.
- Server PC: Client가 생성한 Script를 원문 그대로 보관하고, 별도 실행 명령을 받았을 때 Automation1 컨트롤러로 전달해 실행한다.
- Automation1 Controller: Controller File System의 `.ascript` 파일을 지정 Task에서 실행한다.
- Server PC는 좌표를 다시 계산하거나 Script 내용을 변경하지 않는다.

## 2. 전체 동작 순서

1. WPF에서 CSV Recipe와 Review/Scanner 파라미터를 읽는다.
2. `CoordinateTransformService`가 Stage, Scanner `Gx/Gy`, 역방향 MOF 순서를 계산한다.
3. `AeroScriptGenerator`가 가공 가능 좌표를 AeroScript UTF-8 소스로 생성한다.
4. `AeroScriptPackage.Create()`가 Job ID와 Script SHA-256을 만든다.
5. 사용자가 **Server 전송**을 누르면 Client가 `UploadScript` 요청을 보낸다.
6. Server는 API Key, Job ID, 파일명, Script 크기와 SHA-256을 검증한다.
7. Server는 Script를 spool 폴더에 저장하고 `Uploaded` 상태를 응답한다.
8. 사용자가 **Server 실행**을 누르면 Client가 같은 Job ID로 `RunScript` 요청을 보낸다.
9. Server는 `Controller.Files.WriteText()`로 Script를 Controller File System에 저장한다.
10. Server는 `Controller.Runtime.Tasks[TaskIndex].Program.Run()`을 호출한다.
11. `Run()`은 프로그램 완료까지 기다리지 않으므로 Server가 Task Status를 100 ms 주기로 한 번씩 읽는다.
12. `TaskState.ProgramComplete`이면 `Completed`, `TaskState.Error`이면 오류 정보와 함께 `Failed`로 기록한다.
13. Client는 `GetStatus`를 폴링하고 최종 완료 또는 실패를 화면에 표시한다.

## 3. WPF 사용 방법

`Automation1 Script I/F` 탭에서 다음 순서로 검증한다.

1. `Server PC Host`, `Server Port`, `API Key`를 입력한다.
2. `Virtual Wait Simulation` 또는 `Hardware Coordinate Program`을 선택한다.
3. 실제 MCD의 Task 번호, Stage Y축, Scanner GX/GY 축 이름을 입력한다. `{0}`을 넣으면 선택 Head 번호로 치환된다.
4. `1. Script 생성`으로 Preview, 좌표 수, Job ID와 SHA-256을 확인한다.
5. `2. Server 전송`으로 Script만 Server PC에 저장한다.
6. 안전 조건과 장비 상태를 확인한 후 `3. Server 실행`을 누른다.
7. `상태 조회`로 현재 상태를 확인하거나 `전체 실행 및 완료 대기`로 전 과정을 자동 검증한다.

`선택 좌표만 Script 생성`을 체크하면 Matrix에서 선택한 좌표만 생성 대상이 된다. 체크하지 않으면 선택된 Scanner Head의 X 가공 가능 범위에 속한 전체 좌표가 대상이다.

## 4. Virtual Wait Simulation

이 모드는 제공된 Automation1 예제와 기술 답변을 기준으로 실제 Laser, PSO, Hardware Aux 또는 Galvo Calibration을 실행하지 않는다. **GX와 Stage Y가 MOF pair로 설정돼 있다는 전제**에서 Stage Y를 비동기로 이동시키고, GX band별 GY drill point 이동이 끝날 때마다 Stage `PositionFeedback`이 임계값을 통과할 때까지 기다린다.

```text
StartY로 이동 및 GX/GY 원점 이동
  -> MoveAbsolute(Y, StartY + Travel, StageSpeed) 비동기 시작
  -> GX Band 1의 GY Drill Point 반복
  -> wait(Y PositionFeedback > StartY + WaitStep)
  -> GX Band 2의 GY Drill Point 반복
  -> wait(Y PositionFeedback > StartY + WaitStep * 2)
  -> 마지막 Band 완료
  -> WaitForMotionDone(Y) + WaitForInPosition(Y)
```

Stage Travel이 음수이면 비교 연산자는 `<`로 자동 변경된다. Virtual mode는 한 번에 Scanner Head 한 개만 허용한다. 여러 Head가 선택돼 있으면 Client 생성 단계에서 차단한다.

| UI 파라미터 | 생성되는 AeroScript | 의미 |
| --- | --- | --- |
| Start Y Position | `$StartYPos` | Stage 이동 시작 기준 |
| Stage Travel / Speed | `MoveAbsolute(Y, target, speed)` | 비동기 기판 이송 |
| Scanner Rapid Speed | `SetupAxisSpeed(GX/GY)` | G0 drill point 이동 속도 |
| Ramp Rate | `SetupAxisRampValue`, `SetupCoordinatedRampValue` | Sine ramp 가속도 |
| Trajectory FIR | `AxisParameter.TrajectoryFirFilter` | Scanner trajectory filter |
| Motion Update kHz | `TaskParameter.MotionUpdateRate` | motion 생성 및 MoveDelay 해상도 |
| Execute Num Lines | `TaskParameter.ExecuteNumLines` | Task 1 ms scheduling line 수 |
| MoveDelay ms | `MoveDelay(GX, value)` | 각 GY point 사이 motion delay |
| Wait Step Y | `wait(StatusGetAxisItem(...PositionFeedback))` | GX band 전환 기준 |
| Software Limit | `SoftwareLimitLow/High` | GX/GY active travel limit |

`VelocityBlendingOn()`은 제공 예제와 실제 Task 초기 설정 일치를 위해 포함하지만, 공식 문서상 Velocity Blending은 `MoveLinear/MoveCw/MoveCcw` 같은 coordinated move에 적용되고 G0에는 직접 적용되지 않는다. `MoveDelay` 인자는 밀리초이며 MotionUpdateRate 설정값의 시간 간격으로 반올림된다.

### Virtual 검증 범위

- 확인 가능: 축 이동 명령, Stage feedback 변화, wait 진입/해제 순서, Task 완료/오류, Client/Server I/F.
- 확인 불가: 실제 Laser 출력, Hardware Aux edge, 실제 PSO pulse, Galvo 광학 보정, Scanner-Laser 물리 동기 정밀도.
- 공식 문서상 Virtual Axis의 feedback은 position command와 동일하게 설정되므로 wait 해제 확인은 논리·순서 검증이며 실제 Encoder 지연 특성 검증이 아니다.
- `SimulationAutomation1Runtime`은 TCP/Job 상태만 모사하고 AeroScript를 실행하지 않는다. Wait 검증은 Automation1 Virtual Controller와 `Automation1ReflectionRuntime`을 사용해야 한다.

제공 예제의 `GX=0/10/20/30`, `GY=-30..30`, `Y wait=+10/+20/+30` 조건을 정리한 `VIRTUAL_WAIT_SIMULATION_REFERENCE.ascript`를 함께 제공한다. WPF 동적 생성 결과와 비교하거나 Automation1 Studio Compiler에서 기준 구문을 확인할 때 사용한다.

## 5. TCP 프로토콜

전송 단위는 `4-byte big-endian payload length + UTF-8 JSON payload`이다. 현재 프로토콜 버전은 2이며 최대 frame은 32 MiB이다. Package에는 생성 모드가 포함되어 Client UI와 Server 로그에서 Virtual/Hardware Job을 구분한다.

| 요청 | 필수 데이터 | Server 처리 | 정상 상태 |
| --- | --- | --- | --- |
| `UploadScript` | API Key, Job ID, file name, Task, Script, SHA-256 | 검증 후 spool 저장 | `Uploaded` |
| `RunScript` | API Key, Job ID | 실행 Queue 등록 | `Queued` |
| `GetStatus` | API Key, Job ID | 현재 snapshot 반환 | 상태별 상이 |

상태 흐름은 `Uploaded → Queued → TransferringToController → Running → Completed`이다. 검증 실패는 `Rejected`, 전송·컴파일·실행·Timeout 오류는 `Failed`로 처리한다.

Server는 `ModePolicy`를 Upload 단계에서 검사한다. Virtual 검증 Server는 `VirtualOnly`, 실제 장비 Server는 `HardwareOnly`로 실행한다. 정책과 다른 Job은 `MODE_POLICY_REJECTED`로 거부되므로 Virtual Script가 실제 장비 Server에 잘못 전달되는 경로를 차단한다.

API Key는 인증용 최소 예제이며 암호화를 제공하지 않는다. 실제 장비망에서는 전용 VLAN/방화벽 allow-list를 적용하고, 비신뢰망을 통과하면 TLS 터널 또는 상위 보안 채널을 반드시 사용해야 한다.

## 6. Server PC 실행

### TCP/Job Protocol 시뮬레이션

```powershell
RUN_AUTOMATION1_SERVER_SIMULATION.bat
```

또는:

```powershell
dotnet run --project Automation1Server/Automation1Server.csproj -- `
  --runtime=simulation --bind=0.0.0.0 --port=46100 --api-key=change-this-key
```

이 Runtime은 전송, SHA-256, Queue와 완료 상태만 검증하며 AeroScript를 compile/execute하지 않는다.

### Automation1 Virtual Wait 검증

Automation1-MDK의 Virtual Controller가 실행 중인 Server PC에서 다음 파일을 사용한다.

```powershell
$env:A1_SCRIPT_API_KEY = "virtual-test-secret"
$env:AUTOMATION1_DOTNET_DLL = "C:\Program Files\Aerotech\Automation1-MDK\APIs\DotNet\netstandard2.1\Aerotech.Automation1.DotNet.dll"
RUN_AUTOMATION1_VIRTUAL_WAIT_SERVER.bat
```

WPF에서는 `Virtual Wait Simulation`을 선택하고 Scanner Head 하나만 선택한다. 생성된 Script Preview에 Laser/PSO 함수가 없는지 확인한 뒤 전송·실행한다.

### 실제 Automation1

Server PC에 Automation1-MDK와 실제 장비 MCD가 설치돼 있어야 한다.

```powershell
$env:A1_SCRIPT_API_KEY = "production-secret"
$env:AUTOMATION1_DOTNET_DLL = "C:\Program Files\Aerotech\Automation1-MDK\APIs\DotNet\netstandard2.1\Aerotech.Automation1.DotNet.dll"
RUN_AUTOMATION1_SERVER_REAL.bat
```

실제 장비 BAT는 `--mode-policy=HardwareOnly`, Virtual Wait BAT는 `--mode-policy=VirtualOnly`를 명시한다. `--runtime=automation1`을 직접 실행할 때 기본 정책은 `HardwareOnly`이다.

원격 Automation1 Controller를 사용할 때는 Server Host 인자가 아니라 Server 실행 인자 `--controller=<Automation1 host>`로 지정한다. 공식 문서는 원격 Ethernet 연결에 `Controller.ConnectSecure` 사용을 권장하므로 실제 보안 설정과 인증서 정책에 맞춰 `Automation1ReflectionRuntime`을 typed SDK adapter로 교체하는 것이 운영 기준이다.

## 7. 클래스 책임

| 클래스 | 실행 위치 | 책임 |
| --- | --- | --- |
| `AeroScriptGenerator` | Client PC | Virtual Wait 또는 Hardware Coordinate Script 생성 |
| `AeroScriptPackage` | Client PC | Job ID, source, SHA-256, Task, 생성 모드 묶음 |
| `AeroScriptClient` | Client PC | Upload/Run/Status TCP 요청 |
| `AeroScriptProtocol` | 양쪽 | 길이 프리픽스 JSON frame 직렬화 |
| `AeroScriptServer` | Server PC | 검증, spool, 실행 queue, 상태 관리 |
| `AeroScriptModePolicy` | Server PC | Any/VirtualOnly/HardwareOnly 업로드 허용 정책 |
| `IAutomation1Runtime` | Server PC | Automation1 실행 추상화 |
| `SimulationAutomation1Runtime` | Server PC | SDK 없는 환경의 TCP/Job 완료·오류 검증. AeroScript는 실행하지 않음 |
| `Automation1ReflectionRuntime` | Server PC | 공식 .NET API DLL을 런타임 로드해 Controller 호출 |

## 8. 장비 적용 전에 확정할 항목

- MCD에 정의된 실제 Scanner Head별 `Gx/Gy` Axis 이름
- 사용할 Automation1 Task 번호와 다른 프로그램과의 Task 점유 규칙
- Scanner/Stage MOF 동기화 방식, PSO/IFOV/레이저 Trigger 명령과 Interlock 순서
- 축 Enable/Home 책임이 Server Script인지 장비 초기화 시퀀스인지
- Script 최대 좌표 수, Controller compile 시간, 실행 Timeout
- E-Stop, Door, Laser Ready, Scanner Ready, Stage In-position 확인 위치
- 중복 Job, 재시도, 전원 재기동 후 spool 복구 정책
- 실제 운영용 인증, TLS, 방화벽, 로그 보존 및 Recipe/Script 버전 추적 정책

현재 생성 Script는 좌표 전달과 Scanner 축 이동의 기본 구조를 보여준다. 레이저 조사 및 PSO/IFOV 명령은 장비별 안전 시퀀스가 확정되기 전에는 자동으로 삽입하지 않는다.

## 9. 공식 Automation1 근거

- [.NET API 개요](https://help.aerotech.com/automation1/Content/APIs/dotNET/Get-Started/Intro-to-dotNET.htm)
- [Controller.Connect 및 Runtime](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Controller-dotNET.htm)
- [Controller Files.WriteText/Upload](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Files-dotNET.htm)
- [Task Program.Run 및 TaskState](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Tasks-dotNET.htm)
- [AeroScript MoveLinear](https://help.aerotech.com/automation1/Content/Concepts/Motion-Functions.htm)
- [Motion Setup과 Ramp/Wait Mode](https://help.aerotech.com/automation1/Content/Concepts/Motion-Setup-Functions.htm)
- [MotionUpdateRate Parameter](https://help.aerotech.com/automation1/Content/Parameters/MotionUpdateRate.htm)
- [Controller Status와 PositionFeedback](https://help.aerotech.com/automation1/Content/Concepts/Controller-Status-Functions.htm)
- [Virtual Axes 제약과 Feedback 동작](https://help.aerotech.com/automation1/Content/Virtual-Axis-Overview.htm)
- [Galvo Functions와 Hardware 조건](https://help.aerotech.com/automation1/Content/Concepts/Galvo-Functions.htm)
- [Controller File System](https://help.aerotech.com/automation1/Content/Controller-File-System.htm)
