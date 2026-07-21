# A3 LD Process SW Architecture

이 문서는 현재 솔루션 구조를 기준으로 작성한 아키텍처/데이터 구조 설명이다. 클래스, 구조체, record, enum, interface의 전체 필드/프로퍼티/메서드 목록은 같은 폴더의 `CLASS_INVENTORY.md`에 빌드 결과 기준으로 전수 추출되어 있다.

## 1. Solution Layer

```text
Drilling.sln
├─ Drilling.UI       WPF 실행 앱, 화면/메뉴/ViewModel 계층
├─ Drilling.File     CSV/설정/레시피/제품/스크립트 파일 입출력 계층
└─ Drilling.Common   장비, 모션, 공정, 제품, 알람, 인터락 도메인 계층
```

의존 관계는 다음과 같다.

```text
Drilling.UI
├─ references Drilling.Common
└─ references Drilling.File

Drilling.File
└─ references Drilling.Common

Drilling.Common
├─ System.IO.Ports 8.0.0
└─ ACS.SPiiPlusNET7.dll
```

`Drilling.Common`은 도메인 인터페이스와 데이터 모델을 정의한다. `Drilling.File`은 그 인터페이스 구현체로 CSV와 runtime 파일을 읽고 쓴다. `Drilling.UI`는 WPF 화면을 구성하고 `Common`의 manager API를 호출한다.

## 2. Startup Composition

실행 진입 구조는 다음 순서다.

```text
CApp / App.xaml
└─ CAppStartup.CreateMainViewModel()
   ├─ Config root 탐색: Drilling.sln 기준 Config 폴더
   ├─ 파일 구현체 생성
   │  ├─ CIpsRecipeFile
   │  ├─ CSettingFile
   │  ├─ CManualScanFile
   │  ├─ CInterfaceFile
   │  ├─ CBETFile
   │  ├─ CPowerMeterFile
   │  ├─ CMotorFile
   │  ├─ CIoFile
   │  ├─ CProductFile
   │  ├─ CLogManager
   │  ├─ CAutomation1ScriptFile
   │  └─ CConfigStructureFile
   ├─ CManager 생성
   ├─ CManager.Initialize() 비동기 실행
   └─ CRootView 생성 후 WPF DataContext로 사용
```

`CManager`는 이 프로젝트의 composition root 역할을 한다. 설정 파일을 검증하고, 인터페이스/모션/제품/스테이션/레시피/설정 manager를 조립한 뒤 UI에 필요한 facade를 제공한다.

## 3. Runtime Object Hierarchy

```text
CManager
├─ _configRoot
├─ file adapters
│  ├─ _recipeFile: IRecipeFile
│  ├─ _settingFile: ISettingFile
│  ├─ _manualScanFile: IManualScanFile
│  ├─ _interfaceFile: IInterfaceFile
│  ├─ _betFile: IBETFile
│  ├─ _powerMeterFile: IPowerMeterFile
│  ├─ _motorFile: IMotorFile
│  ├─ _ioFile: IIoFile
│  ├─ _productFile: IProductFile
│  ├─ _logManager: ILogManager
│  └─ _automationScriptFile: IAutomationScriptFile
├─ domain managers
│  ├─ _interfaceManager: CInterfaceManager
│  ├─ _motionManager: CMotionManager
│  ├─ _alarmManager: CAlarmManager
│  ├─ _interLockManager: CInterLockManager
│  ├─ _stationManager: CStationManager
│  ├─ _recipeManager: CRecipeManager
│  ├─ _settingManager: CSettingManager
│  └─ _productManager: CProductManager
└─ startup tracking
   ├─ _startupMessages
   ├─ _startupSteps
   ├─ _loadedInterfaceCount
   ├─ _loadedMotorCount
   ├─ _loadedIoCount
   └─ _activeProductLoaded
```

UI 쪽 root는 다음처럼 manager를 화면 메뉴에 분배한다.

```text
CRootView
├─ IStationManager
├─ IInterfaceManager
├─ IMotionManager
├─ CAlarmManager
├─ CInterLockManager
├─ IRecipeManager
├─ ISettingManager
├─ IProductManager
├─ HeaderStatusItems / FooterStatusItems
├─ CurrentScreen: CScreenViewModel
└─ _menus
   ├─ CMenuMain
   ├─ CMenuManual
   ├─ CMenuRecipe
   ├─ CMenuSetting
   ├─ CMenuAlarm
   ├─ CMenuMonitor
   ├─ CMenuPm
   └─ CMenuExit
```

