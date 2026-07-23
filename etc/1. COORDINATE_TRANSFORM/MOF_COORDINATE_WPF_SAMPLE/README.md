# MOF Coordinate WPF Sample

## 2026-07-23 Scanner Axis Context Menu Update

- Each scanner now displays current `GXn/GYn` `PositionFeedback` below the scanner icon after Automation1 connection.
- Axis positions refresh after direct connection, after manual axis commands, and during process monitoring.
- The top summary line also includes a compact current axis-position list such as `H1: GX1=0.000, GY1=0.000`.
- The scanner icon now shows live axis positions inside the button body without the `Now` prefix, using two compact lines such as `GX1:0.000` and `GY1:0.000`.
- A dedicated 500 ms axis-position polling timer keeps scanner positions updating even when no AeroScript process task is running.
- Scanner icon right-click opens an axis control context menu for that head.
- The head menu exposes `GXn` and `GYn` submenus with `Enable`, `Disable`, `Home`, `Jog Move`, and `Motion Parameter Setting`.
- Right-clicking an empty area of the board layout opens an all-scanner menu for `All Enable`, `All Disable`, `All Home`, and `All Motion Parameter Setting`.
- Manual axis commands run on the same fixed task as the scanner number. H3 axis commands run on Automation1 Task 3.

## 2026-07-23 Scanner Axis Naming And Live Process Marking Update

이번 버전은 A1 Studio 축 네이밍을 현장 설정과 맞췄다.

- 기본 `GX Axis Template`은 `GX{0}`이다.
- 기본 `GY Axis Template`은 `GY{0}`이다.
- Scanner 1번은 `GX1`, `GY1`로 생성된다.
- Scanner 2번은 `GX2`, `GY2`로 생성된다.
- 여러 scanner를 선택하면 기존과 동일하게 scanner별 독립 파일을 만든다. 예: `mof_generated_H1.ascript`, `mof_generated_H2.ascript`.
- 같은 파일명으로 반복 생성할 때 `_H1_H1.ascript`처럼 suffix가 중복되지 않도록 기존 `_H숫자` suffix를 제거하고 새 head suffix를 붙인다.
- Controller compile 전 필수 축 검증도 생성된 축명 기준으로 수행된다. 따라서 A1 Studio MCD에 `GX1/GY1`, `GX2/GY2`가 존재해야 H1/H2 script가 정상 compile 대상이 된다.
- 가공 모니터링 UI는 현재 가공 중인 위치를 cyan 외곽선으로 표시하고, 이미 지나간 가공 완료 위치는 emerald 색상으로 누적 표시한다.
- 복수 Task 실행 중에는 각 Task의 진행 상태를 순회 조회하여 scanner별 완료 위치가 UI에 계속 누적된다.

## 2026-07-23 Simulation-Safe Galvo Substitute Update

Automation1 공식 문서 기준으로 `GalvoLaserOutput`, `GalvoConfigureLaserMode`, `PsoReset` 같은 Galvo/PSO 함수는 GI4/GL4 galvo hardware 및 laser output 제어 영역이다. 따라서 Virtual Controller 시뮬레이션에서는 실제 하드웨어 함수를 그대로 호출하지 않는다.

이번 버전의 분리 기준:

- `Simulation - Virtual Wait` 모드에서는 `GalvoLaserOutput()`을 생성하지 않는다.
- 대신 `SimulatedGalvoLaserOutput(state)` 함수를 생성한다.
- 이 대체 함수는 실제 laser output을 내보내지 않고 `$iglobal[2]`에 laser state, `$iglobal[3]`에 simulated pulse count만 기록한다.
- WPF 모니터는 `$iglobal[2]`, `$iglobal[3]`을 읽어 `SIM Laser=ON/OFF Pulse=N`으로 표시한다.
- `External Stage AUX MOF`와 실제 `Hardware Coordinate` 계열은 장비 모드이므로 hardware readiness/interlock 확인 후 실제 Galvo/PSO 함수를 유지한다.
- 이렇게 하면 동일한 좌표/순서/shot timing을 Virtual 환경에서 검증하면서도, 하드웨어 전용 함수로 인한 compile/run 실패를 피할 수 있다.

## 2026-07-23 Field AUX MOF Multi-Task Update

