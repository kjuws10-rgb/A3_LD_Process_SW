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
2. 실제 MCD의 Task 번호와 스캐너 축 이름 규칙을 입력한다.
3. `Gx Axis Template=Gx{0}`, `Gy Axis Template=Gy{0}`이면 H1은 `Gx1/Gy1`, H5는 `Gx5/Gy5`로 생성된다.
4. `1. Script 생성`으로 Preview, 좌표 수, Job ID와 SHA-256을 확인한다.
5. `2. Server 전송`으로 Script만 Server PC에 저장한다.
6. 안전 조건과 장비 상태를 확인한 후 `3. Server 실행`을 누른다.
7. `상태 조회`로 현재 상태를 확인하거나 `전체 실행 및 완료 대기`로 전 과정을 자동 검증한다.

`선택 좌표만 Script 생성`을 체크하면 Matrix에서 선택한 좌표만 생성 대상이 된다. 체크하지 않으면 선택된 Scanner Head의 X 가공 가능 범위에 속한 전체 좌표가 대상이다.

## 4. TCP 프로토콜

전송 단위는 `4-byte big-endian payload length + UTF-8 JSON payload`이다. 현재 프로토콜 버전은 1이며 최대 frame은 32 MiB이다.

| 요청 | 필수 데이터 | Server 처리 | 정상 상태 |
| --- | --- | --- | --- |
| `UploadScript` | API Key, Job ID, file name, Task, Script, SHA-256 | 검증 후 spool 저장 | `Uploaded` |
| `RunScript` | API Key, Job ID | 실행 Queue 등록 | `Queued` |
| `GetStatus` | API Key, Job ID | 현재 snapshot 반환 | 상태별 상이 |

상태 흐름은 `Uploaded → Queued → TransferringToController → Running → Completed`이다. 검증 실패는 `Rejected`, 전송·컴파일·실행·Timeout 오류는 `Failed`로 처리한다.

API Key는 인증용 최소 예제이며 암호화를 제공하지 않는다. 실제 장비망에서는 전용 VLAN/방화벽 allow-list를 적용하고, 비신뢰망을 통과하면 TLS 터널 또는 상위 보안 채널을 반드시 사용해야 한다.

## 5. Server PC 실행

### 시뮬레이션

```powershell
RUN_AUTOMATION1_SERVER_SIMULATION.bat
```

또는:

```powershell
dotnet run --project Automation1Server/Automation1Server.csproj -- `
  --runtime=simulation --bind=0.0.0.0 --port=46100 --api-key=change-this-key
```

### 실제 Automation1

Server PC에 Automation1-MDK와 실제 장비 MCD가 설치돼 있어야 한다.

```powershell
$env:A1_SCRIPT_API_KEY = "production-secret"
$env:AUTOMATION1_DOTNET_DLL = "C:\Program Files\Aerotech\Automation1-MDK\APIs\DotNet\netstandard2.1\Aerotech.Automation1.DotNet.dll"
RUN_AUTOMATION1_SERVER_REAL.bat
```

원격 Automation1 Controller를 사용할 때는 Server Host 인자가 아니라 Server 실행 인자 `--controller=<Automation1 host>`로 지정한다. 공식 문서는 원격 Ethernet 연결에 `Controller.ConnectSecure` 사용을 권장하므로 실제 보안 설정과 인증서 정책에 맞춰 `Automation1ReflectionRuntime`을 typed SDK adapter로 교체하는 것이 운영 기준이다.

## 6. 클래스 책임

| 클래스 | 실행 위치 | 책임 |
| --- | --- | --- |
| `AeroScriptGenerator` | Client PC | 좌표와 조건으로 AeroScript 생성 |
| `AeroScriptPackage` | Client PC | Job ID, source, SHA-256, Task 묶음 |
| `AeroScriptClient` | Client PC | Upload/Run/Status TCP 요청 |
| `AeroScriptProtocol` | 양쪽 | 길이 프리픽스 JSON frame 직렬화 |
| `AeroScriptServer` | Server PC | 검증, spool, 실행 queue, 상태 관리 |
| `IAutomation1Runtime` | Server PC | Automation1 실행 추상화 |
| `SimulationAutomation1Runtime` | Server PC | SDK 없는 환경의 완료/오류 검증 |
| `Automation1ReflectionRuntime` | Server PC | 공식 .NET API DLL을 런타임 로드해 Controller 호출 |

## 7. 장비 적용 전에 확정할 항목

- MCD에 정의된 실제 Scanner Head별 `Gx/Gy` Axis 이름
- 사용할 Automation1 Task 번호와 다른 프로그램과의 Task 점유 규칙
- Scanner/Stage MOF 동기화 방식, PSO/IFOV/레이저 Trigger 명령과 Interlock 순서
- 축 Enable/Home 책임이 Server Script인지 장비 초기화 시퀀스인지
- Script 최대 좌표 수, Controller compile 시간, 실행 Timeout
- E-Stop, Door, Laser Ready, Scanner Ready, Stage In-position 확인 위치
- 중복 Job, 재시도, 전원 재기동 후 spool 복구 정책
- 실제 운영용 인증, TLS, 방화벽, 로그 보존 및 Recipe/Script 버전 추적 정책

현재 생성 Script는 좌표 전달과 Scanner 축 이동의 기본 구조를 보여준다. 레이저 조사 및 PSO/IFOV 명령은 장비별 안전 시퀀스가 확정되기 전에는 자동으로 삽입하지 않는다.

## 8. 공식 Automation1 근거

- [.NET API 개요](https://help.aerotech.com/automation1/Content/APIs/dotNET/Get-Started/Intro-to-dotNET.htm)
- [Controller.Connect 및 Runtime](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Controller-dotNET.htm)
- [Controller Files.WriteText/Upload](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Files-dotNET.htm)
- [Task Program.Run 및 TaskState](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Tasks-dotNET.htm)
- [AeroScript MoveLinear](https://help.aerotech.com/automation1/Content/Concepts/Motion-Functions.htm)
- [Controller File System](https://help.aerotech.com/automation1/Content/Controller-File-System.htm)

