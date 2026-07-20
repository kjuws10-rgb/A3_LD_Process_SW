# A3 Optical Component Driver Validation WPF

`manual` 폴더의 Laser, Attenuator, Motorized Beam Expander, Power Meter, XPS, Picomotor 자료를 분석하여 동일한 구조로 검증할 수 있게 만든 독립 실행형 .NET 8 WPF 예제입니다.

## 실행

1. `RUN_COMPONENT_DRIVER_VALIDATION.bat`를 실행합니다.
2. 상단에서 컴포넌트를 선택합니다.
3. 기본값인 `Simulation` 상태에서 연결합니다.
4. `선택 Component Read-only 검증` 또는 `전체 Component 통합 검증`을 실행합니다.
5. 실제 장비는 `Hardware`를 선택하고 endpoint를 입력하되, 먼저 read-only 명령만 확인합니다.

자동 검증은 `RUN_ALL_COMPONENT_TESTS.bat`로 실행합니다.

## 프로젝트

- `Equipment.Driver`: 6개 컴포넌트 공통 Profile, Command Catalog, Protocol, Serial/TCP Transport, Simulator, Driver
- `Equipment.Driver.Verification`: 전체 컴포넌트 자동 검증 8개
- `Talon.Driver`: Talon Rev.C 전용 상세 드라이버
- `Talon.Driver.Verification`: Talon 전용 자동 검증 6개
- `Talon.Driver.Wpf`: 전체 컴포넌트 통합 WPF 검증 UI
- `COMPONENT_DRIVER_FUNCTION_LIST.md`: 장비별 기능 및 protocol 목록
- `COMPONENT_THEORY_OPERATION_GUIDE.md`: 이론, 동작법, 안전, 실장비 검증 절차
- `UNIFIED_COMPONENT_DRIVER_ARCHITECTURE.svg`: 통합 구조와 데이터 흐름

## 중요 제한

- Simulator 검증은 protocol formatting, 범위 차단, 상태 갱신, 오류 전달을 확인합니다. 실제 광출력 정확도나 motion 방향은 보증하지 않습니다.
- Picomotor 첨부 파일은 명령 사전이 아니라 `CmdLib.dll` programming sample입니다. Reflection 기반 vendor adapter가 포함되어 있으며 실제 연결에는 제조사 DLL과 해당 모델 User Manual이 필요합니다.
- XPS의 `Group1`, stage database, travel limit, 축 수는 실제 controller 설정에 맞게 변경해야 합니다.
- 레이저 출력 명령은 교육받은 작업자, interlock, beam dump 또는 power meter가 준비된 통제 구역에서만 검증해야 합니다.
