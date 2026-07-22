# PPID Process Program Recipe Guide

이 샘플은 기존 `CellBlockColumns / CellBlockRows / CellBlockPitch` 중심의 배치 개념을 PPID의 PP 정보로 대체한다.

핵심은 `ONLINE_PPID_NAME`이 선택되면 그 안의 PP 파라미터가 레시피 원본이 되고, 프로그램은 이 값으로 Cell 좌표와 가공 좌표를 만든다는 점이다.

## 1. PPID가 담당하는 값

- `ONLINE_PPID_NAME`: CIM 또는 상위 프로그램에서 선택하는 레시피 이름.
- `STAGE_SPEED`: MOF 중 기판 Y 이동 속도. Simulation에서는 software counter 속도, 장비 모드에서는 Stage/AUX 검증 기준으로 사용한다.
- `LASER_POWER`, `LASER_FREQUENCY`, `SHOT_COUNT`: 레이저 조건. 현재 샘플은 script header와 PP 구조에 반영하고, 실제 Laser/PSO 호출은 장비 interlock 확정 후 연결한다.
- `MAX_CELL_NUMBER`: 기판 안에서 사용할 Cell 개수. `CELL1`부터 이 번호까지의 좌표만 생성한다.
- `NUM_OF_PIXEL_X`, `NUM_OF_PIXEL_Y`, `PITCH`: Cell 하나 안의 가공 pixel matrix를 만든다.
- `CHESS`: 가공 pixel을 건너뛰며 처리하는 전략 값. 현재는 PP 정보로 보관하고, tact 최적화 단계에서 target filtering 조건으로 확장한다.
- `SPLITED_BEAM_COUNT`: DOE 분기 beam 수. 현재 UI는 4 x 4 = 16 matrix를 기준으로 표시한다.
- `MASKING_HOLE*`, `CELL_*_ROUND_*`: 보호/마스킹 조건. 좌표 생성 원본에 보관되며 실제 가공 exclusion은 후속 mask filter 단계에서 연결한다.
- `CELLn_ALIGN_TO_1ST_PIXEL_X/Y`, `CELLn_ROTATION`: AK1 기준 각 Cell의 첫 번째 가공 pixel 위치와 cell 자체 회전.
- `HEADn_*`: scanner head별 shot delay, ramp, jump speed, jump delay, DOE Z 조건.

## 2. 좌표 생성 방식

1. Review camera에서 AK1 pixel을 측정한다.
2. `ReviewCenter + PixelScale * (MeasuredAK1 - PixelCenter)`로 AK1 Stage anchor를 만든다.
3. `CELLn_ALIGN_TO_1ST_PIXEL_X/Y`를 AK1 기준 Cell# n의 첫 pixel 좌표로 사용한다.
4. Cell 내부 pixel은 `NUM_OF_PIXEL_X/Y`와 `PITCH`로 matrix 생성한다.
5. Cell 회전 `CELLn_ROTATION`을 먼저 적용한 뒤, 기판 전체 보정각 `ThetaAlignDeg`를 적용한다.
6. Review 보정 offset을 더해 `ProcessStageX/Y`를 만든다.
7. 고정 scanner 위치와 Review-to-scanner physical offset을 사용해 `Gx/Gy`를 만든다.
8. 선택된 scanner와 DOE beam을 기준으로 review 좌표계를 따로 표시한다.

## 3. CSV 작성 규칙

파일: `PPID_PROCESS_PROGRAM_TEMPLATE.csv`

CSV는 `Key,Value` 형식이다. Excel에서 열어 값만 바꾸고 저장한 뒤 WPF에서 `Load Saved CSV Config` 또는 `Reload Current CSV`로 반영한다.

`MAX_CELL_NUMBER`가 6이면 `CELL1`부터 `CELL6`까지만 사용한다. `CELL7` 이후 값이 0이어도 좌표 생성에는 들어가지 않는다.

기존 `CellBlock*` 항목은 legacy 호환용이다. 새 PPID CSV가 `CELLn_ALIGN_TO_1ST_PIXEL_*`를 제공하면 block grid는 좌표 생성에 사용하지 않는다.

## 4. 현재 코드 반영 위치

- `Models/CoordinateModels.cs`: `PpidProcessProgram`, `CellRecipeDefinition`, `MaskingHoleDefinition`, `HeadProcessDefinition` 추가.
- `Services/CoordinateTransformService.cs`: `ResolveCellDefinitions()`가 PPID Cell 목록을 우선 사용한다.
- `MainWindow.xaml`: PPID 핵심 항목 입력 영역 추가.
- `MainWindow.xaml.cs`: PPID CSV load/save, PPID tooltip, PPID와 matrix parameter 동기화.
- `Automation1/AeroScriptGenerator.cs`: 생성 script header에 PPID와 PP 주요 조건 기록.

