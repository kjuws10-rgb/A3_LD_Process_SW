# Client PC 생성 AeroScript의 Server PC 전송·실행·상태확인 절차서

문서 기준일: 2026-07-21  
대상 프로젝트: `MOF_COORDINATE_WPF_SAMPLE`  
대상 Branch: `codex/automation1-script-server-workflow`

## 1. 목적

Client PC에서 좌표를 계산하고 생성한 `.ascript`를 Server PC로 전달한 뒤, Server PC가 Automation1 Controller File System에 저장하고 지정 Task에서 실행하며, Client PC가 완료 또는 오류를 확인하는 전체 절차를 정의한다.

이 문서는 다음 두 검증 단계를 구분한다.

1. `Simulation runtime`: TCP, 인증, Upload, Run, Status protocol만 검증한다. 실제 AeroScript와 Controller는 실행하지 않는다.
2. `Automation1 runtime`: Server PC의 공식 Automation1 .NET API를 통해 실제 또는 Virtual Controller에 파일을 기록하고 Task를 실행한다.

## 2. 코드 검토 결론

| 검토 항목 | 판정 | 코드 동작 |
| --- | --- | --- |
| Client Script 생성 | 적합 | 좌표와 선택 Head를 기반으로 Client에서만 Script 생성 |
| Client 로컬 파일 저장 | 적합 | UTF-8 BOM 없이 실제 `.ascript` 파일 저장 |
| 전송 무결성 | 적합 | Script UTF-8 바이트의 SHA-256을 Client/Server에서 각각 계산해 비교 |
| TCP Frame | 적합 | 4-byte Big Endian 길이 + JSON payload, 최대 Frame 제한 적용 |
| 인증 | 검증용 적합 | 고정시간 API Key 비교. 단, 평문 TCP이므로 폐쇄 장비망 전제 |
| 포트 분리 | 적합 | `46100` Script Gateway와 `12200` Automation1 native endpoint 분리 |
| Server 임시 보관 | 적합 | Controller 전송 전에 Job ID별 spool `.ascript` 저장 |
| Controller 파일 기록 | 공식 API와 일치 | `Controller.Files.WriteText(controllerFileName, scriptText)` 사용 |
| Task 실행 | 공식 API와 일치 | `Controller.Runtime.Tasks[taskIndex].Program.Run(fileName)` 사용 |
| Task index | 적합 | Automation1 규칙에 따라 1 이상만 허용 |
| 완료 확인 | 적합 | Task Status snapshot을 100 ms 간격으로 읽고 `ProgramComplete` 확인 |
| 오류 확인 | 적합 | `Error` 또는 예상하지 못한 TaskState를 `Failed`로 변환 |
| 동시 실행 | 검증용 적합 | Server의 단일 Semaphore로 Automation1 실행을 직렬화 |
| 실행 제한시간 | 적합 | 기본 30분 후 취소하고 가능한 경우 Task Stop 수행 |
| Server 재시작 복구 | 제한 | spool 파일은 남지만 Job Dictionary는 메모리 기반이므로 Status 이력은 소실 |
| 운영 보안 | 추가 필요 | TLS, 인증서, Job 영속화, 감사로그, 재시도/멱등성 정책 필요 |

결론적으로 현재 코드는 기능검증과 장비 연동 Base 코드로 사용할 수 있다. 다만 실제 축, Laser, PSO, Interlock이 포함된 Hardware 실행은 장비 안전 시퀀스와 Automation1 버전을 확인한 후 별도 승인해야 한다.

## 3. 통신 구조와 포트

```text
Client PC 192.168.10.200
  MOF WPF
  ├─ 좌표 계산 및 AeroScript 생성
  ├─ Local Script File 저장
  └─ TCP 46100 / Protocol v3
           │
           ▼
Server PC 192.168.10.10
  Automation1Server.exe
  ├─ API Key / Protocol / SHA-256 / ModePolicy 검증
  ├─ script-spool\{JobId}.ascript 저장
  └─ Automation1ReflectionRuntime
           │ 공식 Automation1 .NET API
           ▼
Automation1 Controller native endpoint 12200
  ├─ Controller.Files.WriteText("programs/mof_generated.ascript", source)
  ├─ Runtime.Tasks[1].Program.Run(...)
  └─ Task.Status polling
```