`CRootView`는 현재 메뉴 선택, 선택 Head, 선택 Recipe, 선택 Setting tab/group, 선택 Monitor tab, PM lock 상태를 보관한다. 각 menu class는 `Build()`에서 화면 DTO인 `CScreenViewModel`을 만든다.

## 4. Config And Runtime Data

```text
Config
├─ IPS_RCP.csv              Recipe form schema
├─ RECIPE/*.csv              Recipe value files
├─ IPS_SETTING.csv          Setting form schema
├─ Setting/Setting.csv       Setting values
├─ IPS_INTERFACE.csv        장비 통신 정의
├─ IPS_MOTOR.csv            축/모션 정의
├─ IPS_IO.csv               IO 정의
├─ IPS_BET.csv              BET 기본 테이블
├─ BET/BET.csv               BET 사용자 값
├─ IPS_POWERMETER.csv       Power meter 기본 테이블
├─ PowerMeter/*.pwm          Power meter process value
├─ IPS_MANUAL_SCAN.csv      Manual scan form schema
└─ Manual/*.scan             Manual scan setting value

Runtime generated
├─ Data/Script/PROCESS.ascript
├─ Data/Product/ActiveProduct.csv
├─ Data/Product/History/yyyyMMdd/ProductHistory_yyyyMMdd.csv
└─ Log
   ├─ Recipe/yyyyMMdd/Recipe_yyyyMMdd_Trace.txt
   ├─ Setting/yyyyMMdd/Setting_yyyyMMdd_Trace.txt
   ├─ Interface/yyyyMMdd/<MODULE>_yyyyMMdd_Trace.txt
   ├─ Station/yyyyMMdd/Station_yyyyMMdd_Trace.txt
   └─ Product/yyyyMMdd/Product_yyyyMMdd_Trace.txt
```

`CConfigStructureFile`이 필수 CSV와 헤더를 검증한다. `CCsvParser`는 공통 CSV read/write, header 검증, type 변환, extra column 보존을 담당한다.

## 5. Main Data Model Hierarchy

공정 데이터는 다음 계층으로 흐른다.

```text
ST_RECIPE_DATA
├─ Id
├─ Name
├─ Parameters: IReadOnlyList<ST_RECIPE_PARAM>
└─ History: IReadOnlyList<ST_RECIPE_HISTORY>

CRootView.CreateProcessParameters()
└─ Dictionary<string,string>

ST_PROCESS_PLAN
├─ ProcessId
├─ RecipeId
├─ ProductId
├─ PanelId
├─ LotId
├─ CreatedAt
└─ Parameters

CStationProcess.BuildProcessModel()
└─ ST_PROCESS_MODEL
   ├─ Plan: ST_PROCESS_PLAN
   ├─ Product: ST_PRODUCT_DATA?
   ├─ Heads: IReadOnlyList<ST_HEAD_PROCESS_DATA>
   │  └─ ST_HEAD_PROCESS_DATA
   │     ├─ HeadNo
   │     ├─ Use
   │     ├─ Shape
   │     ├─ LaserPower
   │     ├─ FrequencyKhz
   │     ├─ ShotCount
   │     ├─ MarkSpeed / JumpSpeed
   │     ├─ OffsetX / OffsetY
   │     └─ Path: IReadOnlyList<ST_PATH_POINT>
   ├─ Parameters
   └─ CreatedAt

CAutomation1ScriptFile.Build()
└─ ST_AUTOMATION1_SCRIPT
   ├─ FileName
   ├─ FilePath
   ├─ Lines
   ├─ TotalPoints
   ├─ HeadCount
   └─ CreatedAt
```

제품 데이터는 공정 모델과 연결된다.

```text
ST_PRODUCT_DATA
├─ ProductId / PanelId / LotId
├─ ProcessId / RecipeId
├─ State: EN_PRODUCT_STATE
├─ Result: EN_PRODUCT_RESULT
├─ CreatedAt / StartedAt / CompletedAt
├─ Parameters
└─ Heads: IReadOnlyList<ST_PRODUCT_HEAD_RESULT>
   └─ HeadNo, State, TotalPoints, CompletedPoints, Result, ErrorCode, Message, timestamps
```

