# MOF Multi Scanner Coordinate Transform Concept

본 문서는 리뷰 카메라가 측정한 기판 좌측상단 Align Mark(AK1)를 기준으로, Recipe Cell 좌표를 Stage 좌표계로 변환하고, 고정 배치된 Multi Scanner의 MOF 가공 좌표(Gx, Gy)를 생성하는 개념을 정리한다.

참고 엑셀: `Review_MasterAK_Global_MMULT_Simulation_6GH.xlsx`

## 1. 좌표계 정의

### 1.1 Recipe Local Coordinate

- 기준점: 기판 내부 좌측상단 Align Mark, 즉 Master AK1.
- 단위: mm.
- 방향: 기판 설계 좌표 기준 X/Y.
- 역할: Cell 첫 가공 위치, Cell Pitch, Pattern Offset, AK 간 거리 등 제품/Recipe 기준 데이터를 표현한다.

### 1.2 Review Camera Pixel Coordinate

- 기준점: Review Camera 영상 중심 `(U0, V0)`.
- 단위: pixel.
- 역할: 실제 AK1 위치를 영상에서 측정하고, Pixel Scale을 통해 mm 변위로 환산한다.

### 1.3 Stage Global Coordinate

- 기준점: 설비 Stage 기준 좌표.
- 단위: mm.
- 역할: Review Camera, Scanner Center, 기판 Cell Target 위치를 모두 같은 기준으로 정렬한다.

### 1.4 Scanner Field Coordinate

- 기준점: 각 Scanner의 물리 중심 또는 가상 Field Center `C_i`.
- 단위: mm 또는 Scanner Command unit.
- 역할: Stage Global Target을 각 Scanner의 상대 좌표로 변환한 뒤 MOF 가공 명령 `Gx`, `Gy`를 만든다.

## 2. 주요 입력 변수

| Group | Variable | Excel Example | Meaning |
|---|---:|---:|---|
| Board | BoardSizeX | 1500 | 기판 X 크기 mm |
| Board | BoardSizeY | 925 | 기판 Y 크기 mm |
| Align Key | AlignMarginX | 55 | 기판 좌우 AK Edge Margin mm |
| Align Key | AlignMarginY | 45 | 기판 상하 AK Edge Margin mm |
| Align Key | EffectiveAkDistanceX | 1390 | AK1-AK3 설계 거리 mm |
| Align Key | EffectiveAkDistanceY | 835 | AK1-AK2 설계 거리 mm |
| Review | ReviewCenterGlobalX | 105 | Review Camera 중심 Stage X mm |
| Review | ReviewCenterGlobalY | 1200 | Review Camera 중심 Stage Y mm |
| Review | ReviewPixelCenterU | 1224 | 영상 중심 U pixel |
| Review | ReviewPixelCenterV | 1024 | 영상 중심 V pixel |
| Review | PixelScaleU/V | 0.00345 | pixel to mm scale |
| Vision | ThetaAlignDeg | 0.05 | Review 측정 기반 기판 회전 보정각 |
| AK Measurement | MeasuredAk1U | 1282 | 측정된 AK1 U pixel |
| AK Measurement | MeasuredAk1V | 1053 | 측정된 AK1 V pixel |
| Cell | CellFirstX | 50 | AK1 기준 첫 Cell 가공 X mm |
| Cell | CellFirstY | 35 | AK1 기준 첫 Cell 가공 Y mm |
| Cell | CellPitchX | 50 | Cell X Pitch mm |
| Cell | CellPitchY | 45 | Cell Y Pitch mm |
| Cell | PatternOffsetX | 10 | Cell 내부 Pattern X Offset mm |
| Cell | PatternOffsetY | 0 | Cell 내부 Pattern Y Offset mm |
| Scanner | ScannerCount | 예: 8 | 배치된 Scanner 수 |
| Scanner | ScannerCenterX/Y | 예: H5 = 879.7, 1640.1 | 각 Scanner의 Stage 기준 중심 |
| Scanner | Odd/Even Type | H1/H3/H5... Odd, H2/H4/H6... Even | Zigzag 배치 및 Gx/Gy 부호 규칙 |

## 3. 변환 흐름

### Step 1. Review Pixel에서 AK1 Stage Anchor 생성

Review Camera 중심 Stage 좌표를 알고 있고, 영상 중심에서 측정 AK1까지의 Pixel Offset을 알고 있으므로 AK1의 Stage 좌표를 생성한다.

```text
dU = U_AK1 - U0
dV = V_AK1 - V0

P_AK1_global.X = ReviewCenterGlobalX + dU * PixelScaleX
P_AK1_global.Y = ReviewCenterGlobalY + dV * PixelScaleY
```

Excel 예제:

```text
dU = 1282 - 1224 = 58 px
dV = 1053 - 1024 = 29 px

AK1_global.X = 105 + 58 * 0.00345 = 105.2001 mm
AK1_global.Y = 1200 + 29 * 0.00345 = 1200.10005 mm
```

### Step 2. Cell Local 좌표 생성

기판 내부의 모든 Cell은 첫 Cell 위치와 Pitch를 기반으로 생성한다. Cell Index는 Excel 예제처럼 곱셈 인덱스로 사용한다.

```text
P_local.X = CellFirstX + CellColumnIndex * CellPitchX + PatternOffsetX
P_local.Y = CellFirstY + CellRowIndex    * CellPitchY + PatternOffsetY
```