이번 버전은 현장 설비에서 사용하는 Automation1 AeroScript 구조를 기준으로 External Stage AUX MOF 생성을 다시 맞췄다.

- 외부 Stage Y축은 Automation1 축으로 만들지 않는다. Stage encoder는 scanner `GY`의 `AuxiliaryFeedback`으로 들어오는 것으로 본다.
- 기본 encoder 분해능은 현장값인 `16000 cts/mm`로 변경했다.
- `Follow Before Process mm` 입력을 추가했다. 기본값은 `200 mm`이며, 가공 시작 전까지 scanner가 Stage를 따라가는 구간이다.
- External Stage AUX MOF script는 현장 코드처럼 `MoveRapid([GY,GX], [gy,gx])`, `wait(StatusGetAxisItem(GY, AxisStatusItem.AuxiliaryFeedback) > count)`, `GalvoLaserOutput(GY, GalvoLaser.On/Off)` 순서로 생성한다.
- wait threshold는 `200 mm 추종거리 + AUX Initial Wait + AK1->AK2 방향 LocalY 진행량`에 `External Encoder cnt/mm`를 곱해 count로 만든다.
- scanner `GY/GX` 좌표는 가공 위치 명령이고, AUX wait count는 외부 Stage 이동량이다. 두 값은 같은 좌표계가 아니므로 숫자가 다를 수 있다.
- 여러 scanner를 선택하면 scanner head별로 독립 `.ascript`를 생성한다. Task는 선택 순서가 아니라 scanner 번호와 동일하게 고정 배정된다. 예를 들어 H1/H3/H5를 선택하면 H1은 Task 1, H3은 Task 3, H5는 Task 5에서 실행된다.
- 따라서 H8까지 사용할 경우 Automation1 Controller에는 최소 Task 1~8이 준비되어 있어야 하며, 각 Task는 해당 head script `mof_generated_H{head}.ascript`를 실행한다.
- Controller file과 local file은 `_H{head}` suffix가 붙는다. 예: `programs/mof_generated_H3.ascript`.
- Controller upload, compile, run, status, stop, 전체 실행은 생성된 모든 scanner Task를 대상으로 처리한다.
- 상태 모니터는 실행 중인 Task들의 진행 상태를 조회하고 가장 진행된 좌표를 기준으로 UI를 갱신한다.

## 2026-07-23 PPID Process Program Update

이번 버전은 기존 `CellBlockColumns / CellBlockRows / CellBlockPitch` 중심의 배치 개념을 PPID 기반 PP 정보로 대체한다.

- PPID 입력 영역에 `Cell PP Table`을 추가했다. `MAX_CELL_NUMBER`를 입력하고 `Cell PP Table 생성`을 누르면 Cell# 1부터 입력한 개수까지 Excel 표처럼 행이 생성된다.
- 각 행에서 `1st Pixel X`, `1st Pixel Y`, `Rotation`을 직접 수정하면 `CELLn_ALIGN_TO_1ST_PIXEL_X/Y`, `CELLn_ROTATION` 값으로 반영된다.
- `Cell 좌표 자동배치`는 Cell1 기준 위치와 현재 pitch를 사용해 초안 좌표를 자동으로 채운다. 이후 필요한 Cell 좌표만 사용자가 직접 수정하면 된다.
- 표에서 수정한 값은 `Generate / Refresh`, `Save Current CSV`, `Script 생성` 시 자동으로 PP정보에 반영된다.
- `PPID_PROCESS_PROGRAM_TEMPLATE.csv`를 추가했다. 사용자는 이 CSV에서 `ONLINE_PPID_NAME`, `STAGE_SPEED`, `LASER_POWER`, `NUM_OF_PIXEL_X/Y`, `PITCH`, `CELL1~CELL50_ALIGN_TO_1ST_PIXEL_X/Y`, `HEAD1~HEAD8_*` 항목을 작성할 수 있다.
- `MAX_CELL_NUMBER` 이하의 `CELLn_ALIGN_TO_1ST_PIXEL_X/Y`만 실제 Cell 좌표 생성에 사용한다.
- `NUM_OF_PIXEL_X`, `NUM_OF_PIXEL_Y`, `PITCH`는 Cell 내부 가공 pixel matrix의 열/행/pitch로 연결된다.
- `CELLn_ROTATION`은 각 Cell 내부 pixel matrix에 먼저 적용되고, 이후 Review AK1 기준 기판 전체 `ThetaAlignDeg`가 적용된다.
- 생성되는 AeroScript header에 `ONLINE_PPID_NAME`과 주요 PP 조건이 기록되어 장비 로그에서 어떤 PPID로 만든 script인지 추적할 수 있다.
- 기존 `CellBlock*` 값은 legacy CSV 호환용 fallback으로 남겨 두었다. 새 PPID CSV가 Cell 좌표를 제공하면 block grid는 좌표 생성 원본으로 쓰지 않는다.
- 설명 자료는 `PPID_PROCESS_PROGRAM_GUIDE.md`와 `PPID_PROCESS_PROGRAM_FLOW.svg`를 참고한다.

