# MOF Coordinate WPF Sample

이 샘플은 Review Camera에서 측정한 AK1 Pixel 좌표를 기반으로 기판 내부 Cell 좌표를 Stage Global 좌표로 변환하고, Multi Scanner의 Zigzag Odd/Even 배치에 따라 MOF 가공 명령 `Gx`, `Gy`를 생성하는 예제이다.

## 실행 방법

```powershell
dotnet build
dotnet run
```

## 화면 구성

- 좌측 입력 패널: Board, Review Camera, Cell, Scanner, Review Offset 변수 입력.
- 상단 Canvas: Board, Align Key, Cell Target, Scanner Field 시각화.
- 중앙 Grid: 모든 Cell의 Local 좌표, Stage 좌표, Scanner 선택, Relative 좌표, Gx/Gy 표시.
- 하단 Formula: 좌표 변환 공식 요약.

## 주요 코드

- `Models/CoordinateModels.cs`: 입력 데이터, Scanner Model, Cell Command 구조.
- `Services/CoordinateTransformService.cs`: AK1 Anchor 생성, Cell Stage 좌표 생성, Scanner Gx/Gy 변환.
- `MainWindow.xaml.cs`: 화면 입력값 읽기, 결과 표시, Canvas 도식화.

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

