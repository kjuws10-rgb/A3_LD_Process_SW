# Talon Laser Driver Validation WPF

Spectra-Physics `Talon Users Manual Rev. C 90065281`의 RS-232 및 Appendix A 명령 규칙을 기준으로 만든 독립형 드라이버 검증 프로젝트다. 기존 A3 LD 코드의 `C` 클래스, `EN_` enum, `ST_` record 명명법과 Build/Apply 계층 분리 방식을 유지했다.

## 안전 원칙

- 프로그램은 기본적으로 `Simulation` 모드로 시작한다.
- `Read-only 자동 검증`은 출력 상태를 바꾸지 않는 Query만 실행한다.
- Hardware 모드의 `ON`, 전류/PRF/QMODE 변경, shutter/gate open은 네 가지 안전 확인이 모두 선택되어야 한다.
- `OFF`, diode current 0, shutter close, gate close는 안전 잠금과 무관하게 실행할 수 있다.
- 이 프로그램의 안전 잠금은 소프트웨어 보조장치다. 실제 Laser Area Interlock, key switch, analog interlock와 shutter interlock을 대체하지 않는다.

## 실행

1. `.NET 8 SDK`가 설치된 PC에서 `RUN_TALON_DRIVER_VALIDATION.bat`를 실행한다.
2. 장비 없이 확인할 때는 `Simulation`을 유지하고 `Read-only 자동 검증`을 누른다.
3. 실제 장비는 `Hardware`, COM Port, Baud Rate, Timeout을 입력하고 연결한다.
4. Hardware 연결 직후에는 출력 명령보다 `Read-only 자동 검증`을 먼저 실행한다.
5. 자동 코드 검증은 `RUN_AUTOMATED_VERIFICATION.bat`로 실행한다.

`Packages`와 `NuGet.config`를 포함하므로 인터넷 연결 없이 `System.IO.Ports`를 복원할 수 있다.

## 프로젝트 구조

```text
Talon.Driver
  Models/TalonModels.cs              명령, 상태, 상태비트, 안전 Context
  Protocol/CTalonCommandCatalog.cs   매뉴얼 명령과 범위/위험도 Metadata
  Protocol/CTalonProtocol.cs         명령 생성, 응답 정규화와 Parsing
  Transport/ITalonTransport.cs       실제/Simulation 공통 통신 계약
  Transport/CSerialTalonTransport.cs RS-232 115200/N/8/1 구현
  Transport/CTalonSimulatorTransport.cs 장비 없는 상태/오류 시뮬레이터
  Services/CTalonDriver.cs           직렬화, 안전검사, Status 반영, Poll/검증

Talon.Driver.Wpf
  MainWindow.xaml                    Dark 운영 도구 UI
  MainWindow.xaml.cs                 연결, 명령, 검증, Trace 제어

Talon.Driver.Verification
  Program.cs                         외부 Test Package 없는 자동 회귀 검증
```

## WPF 기능

- Simulation / Hardware 전송부 전환
- COM Port, Baud Rate, Timeout 설정
- 매뉴얼 명령 카탈로그와 Parameter 범위 표시
- 명령 위험도별 실행 버튼 색상 구분
- System status, power, current, temperature 대시보드
- `*STB?` 상태비트 표시
- `?FH` Event Code 이력 표시
- Read-only 핵심 Query 14개 자동 검증
- Timeout 및 잘못된 응답 단발성 오류 주입
- TX/RX, 처리시간, 오류 메시지 Trace
- Hardware 출력/설정 명령 안전 잠금

## 실제 프로젝트 반영 권장안

기존 `CTalonLaser`를 즉시 교체하지 않고 다음 순서로 반영한다.

1. `CTalonProtocol`과 `CTalonCommandCatalog`의 명령 문자열 및 범위 검증을 기존 코드에 적용한다.
2. `?FH`를 단일 정수가 아닌 16개 Event Code 목록으로 상태 구조에 추가한다.
3. Query와 Set/Action을 분리하여 Set 명령에서 응답을 기다리지 않도록 한다.
4. CR/LF 어느 쪽이든 응답 종료로 처리하는 Talon 전용 Read 함수를 적용한다.
5. Simulation과 실제 COM Port에서 Read-only 검증을 통과시킨다.
6. Laser output 명령은 설비 Interlock Manager와 Station 조건을 결합한 뒤 운영 코드에 연결한다.

세부 매뉴얼 분석은 `TALON_MANUAL_PROTOCOL_ANALYSIS.md`, 구조도는 `TALON_DRIVER_ARCHITECTURE.svg`를 참고한다.