장비 상태 데이터는 UI header, alarm, interlock, monitor 화면에서 공유된다.

```text
ST_DEVICE_STATUS
├─ Io: IReadOnlyList<ST_IO_STATUS>
├─ Motors: IReadOnlyList<ST_MOTOR_AXIS_STATUS>
├─ Laser: ST_LASER_STATUS
├─ Chiller: ST_CHILLER_STATUS
├─ Attenuator: ST_ATTENUATOR_STATUS
├─ Bet: ST_BET_STATUS
└─ PowerMeter: ST_POWER_METER_STATUS

ST_SYSTEM_STATUS
├─ CurrentRecipeId
├─ OperationMode: EN_SYSTEM_MODE
├─ AlarmState: EN_ALARM_STATE
├─ PMLockState: EN_PM_LOCK_STATE
└─ Modules: IReadOnlyList<ST_DEVICE_COMM_STATUS>
```

## 6. Process Flow

```text
Operator Start
└─ CRootView.StartCycle()
   ├─ PrepareInitialProcessPlan()
   │  ├─ IRecipeManager.LoadRecipes()
   │  ├─ ST_PROCESS_PLAN 생성
   │  └─ IStationManager.PrepareProcessPlan()
   └─ IStationManager.Start()
      └─ CStationProcess.Start()
         ├─ PLAN: 이미 준비된 plan 확인
         ├─ INTERLOCK: CInterLockManager.Evaluate(ST_DEVICE_STATUS)
         ├─ PARAMETER: process parameter load log
         ├─ DEVICE: IInterfaceManager.GetCommunicationStatus()
         ├─ SCRIPT: IAutomationScriptFile.Build(ST_PROCESS_MODEL)
         ├─ TASK: product running 반영, 실제 장비 실행부는 현재 simulation 메시지
         ├─ WAIT_DONE: task done 대기 simulation
         └─ COMPLETE: product/head result 저장, station/interface log 기록
```

현재 `StartAutomationTask()`는 "Real equipment run is not connected yet" 로그를 남기는 simulation 흐름이다. 실제 Automation1 task execute는 아직 연결되지 않은 상태로 보인다.

## 7. Manager Relationships

| Class | Role | Main dependencies | Main state |
| --- | --- | --- | --- |
| `CManager` | 전체 object graph 구성과 초기화 | 모든 file adapter, `CInterfaceManager`, `CMotionManager`, `CStationManager` | config root, startup steps/messages, loaded counts |
| `CInterfaceManager` | 장비 통신 device registry와 장비별 명령 facade | `ILogManager`, `IBETFile`, `IPowerMeterFile`, `IComm` 구현체 | `_devices`, simulation mode, laser/chiller/attenuator/BET/power meter status dictionaries |
| `CMotionManager` | 축/IO 상태, motion command, controller dispatch | `IInterfaceManager`, `ST_MOTOR_DATA`, `ST_IO_DATA`, `CMotionController` 파생 | `_motors`, `_axisData`, `_axes`, `_io`, `_controllers`, `_simulationMode` |
| `CStationManager` | station facade | `CStationProcess` | `_processStation` |
| `CStationProcess` | 공정 상태 machine, script 생성, 제품 상태 갱신 | interface/motion/interlock/script/product/log manager | `_processModel`, `_snapshot`, `_stationStatus`, `_processLogs`, `_lastScript`, `_statistics` |
| `CRecipeManager` | recipe use case facade | `IRecipeFile` | 별도 캐시 없음 |
| `CSettingManager` | setting/interface setting facade | `ISettingFile`, `IInterfaceFile`, `IInterfaceManager` | 별도 캐시 없음 |
| `CProductManager` | active product lifecycle | `IProductFile`, `ILogManager` | `_current` |
| `CAlarmManager` | alarm 후보 생성과 active 발생 시각 유지 | `ST_DEVICE_STATUS`, `ST_INTERLOCK_SUMMARY` | `_activeSince` |
| `CInterLockManager` | interlock rule 평가 | `ST_DEVICE_STATUS` | static `Rules` |
| `CLogManager` | trace log read/write | config root | log root paths |