## 2026-07-22 Virtual Counter Monitor V6

- Simulation은 Automation1 Stage Y 축을 생성하거나 요구하지 않습니다.
- Script 내부의 `$rglobal[0]`을 `Stage Speed × Virtual Counter Tick`만큼 갱신하여 Stage 이동을 모사합니다.
- `$rglobal[0..2]`, `$iglobal[0..1]`을 WPF가 읽어 Stage 위치, 현재 Cell, 진행률을 표시합니다.
- 역물류 가공 순서는 기판 로컬 좌표 기준 AK1 측에서 AK2 측으로 고정됩니다.
- Matrix는 열린 탭만 생성하고 창 크기 변경은 지연 처리하여 UI 부하를 줄였습니다.
- 상세 내용은 `VIRTUAL_STAGE_COUNTER_MONITOR_UPDATE_20260722.md`를 참고합니다.

## 2026-07-22 External Stage AUX MOF V5

- 타사 Stage Y는 Automation1 축 목록과 Script의 이동 명령에서 제외합니다.
- Stage encoder는 Scanner GY AUX 입력으로 수신하며 `AuxiliaryFeedback` 누적값으로 가공 구간을 해제합니다.
- `External Encoder cnt/mm`, `Encoder Direction`, `AUX Initial Wait mm`을 UI에서 설정합니다.
- Compile 성공 시 source와 같은 이름의 `.a1exe`를 Controller File System에 기록합니다.
- Controller 축 목록에 `z`/`Z`처럼 대소문자만 다른 항목이 있어도 중복 Key 예외가 발생하지 않습니다.
- 상세 내용은 `EXTERNAL_STAGE_AUX_MOF_UPDATE_20260722.md`를 참고합니다.

## 2026-07-22 AeroScript Task 실행 문법 수정

- `$StartYPos`를 `program` 블록 안에서 선언합니다.
- 축 이름을 문자열에서 `axis` 값으로 변환하고 `MoveAbsolute($StageAxis, $StartYPos, StageSpeed)`를 생성합니다.
- Equipment 모드는 `MoveLinear([GX, GY], [Gx, Gy], speed)` 형태의 축 배열 리터럴을 사용합니다.
- 배포 로그 화면을 더블클릭하면 표시 로그가 초기화됩니다.
- 상세 내용은 `AEROSCRIPT_TASK_RUN_SYNTAX_FIX_20260722.md`를 참고합니다.

이 샘플은 Review Camera에서 측정한 AK1 Pixel 좌표를 기반으로 기판 내부 Cell 좌표를 Stage Global 좌표로 변환하고, Multi Scanner의 Zigzag Odd/Even 배치에 따라 MOF 가공 명령 `Gx`, `Gy`를 생성하는 예제이다.

2026-07-22 업데이트에서는 Automation1 실행을 `Simulation - Virtual Wait`, `Equipment - External Stage AUX MOF`, `Equipment - Hardware Coordinate`로 분리했다. Controller 기록 후 `Controller.Compiler.CompileControllerFile`로 사전 컴파일하고, 파일/행/열별 오류가 없을 때만 `CompiledAeroScript`를 Task에 실행한다. Simulation은 MCD의 Virtual 축만 허용하며, Equipment 실행은 축 상태·Safety Interlock·Laser/Beam Path·작업자 최종 승인 확인을 모두 요구한다.