| 구분 | 주소 | 사용 주체 | Protocol |
| --- | --- | --- | --- |
| Script Gateway | `192.168.10.10:46100` | Client WPF ↔ Automation1Server | 프로젝트 protocol v3 |
| Automation1 Controller | `192.168.10.10:12200` | Server의 Automation1 .NET API | Automation1 native protocol |

Client WPF의 `Script Gateway Port`에는 반드시 `46100`을 입력한다. `12200`은 직접 입력하지 않는다.

## 4. 코드별 책임

| 파일/클래스 | 책임 |
| --- | --- |
| `MainWindow.GenerateCurrentAeroScriptPackage` | 좌표 필터, Script 생성, Local 파일 저장, Package 생성 |
| `AeroScriptGenerator` | Virtual/Hardware 모드별 AeroScript source 생성 |
| `AeroScriptLocalFileStore` | Client `.ascript` 실제 저장 |
| `AeroScriptPackage` | Job ID, Controller 경로, source, SHA-256, Task, Mode 전달 |
| `AeroScriptClient` | Health, Upload, Run, Status 요청 |
| `AeroScriptProtocol` | 길이 프리픽스 JSON Frame 송수신 |
| `AeroScriptServer` | 인증, 검증, spool, Job 상태, 실행 직렬화 |
| `Automation1ReflectionRuntime` | 공식 .NET API DLL 로드, Controller 연결, 파일 기록, Task 실행/상태 확인 |
| `SimulationAutomation1Runtime` | Controller 없이 protocol과 Job 상태만 모사 |

## 5. 사전 준비 체크리스트

### 5.1 공통

- Client IP가 `192.168.10.200`인지 확인한다.
- Server IP가 `192.168.10.10`인지 확인한다.
- 양쪽 Subnet Mask와 Gateway가 장비망 정책에 맞는지 확인한다.
- Client와 Server의 프로젝트/Protocol 버전이 같은지 확인한다.
- Client와 Server에서 같은 API Key를 사용한다.
- Windows 시간이 크게 어긋나지 않게 동기화한다.

### 5.2 Server PC

- Automation1-MDK 설치 및 라이선스 인증 완료
- `Aerotech.Automation1.DotNet.dll` 설치 확인
- Automation1 Controller 또는 Virtual Controller 구성 완료
- MCD에 `Y`, `GX`, `GY` 또는 실제 적용 Axis 이름 존재
- 사용할 Task index가 존재하며 다른 Program이 점유하지 않음
- TCP `46100` 인바운드 방화벽 허용
- `12200` Automation1 native endpoint 동작 확인

### 5.3 Hardware 실행 추가 조건

- E-Stop, Door, Laser Ready, Scanner Ready 상태 확인
- 축 Homing 및 Software Limit 확인
- Laser/PSO/HW Aux 명령 승인
- GX/GY와 Stage Y의 MOF Pair 및 Encoder 설정 확인
- Dry Run 또는 Laser 출력 차단 상태에서 좌표 검증 완료

## 6. Server 배포 절차

### 6.1 배포본 생성

소스가 있는 개발 PC에서 다음 파일을 실행한다.

```bat
PUBLISH_AUTOMATION1_SERVER_WIN64.bat
```

생성 폴더:

```text
ServerDeployment\win-x64
```

이 폴더 전체를 Server PC `192.168.10.10`으로 복사한다. 일부 DLL이나 BAT만 선택 복사하지 않는다.

### 6.2 방화벽 설정

Server PC에서 다음 파일을 관리자 권한으로 실행한다.

```bat
OPEN_SERVER_FIREWALL_PORT_46100_ADMIN.bat
```

PowerShell 확인 명령:

```powershell
Get-NetFirewallRule -DisplayName "A3 Automation1 Script Server TCP 46100"
Get-NetTCPConnection -LocalPort 46100 -State Listen
```

두 번째 명령은 Server를 실행한 뒤 확인한다.

### 6.3 Server 실행

Server PC 배포 폴더에서 실행한다.

```bat
START_AUTOMATION1_SERVER.bat
```

1. Client WPF와 동일한 API Key를 입력한다.
2. `Virtual Wait Simulation` 검증은 `1. VirtualOnly`를 선택한다.
3. 실제 `Hardware Coordinate Program`은 안전승인 후 `2. HardwareOnly`를 선택한다.
4. Server 창을 닫지 않는다.