## 8. Interface/Communication Structure

```text
IPS_INTERFACE.csv
└─ CInterfaceFile.LoadAll()
   └─ ST_INTERFACE_DATA
      ├─ InterfaceType: EN_INTERFACE_TYPE
      ├─ Device: EN_EQP_MODULE
      ├─ Number
      ├─ NickName
      ├─ SystemSection
      ├─ AutoConnection
      ├─ IsSimulation
      ├─ Arguments: ARG1..ARG5
      └─ Extra

CInterfaceManager.Register()
└─ CInterfaceDevice
   ├─ Data: ST_INTERFACE_DATA
   ├─ ConnectOption: ST_INTERFACE_CONNECT_OPTION
   └─ _comm: IComm
      ├─ CSocketComm
      ├─ CSerialComm
      │  ├─ CTalonLaser
      │  ├─ COrionChiller
      │  ├─ CConex_AGP
      │  ├─ CBeamExpander
      │  └─ CPowerMeter
      ├─ CACSComm
      └─ CReadyOnlyComm
```

`CComm`는 reflection으로 `[CCommType]`가 붙은 통신 구현체를 찾고, interface type/device name에 맞는 `IComm`을 생성한다. 통신 상태는 `ST_INTERFACE_COMM_STATUS`와 `ST_DEVICE_COMM_STATUS`로 UI와 system status에 전달된다.

## 9. Motion Structure

```text
IPS_MOTOR.csv
└─ CMotorFile.LoadAll()
   └─ ST_MOTOR_DATA
      ├─ axis identity: Name, Axis, DevType, DevNo
      ├─ kinematics: Scale, MaxVel, MaxAcc, Min, Max
      ├─ station mapping: System, StationName, DisplayName
      └─ offsets/reverse/home/precheck metadata

IPS_IO.csv
└─ CIoFile.LoadAll()
   └─ ST_IO_DATA
      ├─ Id, Address, Name
      ├─ IsOutput
      ├─ DevType, DevNo
      └─ InitialState, DisplayOrder, Description

CMotionManager
├─ _axisData: axis id -> ST_MOTOR_DATA
├─ _axes: axis id -> ST_AXIS_STATE
├─ _io: id/address -> ST_IO_STATE
└─ _controllers: DevType/DevNo -> CMotionController
   ├─ CAutomation1Motion
   ├─ CACSMotion
   ├─ CA3200Motion
   ├─ CAjinMotion
   ├─ CPmacMotion
   └─ CUmacMotion
```

motion command는 `EN_MOTION_COMMAND`로 표준화되고 `ExecuteAxisCommand()`가 validation, 실제 controller dispatch, simulation state 반영을 담당한다.

## 10. UI Screen Model

```text
IMenu
└─ Build() -> CScreenViewModel
   ├─ Menu: EN_MENU
   ├─ Title / Subtitle
   ├─ Metrics: IReadOnlyList<ST_DISPLAY_ITEM>
   ├─ Sections: IReadOnlyList<ST_SCREEN_SECTION>
   ├─ layout-specific references
   │  ├─ MainOperating: CMenuMain?
   │  ├─ Manual: CMenuManual?
   │  ├─ Recipe: CMenuRecipe?
   │  ├─ Setting: CMenuSetting?
   │  ├─ Alarm: CMenuAlarm?
   │  ├─ Monitor: CMenuMonitor?
   │  └─ Pm: CMenuPm?
   └─ Is*Layout flags
```

화면별 DTO는 UI 표시 전용 record/class다. 예를 들어 `CMenuRecipe`는 `ST_RECIPE_MANAGED_ITEM`, `ST_RECIPE_FILE`, `ST_RECIPE_HISTORY_ROW`를 만들고, `CMenuMonitor`는 `ST_MONITOR_IO_ROW`, `ST_MONITOR_AXIS_ROW`, `ST_PWM_STEP_ROW` 등을 만든다. Common 도메인 record를 직접 화면에 바인딩하기보다 화면 전용 row model로 변환하는 패턴이다.

## 11. File Adapter Mapping