2026-07-17 업데이트에서는 Review Camera 광학 중심과 H1 Scanner 가공 중심 사이의 고정 물리 오프셋을 좌표 변환의 필수 입력으로 분리했다. Scanner 배치는 명시적인 `FirstScannerInitialStageX/Y`에서 시작하고, 이 값이 `ReviewCenter + CameraToScannerPhysicalOffset`과 일치하는지 검증한다. 가공 후 측정오차를 보정하는 `ProcessOffsetGlobal`은 별도 데이터로 관리한다.

장비 물류 순서는 `Home → Review Camera 통과 → Scanner 뒤쪽까지 정방향 이동 → 방향 반전 → 역방향 이동 중 MOF → Review Camera 후측정 → Home 복귀`로 반영했다. 역물류 중 가공점은 Stage 축 부호와 무관한 기판 Local 좌표를 기준으로 AK1 측에서 AK2 측 순서로 실행된다.

2026-07-16 업데이트에서는 첨부 예시 이미지처럼 기판 Cell과 Scanner Head 선택 상태를 색상으로 쉽게 파악할 수 있도록 UI를 개선했다. 또한 설계좌표, 가공좌표, 리뷰좌표계를 별도 탭으로 분리하고 모든 좌표 결과를 `(x, y)` 2D Matrix 형태로 표시한다.

추가 업데이트에서는 AK1 기준 첫 번째 가공 위치, X/Y pitch, 내부 가공점 행/열, 기판 내 Cell# 블록 행/열과 pitch를 CSV 설정으로 읽을 수 있게 했다. CSV는 Excel에서 열고 저장할 수 있으므로 Recipe 설정표처럼 사용할 수 있다.

최근 업데이트에서는 전체 화면을 Dark Theme로 조정하고, Clear Scanner 실행 시 선택된 Scanner Head 표시가 모두 해제되도록 정리했다. Scanner 선택 상태와 Review 기준 Head는 분리해서 표시하므로 `Highlight Heads` 값이 비어 있으면 Scanner 버튼은 활성 색상으로 남지 않는다.

색상 체계는 Dark Theme 기준으로 재정리했다. 선택 좌표는 Amber, 선택 Scanner가 처리 가능한 좌표와 X 가공 가능 Band는 Cyan/Teal, Scanner Head 선택 상태는 Blue로 구분해 Board와 Matrix에서 동일한 의미가 같은 색으로 보이도록 했다.

가공 가능 Band 설명 라벨은 Board 내부가 아니라 Board 아래 전용 라벨 영역에 표시된다. 여러 Scanner를 선택해도 라벨이 2줄로 분산 배치되며, Scanner 박스는 최소 Y 간격을 확보해 좌표 텍스트와 겹치지 않도록 했다.

Board 확대 비율과 Matrix 셀 크기는 마지막 사용값을 로컬 사용자 설정으로 저장한다. 앱을 다시 실행해도 마지막으로 조정한 스케일이 유지된다.

Matrix 선택 방식은 일반 데스크톱 UI에 가깝게 확장했다. 셀을 누른 채 드래그하면 지나간 셀이 연속 선택되고, `Shift + Click`은 마지막 기준 셀부터 현재 셀까지 범위 선택하며, `Ctrl + Click/Drag`는 기존 선택을 유지한 채 추가 또는 해제한다.

## 실행 방법

```powershell
dotnet build
dotnet run
```

## 화면 구성

