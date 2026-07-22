# Automation1 직접 연결 AeroScript 운전 절차서

## 1. 목적

Client PC의 WPF가 선택 Scanner의 가공 가능 좌표로 MOF AeroScript를 생성하고, 공식 Aerotech Automation1 .NET API로 Server PC의 Controller에 직접 접속해 기록·실행·완료 확인·이력 보존까지 수행한다.

별도 `Automation1Server.exe`, 자체 Socket Server, JSON protocol, TCP 46100은 사용하지 않는다.

## 2. 시스템 구성

| 구분 | 설정 |
| --- | --- |
| Client PC | `192.168.10.200`, MOF Coordinate WPF 실행 |
| Server PC / Controller Host | `192.168.10.10` |
| Automation1 endpoint | TCP `12200` 기본값 |
| API | `Aerotech.Automation1.DotNet.dll` 2.13.1.3468 |
| 실행 Task | UI의 `Automation1 Task`, 기본 1 |
| Script 기록 | Controller File System `programs/*.ascript` |
| 결과 기록 | Controller File System `programs/mof_job_*.json` |

## 3. 사전 준비

1. Server PC에 Automation1 MDK/Runtime과 라이선스가 정상 설치되어 있어야 한다.
2. Server PC에서 대상 Controller 또는 Virtual Controller를 시작한다.
3. Client PC의 A1 Studio에서 `192.168.10.10` 원격 Controller 접속을 먼저 확인한다.
4. Client PC에서 `ping 192.168.10.10`과 `Test-NetConnection 192.168.10.10 -Port 12200`을 확인한다.
5. Server 방화벽과 장비망 정책에서 Automation1 endpoint를 허용한다.
6. Client와 Server의 Automation1 API/Runtime 호환 버전을 확인한다.
7. Access Control이 켜진 장비는 사용자 인증 연결 구현과 계정 정책을 별도로 확정해야 한다. 현재 UI 기본 모드는 Access Control 비활성 연결이다.

`Test-NetConnection` 성공은 TCP 도달성만 의미한다. 실제 protocol 준비 여부는 WPF의 `Automation1 직접 연결` 또는 A1 Studio 접속으로 확인한다.

## 4. 빌드와 실행

```powershell
dotnet build MOF_COORDINATE_WPF_SAMPLE.sln -c Release -p:Platform=x64
dotnet run --project MofCoordinateDemo.csproj -c Release -p:Platform=x64
```

프로젝트는 `etc/downloadable/a1참고자료`에서 검증해 복사한 `Automation1Sdk`의 공식 DLL을 참조한다. 실행 결과 폴더에는 DotNet/Communication/Compiler DLL이 함께 복사된다.

## 5. UI 운전 순서

1. 좌표 메뉴에서 기판, AK1, Cell, Scanner 물리 배치, Process Area를 설정한다.
2. Scanner 아이콘을 클릭한다. 선택 Head의 `InField=true` 가공 좌표 전체가 자동 선택된다.
3. 필요하면 Matrix에서 좌표를 추가 선택하거나 해제한다.
4. Script Mode를 선택한다.
5. `Controller Host=192.168.10.10`, `Controller Port=12200`, Task 번호를 확인한다.
6. `1. Script 생성`을 누른다. Local Script File에 `.ascript`가 즉시 저장된다.
7. `Automation1 직접 연결`을 눌러 IsRunning과 Task Count를 확인한다.
8. `2. Controller 기록`을 누른다. Script와 최초 Audit JSON이 Controller File System에 기록된다.
9. `3. Task 실행`을 누른다. `Runtime.Tasks[n].Program.Run(controllerFile)`이 호출된다.
10. `상태 조회`로 현재 TaskState를 확인하거나 `전체 실행 및 완료 대기`로 250 ms 자동 조회한다.
11. 비정상 상황에서는 `실행 중지`를 눌러 `Task.Program.Stop(5000)`을 요청한다.

## 6. 코드와 API 대응

| 단계 | 코드 | Automation1 API |
| --- | --- | --- |
| 직접 연결 | `Automation1DirectClient.ConnectAsync` | `Controller.Connect(host, port)` |
| Controller 시작 | 옵션 사용 시 | `Controller.Start()` |
| Script 기록 | `UploadAsync` | `Controller.Files.WriteText(file, source)` |
| Task 실행 | `RunAsync` | `Controller.Runtime.Tasks[n].Program.Run(file)` |
| 상태 확인 | `GetStatusAsync` | `Controller.Runtime.Tasks[n].Status` |
| 실행 중지 | `StopAsync` | `Task.Program.Stop(5000)` |
| 결과 기록 | `AppendAuditEvent` | `Controller.Files.WriteText(auditFile, json)` |

## 7. 완료 및 오류 판정

| TaskState | WPF 판정 | 의미 |
| --- | --- | --- |
| `ProgramRunning` | Running | Script 실행 중 |
| `ProgramFeedhold` | Running | Feedhold 상태 |
| `ProgramPaused` | Running | 일시 정지 상태 |
| `ProgramComplete` | Completed | 정상 완료 |
| `Error` | Failed | Controller/Script 오류 |
| 실행 후 `Idle`, `Inactive`, `ProgramReady` | Failed | 완료 전 비정상 이탈 |

Task Run 직후 상태 반영 지연을 고려해 2초 전환 유예를 둔다. 정상 완료는 반드시 `ProgramComplete`로 확인한다.

## 8. Controller 이력 파일

`Job별 Controller Script 보존`이 켜져 있으면 Script 파일명에 UTC 생성시각과 Job ID 앞 8자리가 붙는다.

```text
programs/mof_generated_20260722_143012_a1b2c3d4.ascript
programs/mof_job_20260722_143012_a1b2c3d4.json
```

Audit JSON에는 Job ID, Host/Port, Script 파일, Task 번호, 좌표 수, 생성 모드, SHA-256, 생성/갱신 UTC, 최종 상태와 상태 변경 이벤트가 기록된다. 이 파일은 Windows `D:\` 경로가 아니라 Automation1 Controller File System의 파일이다.

## 9. 장애 점검

| 증상 | 점검 |
| --- | --- |
| 연결 거부 | Server IP, Runtime/Controller 시작, TCP 12200 방화벽 확인 |
| 원격 호스트 강제 종료 | 일반 Socket/JSON을 12200에 보내지 않았는지 확인; 공식 API 사용 |
| DLL 로드 오류 | x64 빌드, 2.13.1 DLL과 Compiler DLL 출력 폴더 존재 확인 |
| Controller not running | Server에서 시작하거나 승인 후 `정지 시 시작` 사용 |
| Task 범위 오류 | 연결 로그의 Task Count와 UI Task 번호 비교 |
| File write 오류 | Controller 경로와 폴더 존재, 사용자 권한, 저장 공간 확인 |
| Task busy | 기존 Program/Queue 종료 후 재실행 |
| 즉시 Error | A1 Studio Task Error, Script 줄 번호, 축 이름, Parameter 지원 여부 확인 |

## 10. 장비 적용 전 필수 확인

- Virtual Mode에서 Laser, PSO, Hardware Aux, Galvo calibration은 검증하지 않는다.
- Hardware Coordinate Program은 실제 축 이동을 발생시킬 수 있다.
- GX/GY/Y 축 이름, 단위, Ramp, Software Limit, MOF Pair, Interlock, Laser 안전 회로를 장비 담당자와 확인한다.
- Controller Task를 A1 Studio와 WPF가 동시에 실행하지 않도록 운전권 정책을 정한다.
- 운영 환경에서는 Access Control, 계정 권한, 인증서, 감사 파일 보존·삭제 정책을 확정한다.