| File adapter | Implements | Reads/Writes | Domain data |
| --- | --- | --- | --- |
| `CIpsRecipeFile` | `IRecipeFile` | `IPS_RCP.csv`, `RECIPE/*.csv`, recipe log | `ST_RECIPE_DATA`, `ST_RECIPE_PARAM`, `ST_RECIPE_HISTORY` |
| `CSettingFile` | `ISettingFile` | `IPS_SETTING.csv`, `Setting/Setting.csv`, setting log | `ST_SYSTEM_PARAMETER`, `ST_SETTING_HISTORY` |
| `CInterfaceFile` | `IInterfaceFile` | `IPS_INTERFACE.csv` | `ST_INTERFACE_DATA` |
| `CMotorFile` | `IMotorFile` | `IPS_MOTOR.csv` | `ST_MOTOR_DATA` |
| `CIoFile` | `IIoFile` | `IPS_IO.csv` | `ST_IO_DATA` |
| `CBETFile` | `IBETFile` | `IPS_BET.csv`, `BET/BET.csv` | `ST_BET_TABLE_DATA` |
| `CPowerMeterFile` | `IPowerMeterFile` | `IPS_POWERMETER.csv`, `PowerMeter/*.pwm` | `ST_POWER_METER_TABLE_DATA`, `ST_POWER_METER_STEP_DATA` |
| `CManualScanFile` | `IManualScanFile` | `IPS_MANUAL_SCAN.csv`, `Manual/*.scan` | `ST_MANUAL_SCAN_PARAM` |
| `CProductFile` | `IProductFile` | `Data/Product/*.csv` | `ST_PRODUCT_DATA`, `ST_PRODUCT_HISTORY` |
| `CAutomation1ScriptFile` | `IAutomationScriptFile` | `Data/Script/PROCESS.ascript` | `ST_AUTOMATION1_SCRIPT` |
| `CConfigStructureFile` | `IConfigStructureFile` | Config folder validation | `ST_CONFIG_FILE_STATUS` |

## 12. Detailed Member Reference

`CLASS_INVENTORY.md` contains:

- `Drilling.Common`: 154 types
- `Drilling.File`: 17 types
- `Drilling.UI`: 78 types

For each type it lists:

- kind: class, static class, interface, enum, struct
- base type and implemented interfaces
- constructors
- explicit private/public fields
- properties
- events
- declared methods
- enum values

Use that file when tracing "which class owns which variable" or "which class exposes which method". This architecture document should be used first for direction and dependency flow, then `CLASS_INVENTORY.md` for exact member-level detail.

## 13. Shell UI and Review Offset Base Update

This local-only update intentionally hides the detailed operator screens from the first runtime view. `Drilling.UI/CRootView.xaml` now acts as a structure shell: it keeps the real ViewModels and commands alive, but it does not render the heavy Main/Manual/Recipe/Setting/Monitor views. The goal is to let a new reader understand the assembly path before reading detailed UI code.

Runtime assembly remains:

```text
CAppStartup
  -> CManager
      -> Drilling.File.IPS adapters
      -> CInterfaceManager / CMotionManager
      -> CStationManager
      -> CProductManager
  -> CRootView shell
```

Review measurement and review offset are now represented as base architecture in `Drilling.Common/Review/CReviewCorrectionManager.cs`.

Key records:

- `ST_REVIEW_MEASUREMENT_POINT`: one measured point, including design/process/review/measured coordinates and error.
- `ST_REVIEW_MEASUREMENT_BATCH`: one board/process review data set.
- `ST_REVIEW_OFFSET_POLICY`: gain, max offset, minimum sample count, accumulation, and next-plan apply option.
- `ST_REVIEW_HEAD_OFFSET`: calculated Head-level correction.
- `ST_REVIEW_CORRECTION_RESULT`: final correction result and applicability.

`CStationProcess` now has two additional conceptual steps:

```text
PLAN
  -> INTERLOCK
  -> PARAMETER
  -> DEVICE
  -> SCRIPT
  -> TASK
  -> WAIT_DONE
  -> REVIEW_MEASURE
  -> REVIEW_OFFSET
  -> COMPLETE
```

The current implementation uses simulation review data because the project still has no real Vision PC, Review Camera, or APC file I/F. The added base code shows where those interfaces should feed data and how that data should become `Hxx_OFFSET_X/Y` parameters for the next process model.