정상 Console 예시:

```text
Automation1 Script Server: 0.0.0.0:46100
Runtime: automation1, ModePolicy: VirtualOnly
이 TCP Port는 Client WPF용 Script Gateway입니다.
```

DLL 자동 탐색 실패 시 환경 변수를 지정한다.

```powershell
$env:AUTOMATION1_DOTNET_DLL="C:\Program Files\Aerotech\Automation1-MDK\APIs\DotNet\...\Aerotech.Automation1.DotNet.dll"
```

그 후 같은 PowerShell에서 Server를 다시 실행한다.

## 7. 네트워크 검증 절차

Client PC에서 실행한다.

```bat
CHECK_SERVER_PORT_FROM_CLIENT.bat
```

또는 PowerShell에서 개별 확인한다.

```powershell
Test-NetConnection 192.168.10.10 -Port 12200
Test-NetConnection 192.168.10.10 -Port 46100
```

판정 기준:

- `12200=True`, `46100=False`: Automation1만 실행 중이고 Script Gateway가 실행되지 않은 상태
- `12200=True`, `46100=True`: 실제 연동을 진행할 수 있는 기본 상태
- `12200=False`, `46100=True`: Gateway는 실행 중이나 Controller 연결 HealthCheck가 실패할 가능성이 큼
- 둘 다 `False`: IP, 방화벽, Cable, VLAN부터 점검

## 8. Client WPF 설정

`Automation1 Script I/F` 탭에서 설정한다.

| UI 항목 | Virtual 검증 예시 | 설명 |
| --- | --- | --- |
| Script Mode | `Virtual Wait Simulation` | Laser/PSO 없이 Wait 흐름 검증 |
| Server PC Host | `192.168.10.10` | Automation1Server가 실행되는 PC |
| Script Gateway Port | `46100` | `12200` 사용 금지 |
| API Key | Server와 동일 | 대소문자 포함 완전 일치 |
| Automation1 Task | `1` | Task index는 1부터 시작 |
| Controller File | `programs/mof_generated.ascript` | Controller File System 경로, `/` 사용 |
| Local Script File | Client의 `.ascript` 경로 | Client 디스크에 실제 생성되는 파일 |
| Stage/GX/GY Axis | `Y`, `GX`, `GY` | MCD Axis 이름과 일치 |

Mode 조합은 반드시 일치해야 한다.

| Client Mode | Server ModePolicy | 결과 |
| --- | --- | --- |
| VirtualWaitSimulation | VirtualOnly | 허용 |
| HardwareCoordinateProgram | HardwareOnly | 허용 |
| VirtualWaitSimulation | HardwareOnly | `MODE_POLICY_REJECTED` |
| HardwareCoordinateProgram | VirtualOnly | `MODE_POLICY_REJECTED` |

## 9. 단계별 실행 절차

### 단계 1. Server 연결 확인

Client에서 `Server 연결 확인`을 누른다.

이 버튼은 다음을 순서대로 점검한다.

1. `192.168.10.10:46100` TCP 연결
2. Protocol v3 Frame 송수신
3. API Key 일치
4. Server ModePolicy 확인
5. Automation1 .NET API DLL 로드
6. Controller 연결
7. Controller 실행 여부와 Task 개수 확인

정상 예시:

```text
[연결 확인] Script Gateway 192.168.10.10:46100 TCP 접속 시도
[연결 확인] Success=True
Gateway ready ... Automation1 runtime ready.
Controller=127.0.0.1:12200, IsRunning=True, Tasks=...
```

`Simulation runtime ready`가 표시되면 실제 Controller 검증이 아니라 protocol 모사 Server에 연결된 것이다.

### 단계 2. Scanner 및 좌표 선택

1. Board에서 사용할 Scanner Head를 선택한다.
2. 선택 Scanner의 가공 가능 좌표가 전체 선택되는지 확인한다.
3. Matrix에서 Process Gx/Gy와 Review 좌표를 확인한다.
4. `선택 좌표만`을 사용할 경우 선택 좌표 개수를 확인한다.

### 단계 3. Script 생성

`1. Script 생성`을 누른다.

확인 항목:

- Script Preview에 `program ... end`가 존재
- Local Script File이 실제 생성됨
- 로그에 Job ID, 좌표 수, Mode, SHA-256, Task 표시
- Script의 `G0 GX/GY` 또는 `MoveLinear` 값이 Process 좌표와 일치
- Review 좌표는 주석에만 표시되고 Scanner 명령값으로 사용되지 않음