- 좌측 입력 패널: Board, Review Camera, Cell, Scanner, DOE16 Beam, Camera→Scanner Physical Offset, Dynamic Review Correction 변수 입력.
- Parameter Tooltip: 각 입력칸 또는 버튼 위에 마우스를 올리면 해당 파라미터가 어느 좌표계/계산식에 쓰이는지 말풍선으로 설명한다. Board, Review, Cell, Scanner, DOE 항목은 작은 도식도 함께 표시한다.
- 상단 Canvas: Board Cell 블록, 선택 Cell, Highlight Scanner가 담당하는 Cell, Zigzag Scanner Head 시각화.
- 마우스 클릭: 화면의 Cell 또는 Scanner Head를 클릭하면 선택 상태와 결과 Matrix가 즉시 갱신된다.
- 복수 선택: Matrix View에서 여러 칸을 선택하면 해당 가공점들이 상단 Board UI에 함께 표시된다. 드래그 선택, `Shift + Click` 범위 선택, `Ctrl + Click/Drag` 추가/해제를 지원한다.
- 확대/축소: `Ctrl + Mouse Wheel`일 때만 Board 또는 Matrix가 확대/축소된다. 일반 Mouse Wheel은 스크롤로 동작한다. 마지막 Board/Matrix 스케일은 다음 실행에도 유지된다.
- Board Scroll: Board를 확대하면 상하좌우 ScrollBar로 원하는 위치를 이동해서 볼 수 있다.
- Design 2D Matrix 탭: Cell#별 A/B/C 열, 1/2/3 행 형태로 Recipe Local 좌표를 `(x, y)`로 표시.
- Process 2D Matrix 탭: 동일 Matrix 구조로 최종 MOF `Gx/Gy`를 `(x, y)`로 표시.
- Review Camera 2D Matrix 탭: Scanner Stage 상대좌표에 Camera→Scanner 물리 Offset과 선택 DOE Stage Offset을 적용한 실제 Review Camera 좌표를 `(x, y)`로 표시.
- DOE 16 Matrix 탭: 4 x 4 DOE Beam의 Scanner 내부 Offset Matrix 표시.

## Excel CSV 설정

`CELL_LAYOUT_CONFIG_TEMPLATE.csv`를 Excel에서 열어 값을 바꾼 뒤 저장하고, 화면의 `Load Saved CSV Config` 또는 `Reload Current CSV` 버튼으로 불러온다.

- `Open CSV Template in Excel`: 기본 CSV 템플릿을 Excel 또는 Windows 기본 CSV 앱으로 연다.
- `Load Saved CSV Config`: 사용자가 저장한 CSV를 선택해서 화면에 반영한다.
- `Reload Current CSV`: 마지막으로 열거나 로드한 CSV를 다시 읽어 화면을 갱신한다.
- `Save Current CSV`: 현재 화면 입력값을 마지막 CSV 파일에 저장한다. 마지막 CSV가 없으면 저장 위치를 선택한다.

주요 항목:

- `CellFirstX`, `CellFirstY`: AK1 기준 첫 번째 Cell# 블록의 첫 번째 가공점 A1 거리.
- `CellPitchX`, `CellPitchY`: Cell# 내부 가공점 Matrix의 X/Y pitch.
- `CellColumns`, `CellRows`: Cell# 내부 가공점 Matrix 열/행 수. 화면에는 A/B/C 열과 1/2/3 행으로 표시된다.
- `CellBlockColumns`, `CellBlockRows`: 기판 안에 배치되는 Cell# 블록의 열/행 수.
- `CellBlockPitchX`, `CellBlockPitchY`: Cell# 블록 사이의 X/Y pitch. 0이면 내부 Matrix 크기를 기준으로 자동 배치 pitch를 계산해 겹침을 방지한다.
- `ScannerFieldHalfX`: Scanner가 가공 가능한 X 방향 반폭. Scanner를 클릭하면 `CenterX ± HalfX` 범위에 포함되는 가공점만 강조 표시된다.
- `ScannerFieldHalfY`: Scanner 설계상의 Y 방향 field 반폭 참고값. MOF 컨셉에서는 기판이 Y 방향으로 이동하므로 가공 가능 여부는 X 커버리지로 판단하고, UI의 process band는 Board Y 전체로 표시한다.
- `ReviewToFirstScannerOffsetX/Y`: Review Camera 광학 중심에서 H1 Scanner 가공 중심까지의 고정 물리 거리. 장비 조립치수 또는 캘리브레이션 결과로 관리하며 Recipe나 Review 측정오차에 따라 바뀌면 안 된다.
- `FirstScannerInitialStageX/Y`: 장비 설계 또는 Teaching으로 결정한 H1 Scanner 중심의 실제 Stage 초기좌표. Scanner Pitch와 Even Y Offset은 이 위치에서 파생된다.
- `ScannerOriginTolerance`: H1 실제 초기좌표와 `ReviewCenter + PhysicalOffset` 기대좌표 사이의 허용오차.
- `ProcessOffsetGlobalX/Y`: 가공 후 Review 측정오차를 다음 가공에 반영하는 동적 보정값. 고정 물리 오프셋과 별도 이력 및 버전으로 관리한다.
- `HomeStageY`: 기판 Stage가 공정 전후에 대기하는 원점 Y 좌표.
- `ForwardTransportSignY`: Home에서 Review Camera를 먼저 지나 Scanner 쪽으로 이동하는 정물류 방향. `+1`이면 +Y 전진/-Y 역방향 MOF이고, `-1`이면 반대이다.