Excel 예제:

```text
ColumnIndex = 14
RowIndex    = 9

P_local.X = 50 + 14 * 50 + 10 = 760 mm
P_local.Y = 35 + 9  * 45 + 0  = 440 mm
```

### Step 3. 기판 회전 보정 후 Stage Global Target 생성

Review Vision에서 계산된 `theta_align`으로 Recipe Local 좌표를 회전한 뒤 AK1 Stage Anchor에 더한다.

```text
R(theta) = [ cos(theta)  -sin(theta) ]
           [ sin(theta)   cos(theta) ]

P_target_global = P_AK1_global + R(theta_align) * P_local
```

Excel 예제:

```text
R * P_local = (759.615738, 440.663057)
P_target_global = (864.815838, 1640.763107)
```

### Step 4. Scanner 선택

각 Scanner는 Stage Global 좌표계 기준으로 고정된 중심 `C_i`를 가진다. Target이 Scanner Field 범위에 들어오는지 확인하고, 들어오는 Scanner를 선택한다.

```text
D_i = P_target_global - C_i

InField_i =
  abs(D_i.X) <= FieldHalfX
  and
  abs(D_i.Y) <= FieldHalfY
```

Excel 예제에서는 Target이 H5 중심 `(879.7, 1640.1)` 근처에 있으므로 H5가 선택된다.

```text
D_H5.X = 864.815838 - 879.7 = -14.884162
D_H5.Y = 1640.763107 - 1640.1 = 0.663107
```

### Step 5. Odd/Even Zigzag 배치에 따른 Scanner Gx/Gy 생성

Multi Scanner는 넓은 기판을 커버하기 위해 홀수열/짝수열이 Zigzag로 배치될 수 있다. 이때 Scanner의 물리 장착 방향 또는 광학 좌표 방향이 다르면 Gx/Gy 부호 규칙이 달라진다.

```text
Odd Scanner:
  +Gx = -StageRelativeX
  +Gy = +StageRelativeY

Even Scanner:
  +Gx = +StageRelativeX
  +Gy = -StageRelativeY
```

행렬로 표현하면 다음과 같다.

```text
Odd  S = [ -1  0 ]
         [  0 +1 ]

Even S = [ +1  0 ]
         [  0 -1 ]

P_scanner = S_i * (P_target_global - C_i)
```

Excel 예제 H5는 Odd Scanner이므로:

```text
P_scanner.X = -(-14.884162) = 14.884162
P_scanner.Y = +(0.663107)   = 0.663107
```

### Step 6. Review 측정 결과 기반 Offset 적용

가공 후 Review 측정에서 목표 대비 오차가 확인되면, Global Error를 계산하고 다음 가공 명령에 반대 방향 보정값으로 반영한다.

```text
E_global = P_review_measured_global - P_target_global
Offset_process = -E_global

P_target_corrected = P_target_global + Offset_process
```

이 보정은 다음 두 방식 중 하나로 구조화할 수 있다.

1. Stage Global Target 자체에 보정값을 반영한 뒤 Scanner 변환을 다시 수행한다.
2. Scanner 좌표계로 변환한 뒤 Scanner별 부호 규칙을 적용하여 Gx/Gy 보정량으로 반영한다.

Base 코드 관점에서는 1번 방식이 이해와 검증이 쉽다. 모든 보정값을 Stage Global 좌표계에서 관리하면 Review Camera, Recipe, Scanner 사이의 책임 경계가 명확하다.

## 4. 전체 처리 Pipeline

```text
Recipe / Cell Definition
  -> AK1 Review Measurement
  -> AK1 Stage Anchor
  -> Cell Local Grid Generation
  -> Rotation Compensation
  -> Stage Global Target
  -> Scanner Field Matching
  -> Odd/Even Scanner Transform
  -> MOF Gx/Gy Command
  -> Review Measurement
  -> Global Error
  -> Process Offset Update
```

## 5. WPF 예제 코드 구성

다운로드 폴더의 `MOF_COORDINATE_WPF_SAMPLE` 프로젝트는 다음 항목을 제공한다.

- 입력 변수 패널: Board, Review Camera, Cell, Scanner 변수를 입력한다.
- 좌표 생성 버튼: 모든 Cell의 Stage Global 좌표와 Scanner Gx/Gy를 계산한다.
- DataGrid: Cell별 Local, Global, Scanner, Gx, Gy, InField 결과를 표시한다.
- Canvas: 기판, AK, Cell Target, Scanner Center, Field 범위를 간단히 시각화한다.
- 코드 주석: 좌표 변환 공식과 클래스 책임을 설명한다.

## 6. 설계상 핵심 포인트

- Recipe는 제품 기준 좌표만 가진다. Stage 또는 Scanner 물리 치수를 직접 알 필요가 없다.
- Review Measurement는 실제 기판이 Stage 위에 놓인 위치와 회전각을 제공한다.
- Process Plan은 Recipe Local 좌표를 Stage Global Target으로 확장한 결과다.
- Scanner Model은 고정된 Scanner Center, Field 범위, Odd/Even 부호 규칙을 가진다.
- Review Offset은 Stage Global 기준으로 누적/관리하는 것이 추적성이 좋다.
- 최종 MOF Command는 `Stage Target -> Scanner Relative -> Gx/Gy` 순서로 생성한다.