Requirement mapping:

- `Review 측정 결과 기반 Offset 보정 방식`: base data model and correction manager added.
- `자동 정밀도 보정 / APC 보정`: represented as `EN_REVIEW_MEASUREMENT_MODE.ApcCorrection`; file watcher/parser still needs implementation.
- `0선 방어`: represented as `EN_REVIEW_MEASUREMENT_MODE.ZeroLineDefense`.
- `Fine/Rough/Simple 보정`: represented as separate review modes; detailed UI and algorithm variants remain future implementation.

## Automation1 AeroScript Client/Server I/F (2026-07-20)

```text
Recipe / Review / Scanner parameters
  -> CoordinateTransformService (Client PC)
  -> AeroScriptGenerator (Client PC only)
  -> UploadScript + SHA-256
  -> Scanner Server spool
  -> RunScript command
  -> Controller.Files.WriteText
  -> Runtime.Tasks[n].Program.Run
  -> TaskState polling
  -> Completed / Failed response to Client UI
```

Server PC는 좌표와 AeroScript를 생성하거나 수정하지 않는다. Upload와 Run은 서로 다른 명령이며, Server는 동일 Automation1 Task에 대한 동시 실행을 직렬화한다. 자세한 설정과 protocol 계약은 `AUTOMATION1_AEROSCRIPT_CLIENT_SERVER_GUIDE.md` 및 `AUTOMATION1_AEROSCRIPT_CLIENT_SERVER_FLOW.svg`를 참조한다.

### Virtual Wait Simulation 경로

```text
GX + Stage Y MOF pair 전제
  -> MoveAbsolute(Y, StartY + Travel, Speed) 비동기 시작
  -> GX band별 GY drill point + MoveDelay 반복
  -> wait(StatusGetAxisItem(Y, PositionFeedback) threshold)
  -> 다음 GX band 진행
  -> 마지막에 WaitForMotionDone(Y) + WaitForInPosition(Y)
  -> ProgramComplete
```

이 경로에는 Laser, PSO, Hardware Aux, Galvo calibration 명령을 넣지 않는다. `SimulationAutomation1Runtime`은 protocol만 모사하므로 실제 Wait 검증은 Automation1 Virtual Controller에 `Automation1ReflectionRuntime`으로 접속해야 한다. Virtual axis feedback은 command와 동일하므로 결과는 논리 순서 검증이며 실제 Encoder 지연과 광학 동기 검증이 아니다. 상세 흐름은 `AUTOMATION1_VIRTUAL_WAIT_SIMULATION_FLOW.svg`를 참조한다.

Server 실행 정책은 Virtual Controller에서 `VirtualOnly`, 실제 장비에서 `HardwareOnly`로 고정한다. 반대 생성 모드의 Package는 Controller File System에 기록하기 전 `MODE_POLICY_REJECTED`로 거부한다.

### 2026-07-20 연결 및 좌표 I/F 보완

Client의 `Script 생성`은 `Local Script File`에 실제 `.ascript`를 저장한 뒤 별도의 `Controller File` 경로를 Package에 넣는다. `AeroScriptClient.HealthCheckAsync`와 Server의 `HealthCheck` dispatch가 Upload 전에 TCP/API Key/ModePolicy를 확인한다. Scanner UI 선택은 선택 Head별 `InField` 좌표 합집합을 `_selectedPointKeys`에 반영한다.

Script 이동값은 `CellCommand.Gx/Gy`이며 Review 표시값은 `ReviewCoordinateX/Y`이다. 생성 Script 주석과 Client 로그는 두 좌표를 함께 기록하지만 Scanner 명령에는 Process Gx/Gy만 사용한다.

### Script Gateway와 Automation1 Native Endpoint 분리

Client WPF는 `192.168.10.10:46100`의 `Automation1Server`와 protocol v3으로 통신한다. `Automation1Server` 내부의 `Automation1ReflectionRuntime`이 공식 .NET API `Controller.Connect()`를 호출해 Automation1 Controller native endpoint(통상 `12200`)에 연결한다. Client가 `12200`에 JSON frame을 직접 전송하면 native protocol과 불일치하여 원격 호스트가 연결을 종료한다.