Client 파일 확인:

```powershell
Test-Path "표시된 Local Script File 전체경로"
Get-FileHash "표시된 Local Script File 전체경로" -Algorithm SHA256
```

### 단계 4. Server 전송

`2. Server 전송`을 누른다.

Server가 수행하는 검증:

1. Protocol version
2. API Key
3. Job ID 형식
4. Task index
5. Controller File 확장자와 경로
6. ModePolicy
7. Script 크기
8. SHA-256

정상 Job 상태:

```text
Uploaded: Server PC에 script 저장 완료
```

주의: `Uploaded`는 Server spool 저장 완료를 뜻한다. 아직 Controller File System에 기록되거나 Task에서 실행된 상태는 아니다.

Server spool 예시:

```text
script-spool\{JobId}.ascript
```

### 단계 5. Server 실행

`3. Server 실행`을 누른다.

Server 내부 상태 순서:

```text
Uploaded
  -> Queued
  -> TransferringToController
  -> Running / TaskState=ProgramRunning
  -> Completed / TaskState=ProgramComplete
```

Server Console에는 API Key를 제외하고 Remote endpoint, Request 종류, Job ID, 성공 여부, Job State, Error Code가 기록된다. 반복 Status 요청은 State/Message가 변경될 때만 기록한다.

실행 시 실제 API 호출:

```text
Controller.Files.WriteText("programs/mof_generated.ascript", source)
Controller.Runtime.Tasks[1].Program.Run("programs/mof_generated.ascript")
```

`Program.Run`은 Program이 시작될 때까지만 기다린다. 완료 여부는 다음 단계의 Status Polling으로 판단한다.

### 단계 6. 상태 조회

`상태 조회`를 반복해서 누르거나 `전체 실행 및 완료 대기`를 사용한다.

자동 Polling은 250 ms 간격으로 계속 수행하지만 Client 로그는 Job State 또는 Message가 변경될 때만 추가된다. 실제 Controller TaskState 변경은 Server Job Message에 반영된다.

| Server Job State | 의미 |
| --- | --- |
| Uploaded | Server spool 저장 완료 |
| Queued | 실행 Semaphore 대기 |
| TransferringToController | Controller.Files.WriteText 수행 중 |
| Running | Task 실행 중, Message에 실제 TaskState 표시 |
| Completed | `TaskState=ProgramComplete` 확인 |
| Failed | Compile, Controller, Task, Timeout, 비정상 TaskState 오류 |
| Rejected | 정책 또는 검증 단계에서 거부 |

정상 완료 기준은 반드시 다음 두 조건을 만족해야 한다.

1. Client Job State가 `Completed`
2. 마지막 Server Message가 Automation1 AeroScript 실행 완료

단순히 `RunScript 요청 성공` 또는 `Queued`만으로 완료 판정하지 않는다.

### 단계 7. 전체 자동 실행

`전체 실행 및 완료 대기`는 다음을 자동 수행한다.

```text
Generate
  -> Upload
  -> Run
  -> 250 ms 간격 GetStatus
  -> Completed 또는 Failed/Rejected까지 대기
```

Hardware Mode에서는 실행 전 경고창에서 축, Limit, Interlock, Laser 안전 상태를 다시 확인한다.

## 10. Automation1 Studio 교차검증

Server 실행 후 Studio에서 다음을 확인한다.

1. Controller Files에 `programs/mof_generated.ascript` 존재
2. 파일 내용과 Client Preview가 일치
3. 지정 Task에 같은 Source File이 표시
4. 실행 중 TaskState가 `ProgramRunning`
5. 정상 종료 후 `ProgramComplete`
6. Error 발생 시 Task Error 내용과 Client `Failed` Message가 일치

Controller File은 같은 이름으로 실행할 때 덮어쓴다. Recipe/Job 추적이 필요하면 파일명에 Recipe ID 또는 Job ID를 포함하는 운영 정책을 추가한다.

## 11. 오류별 조치