## 실제 장비 이송 및 가공 순서

```text
1. Home 대기
2. 정물류 방향으로 이동하면서 Review Camera 위치를 먼저 통과
3. Review Camera 뒤쪽에 배치된 Scanner 가공 시작측까지 이동
4. Turnaround 위치에서 Stage 이동 방향 반전
5. 역물류 방향으로 돌아오면서 MOF 가공
6. MOF 종료 후 Review Camera 위치에서 가공 결과 측정
7. 측정 완료 후 Home 복귀
```

기본 설정에서 Review Camera Y는 `1200 mm`, H1 Scanner Y는 `1640.1 mm`이므로 +Y 전진 기준으로 Camera가 앞쪽, Scanner가 뒤쪽에 있다. 프로그램은 모든 Scanner 중심이 Review Camera보다 정물류 방향 뒤쪽에 있는지 검사하고 `배치검증=정상/확인필요`로 표시한다.

가공점 좌표는 Recipe 행 순서가 아니라 실제 역방향 이동 순서로 재정렬한다.

```text
ForwardTransportSignY = +1:
  MofExecutionCommands = ProcessStageY 내림차순

ForwardTransportSignY = -1:
  MofExecutionCommands = ProcessStageY 오름차순
```

각 `CellCommand.MofSequence`는 역방향 MOF 실행 순번을 보관한다. Process Matrix 좌표에 마우스를 올리면 실행 순번을 확인할 수 있다.

## Review Camera와 Scanner 사이의 물리 Offset

좌표 부호는 다음과 같이 정의한다.

```text
CameraToScannerOffset_i = ScannerCenter_i - ReviewCameraCenter
ScannerCenter_i = ReviewCameraCenter + CameraToScannerOffset_i
```

H1의 물리 오프셋을 기준으로 각 헤드의 오프셋을 파생한다.

```text
OffsetX_i = ReviewToFirstScannerOffsetX + (i - 1) * ScannerPitchX
OffsetY_i = ReviewToFirstScannerOffsetY + EvenYOffset(i)

EvenYOffset(i) = 0                  (홀수 Head)
               = EvenScannerYOffset (짝수 Head)
```

Review Camera에서 계산된 가공점을 Scanner 좌표계로 옮기는 순서는 다음과 같다.

```text
CameraRelative = ProcessStageTarget - ReviewCameraCenter
ScannerRelative = CameraRelative - CameraToScannerOffset_i
                = ProcessStageTarget - ScannerCenter_i

Odd Head : Gx = -ScannerRelativeX, Gy = +ScannerRelativeY
Even Head: Gx = +ScannerRelativeX, Gy = -ScannerRelativeY
```

기본 예제에서 Review Camera Center는 `(105, 1200) mm`, Camera→H1 Offset은 `(374.7, 440.1) mm`이므로 H1 기대 중심은 `(479.7, 1640.1) mm`가 된다. 입력한 H1 초기 Stage 위치와 이 기대값이 허용오차 안에서 같아야 한다. Process Matrix의 좌표 칸에 마우스를 올리면 Camera 기준 상대좌표, 선택 Head의 물리 Offset, Scanner 상대좌표, 변환 일치 오차와 최종 Gx/Gy를 함께 확인할 수 있다.

## Performance

좌표 Matrix는 대량 좌표에서 느려지는 `DataGrid` 대신 Canvas 기반 Matrix 뷰로 렌더링한다. 각 좌표 칸은 직접 그려지고 클릭 이벤트만 붙기 때문에 행/열 수가 늘어도 훨씬 가볍게 동작한다.

## 주요 코드

- `Models/CoordinateModels.cs`: 입력 데이터, Scanner Model, Cell Command 구조.
- `Services/CoordinateTransformService.cs`: AK1 Anchor 생성, Cell Stage 좌표 생성, Scanner Gx/Gy 변환, DOE Beam 기준 Review 좌표 생성.
- `MainWindow.xaml.cs`: 화면 입력값 읽기, 결과 탭 표시, 첨부 이미지 형태의 Cell/Scanner 도식화.

