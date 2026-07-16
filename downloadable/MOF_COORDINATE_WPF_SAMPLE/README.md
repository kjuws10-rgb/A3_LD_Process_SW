# MOF Coordinate WPF Sample

이 샘플은 Review Camera에서 측정한 AK1 Pixel 좌표를 기반으로 기판 내부 Cell 좌표를 Stage Global 좌표로 변환하고, Multi Scanner의 Zigzag Odd/Even 배치에 따라 MOF 가공 명령 `Gx`, `Gy`를 생성하는 예제이다.

2026-07-16 업데이트에서는 첨부 예시 이미지처럼 기판 Cell과 Scanner Head 선택 상태를 색상으로 쉽게 파악할 수 있도록 UI를 개선했다. 또한 설계좌표, 가공좌표, 리뷰좌표계를 별도 탭으로 분리하고 모든 좌표 결과를 `(x, y)` 2D Matrix 형태로 표시한다.

추가 업데이트에서는 AK1 기준 첫 번째 가공 위치, X/Y pitch, 내부 가공점 행/열, 기판 내 Cell# 블록 행/열과 pitch를 CSV 설정으로 읽을 수 있게 했다. CSV는 Excel에서 열고 저장할 수 있으므로 Recipe 설정표처럼 사용할 수 있다.

최근 업데이트에서는 전체 화면을 Dark Theme로 조정하고, Clear Scanner 실행 시 선택된 Scanner Head 표시가 모두 해제되도록 정리했다. Scanner 선택 상태와 Review 기준 Head는 분리해서 표시하므로 `Highlight Heads` 값이 비어 있으면 Scanner 버튼은 활성 색상으로 남지 않는다.

색상 체계는 Dark Theme 기준으로 재정리했다. 선택 좌표는 Amber, 선택 Scanner가 처리 가능한 좌표와 X 가공 가능 Band는 Cyan/Teal, Scanner Head 선택 상태는 Blue로 구분해 Board와 Matrix에서 동일한 의미가 같은 색으로 보이도록 했다.

가공 가능 Band 설명 라벨은 Board 내부가 아니라 Board 아래 전용 라벨 영역에 표시된다. 여러 Scanner를 선택해도 라벨이 2줄로 분산 배치되며, Scanner 박스는 최소 Y 간격을 확보해 좌표 텍스트와 겹치지 않도록 했다.

## 실행 방법

```powershell
dotnet build
dotnet run
```

## 화면 구성

- 좌측 입력 패널: Board, Review Camera, Cell, Scanner, DOE16 Beam, Review Offset 변수 입력.
- Parameter Tooltip: 각 입력칸 또는 버튼 위에 마우스를 올리면 해당 파라미터가 어느 좌표계/계산식에 쓰이는지 말풍선으로 설명한다. Board, Review, Cell, Scanner, DOE 항목은 작은 도식도 함께 표시한다.
- 상단 Canvas: Board Cell 블록, 선택 Cell, Highlight Scanner가 담당하는 Cell, Zigzag Scanner Head 시각화.
- 마우스 클릭: 화면의 Cell 또는 Scanner Head를 클릭하면 선택 상태와 결과 Matrix가 즉시 갱신된다.
- 복수 선택: Matrix View에서 여러 칸을 선택하면 해당 가공점들이 상단 Board UI에 함께 표시된다.
- 확대/축소: `Ctrl + Mouse Wheel`일 때만 Board 또는 Matrix가 확대/축소된다. 일반 Mouse Wheel은 스크롤로 동작한다.
- Board Scroll: Board를 확대하면 상하좌우 ScrollBar로 원하는 위치를 이동해서 볼 수 있다.
- Design 2D Matrix 탭: Cell#별 A/B/C 열, 1/2/3 행 형태로 Recipe Local 좌표를 `(x, y)`로 표시.
- Process 2D Matrix 탭: 동일 Matrix 구조로 최종 MOF `Gx/Gy`를 `(x, y)`로 표시.
- Review 2D Matrix 탭: 사용자가 선택한 Scanner Head와 DOE16 Beam 기준으로 모든 Cell의 리뷰 좌표계를 `(x, y)`로 표시.
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
- Review Coordinate 탭의 `Review Coordinate mm (x, y)`는 `ProcessStage - SelectedHeadSelectedBeamStage`로 계산된다.
- Scanner Head별로 처리된 Cell을 구분하기 위해 각 Row에 `Process Scanner`가 함께 표시된다.

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