| 오류/로그 | 원인 | 조치 |
| --- | --- | --- |
| `Connection refused` | 46100 Listener 없음/방화벽 | Server 실행, 방화벽, `Get-NetTCPConnection` 확인 |
| `원격 호스트가 강제로 끊음` | 12200 등 다른 Protocol 접속 | WPF Port를 46100으로 설정 |
| `UNAUTHORIZED` | API Key 불일치 | Client/Server Key 완전 일치 확인 |
| `PROTOCOL_VERSION` | Client/Server 배포본 불일치 | 동일 ZIP으로 양쪽 교체 |
| `RUNTIME_NOT_READY` | DLL 누락, Controller 연결 실패 | DLL 환경변수, MDK, 12200, Controller 상태 확인 |
| `MODE_POLICY_REJECTED` | Client Script Mode와 Server Policy 불일치 | VirtualOnly/HardwareOnly 조합 수정 |
| `HASH_MISMATCH` | 전송 내용 변경/손상 | Script 재생성 후 재전송, 중간 편집 금지 |
| `INVALID_TARGET` | Task 또는 Controller 경로 오류 | Task 1 이상, `.ascript`, `/` 경로 확인 |
| `DUPLICATE_JOB` | 같은 Job ID 재전송 | Script를 다시 생성해 새 Job ID 사용 |
| `JOB_NOT_FOUND` | Server 재시작 또는 다른 Server | 새로 Generate→Upload 수행 |
| DLL을 찾을 수 없음 | Automation1 .NET API 탐색 실패 | `AUTOMATION1_DOTNET_DLL` 지정 |
| Program compile 오류 | 생성 Script와 Controller 버전/Axis 불일치 | Server Failed Message와 Studio Compiler 오류 확인 |
| 예상하지 못한 TaskState | 외부 Stop, Task 충돌, 프로그램 중단 | Studio Task 상태와 외부 제어 주체 확인 |
| 실행 제한시간 초과 | Wait 미해제/축 정지/Program hang | Stage feedback, Wait 조건, Axis/MOF 설정 확인 |

## 12. 중지 및 복구

1. Hardware 이상 시 장비 E-Stop과 장비 안전 절차를 우선한다.
2. Client 대기 취소는 Client Polling만 중단할 수 있으므로 Controller Task 상태를 반드시 확인한다.
3. Server 실행 Timeout이 발생하면 Runtime이 `Program.Stop(5000)`을 시도한다.
4. Server를 강제 종료했다면 Studio에서 Task와 Axis 상태를 확인한 후 재시작한다.
5. Server 재시작 후 기존 Job Status는 복구되지 않으므로 Script를 새로 생성하고 Upload한다.

## 13. 검증 기록 양식

```text
검증 일시:
Client PC/IP:
Server PC/IP:
Client Commit/Version:
Server Commit/Version:
Automation1/MDK Version:
Controller Name/Port:
Mode: VirtualWaitSimulation / HardwareCoordinateProgram
ModePolicy: VirtualOnly / HardwareOnly
Task Index:
Controller File:
Job ID:
SHA-256:
Target Count:
HealthCheck 결과:
Upload 결과:
Run 결과:
최종 TaskState:
최종 Job State:
Studio 교차검증 결과:
검증자:
비고:
```

## 14. 운영 적용 전 추가 개발 권고

- TLS 또는 장비망 전용 보안 Channel
- API Key의 Windows Credential/Secret Store 관리
- Job/상태/로그 Database 영속화
- Client 재시도와 Request ID 기반 멱등성
- Server Windows Service 등록 및 자동 재시작
- Controller Task 전용 예약 및 외부 제어 충돌 방지
- Recipe ID, Script SHA-256, 측정/보정 이력 연결
- Hardware Interlock API와 Laser Enable 승인 단계
- 실제 Automation1 SDK를 직접 참조하는 typed adapter와 버전 고정

## 15. 공식 Automation1 근거

- [.NET API Controller 연결](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Controller-dotNET.htm)
- [Controller Files.WriteText](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Files-dotNET.htm)
- [Tasks, Task index 및 Status](https://help.aerotech.com/automation1/Content/APIs/dotNET/References/Tasks-dotNET.htm)
- [Controller File System 경로 규칙](https://help.aerotech.com/automation1/Content/Controller-File-System.htm)
- [TaskState와 Program.Run 완료 확인](https://help.aerotech.com/automation1/Content/APIs/C/References/Tasks-C.htm)
- [Automation1 Console과 native endpoint](https://help.aerotech.com/automation1/Content/Automation1-Console.htm)