## DOE16 Review Coordinate

1 Beam 가공이 DOE를 통과하면서 16 Beam으로 분기된다는 전제를 반영했다.

- `Review Head`: 리뷰 좌표계 기준으로 사용할 Scanner Head 번호.
- `DOE Beam 1-16`: 4 x 4 DOE Matrix 중 기준으로 사용할 Beam 번호.
- `DOE Pitch X/Y`: DOE Beam 사이 간격.
- Review Camera 좌표는 `ScannerRelativeStage + CameraToSelectedScannerPhysicalOffset + SelectedDoeStageOffset`으로 계산된다.
- 위 식은 Scanner 초기위치와 물리 Offset이 일치할 때 `ProcessStage - ReviewCenter + SelectedDoeStageOffset`과 결과가 같아야 한다.
- Scanner Head별로 처리된 Cell을 구분하기 위해 각 Row에 `Process Scanner`가 함께 표시된다.

제공 설정 검증 예제:

```text
Review Center = (105, 1200)
Camera→H1 Offset = (10, 1000)
H1 Initial = (115, 2200)
H2 Center = (315, 2260)
Camera→H2 Offset = (210, 1060)

B1 Process G = (-99.830, 1024.804)
B1 Review Camera / DOE05 = (109.900, 35.286)
Transform Consistency Error = (0, 0)
```

## Excel 예제 반영값

- Board: 1500 x 925 mm.
- Review Camera Center: (105, 1200) mm.
- Review Pixel Center: (1224, 1024) px.
- Pixel Scale: 0.00345 mm/px.
- Measured AK1: (1282, 1053) px.
- Theta Align: 0.05 deg.
- Cell First: (50, 35) mm.
- Cell Pitch: (50, 45) mm.
- Pattern Offset: (10, 0) mm.
- Scanner H5 예제 중심: H1=(479.7,1640.1), PitchX=100 이므로 H5=(879.7,1640.1).

## Scanner 전체 좌표 선택 및 Script 파일

- Scanner 아이콘을 선택하면 선택된 모든 Head에 대해 `InField=true`인 가공 가능 좌표 전체가 Matrix 선택 집합에 들어간다.
- 여러 Head 선택은 각 Head의 가공 가능 좌표 합집합이며, Scanner 선택 해제 시 해당 합집합을 다시 계산한다.
- `Local Script File`은 Client PC 실제 파일 경로이고 Script 생성 즉시 저장된다.
- `Controller File`은 `Controller.Files.WriteText`로 기록할 Automation1 Controller File System 경로이다.
- WPF는 제공된 Aerotech Automation1 .NET API 2.13.1을 직접 참조하며 `Controller.Connect(host, 12200)`로 접속한다.
- 별도 `Automation1Server`, JSON Gateway, API Key, TCP 46100은 사용하지 않는다.
- `Controller.Compiler.CompileControllerFile(controllerFile, true)`로 먼저 컴파일하고 상세 진단을 표시한다.
- 컴파일된 `CompiledAeroScript`만 `Task.Program.Run(compiledProgram)`으로 실행한 뒤 250 ms 주기로 `Task.Status.TaskState`를 확인한다.
- `ProgramComplete`만 정상 완료이며 `Error` 또는 완료 전 비정상 상태 전환은 실패로 처리한다.
- 실행 Script와 `mof_job_*.json` 감사 이력은 Controller File System에 남는다.
- 설치·운영·장애 점검은 `AUTOMATION1_DIRECT_OPERATION_PROCEDURE.md`와 `AUTOMATION1_DIRECT_CLIENT_FLOW.svg`를 참조한다.

## 저배율 레이아웃 안전 처리

- 저장된 Board Zoom이 `0.35`까지 내려가도 Layout Canvas는 최소 `640 x 560`을 유지한다.
- 좁은 Canvas에서는 동작 순서 Badge와 Legend를 두 줄 영역으로 재배치한다.
- `DrawBadge`는 `NaN`, 무한대, 0 이하 Width/Height/FontSize를 안전한 값으로 정규화한다.
- 프로젝트는 기존 Assembly 속성 중복을 피하기 위해 `GenerateAssemblyInfo=false`, `GenerateTargetFrameworkAttribute=false`를 사용한다.
