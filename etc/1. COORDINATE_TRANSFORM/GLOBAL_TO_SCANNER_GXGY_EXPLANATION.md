# Global Process Coordinate to Scanner GX/GY Explanation

## 1. 사용자가 궁금해한 핵심

우리는 Review Camera 기준 AK1 측정값으로 기판의 실제 Stage Global 위치를 만들고, Recipe/PPID의 Cell 및 Pixel 정보를 이용해서 각 가공점의 Stage Global 좌표를 만들었다.

여기서 질문은 다음이다.

```text
Stage Global 기준 가공 좌표가 실제 Aerotech Scanner의 GX/GY 지령으로 어떻게 바뀌는가?
```

결론부터 말하면, `GX/GY`는 Stage Global 좌표를 그대로 쓰는 값이 아니다. 해당 가공점을 담당하는 Scanner Head의 중심을 원점으로 다시 잡은 상대좌표이다.

## 2. Stage Global 좌표와 Scanner 좌표의 차이

Stage Global 좌표계는 장비 전체 기준 좌표이다. Review Camera 중심, Scanner 중심, 기판 AK1, Cell 가공점이 모두 같은 기준에 놓인다.

Scanner GX/GY 좌표계는 각 Scanner Head 내부의 작은 field 좌표계이다. Scanner는 자기 중심을 기준으로 “얼마나 왼쪽/오른쪽, 위/아래로 움직일지”를 명령받는다.

따라서 변환의 첫 단계는 원점 이동이다.

```text
C_scanner_i = i번 Scanner 중심의 Stage Global 좌표
P_process_global = Stage Global 기준 가공 목표점

D_i = P_process_global - C_scanner_i

D_i.X = TargetStageX - ScannerCenterX
D_i.Y = TargetStageY - ScannerCenterY
```

`D_i`는 “목표점이 i번 Scanner 중심에서 얼마나 떨어져 있는가”를 뜻한다. 이 값이 Scanner field 범위 안에 들어와야 해당 Scanner가 그 점을 가공할 수 있다.

## 3. Scanner Local Vector에서 GX/GY Command 생성

`D_i`가 바로 최종 `GX/GY`가 되는 것은 아니다. 실제 장비에서는 다음 요소들이 들어간다.

- Scanner 축 방향과 Stage 축 방향의 부호 차이
- Odd/Even zigzag 배치에 따른 좌표 부호 차이
- 광학계 반전
- Galvo calibration scale
- Scanner 회전 보정
- Head별 local offset

이를 수식으로 쓰면 다음과 같다.

```text
[GXcmd, GYcmd] = S_i * R_cal_i * D_i + ScannerLocalOffset_i
```

여기서:

- `S_i`: Head별 scale/sign matrix
- `R_cal_i`: Scanner rotation/calibration matrix
- `D_i`: Stage Global target에서 Scanner 중심을 뺀 상대좌표
- `ScannerLocalOffset_i`: 필요 시 적용하는 head 내부 field offset

즉 우리가 프로그램에서 계산한 최종 가공 좌표 `Gx`, `Gy`는 다음 계층의 결과이다.

```text
Recipe/Review coordinate
-> ProcessStageGlobal
-> ProcessStageGlobal - ScannerCenterGlobal
-> ScannerLocalVector
-> sign / scale / rotation / calibration
-> GX/GY command
```

## 4. AeroScript에서 GX/GY가 쓰이는 위치

Head별 축 이름은 다음처럼 고정했다.

```text
Scanner#1 -> Task1 -> GX1, GY1
Scanner#2 -> Task2 -> GX2, GY2
Scanner#8 -> Task8 -> GX8, GY8
```

현장 AUX MOF script에서는 Scanner 명령이 보통 다음 형태로 들어간다.

```text
MoveRapid([GY1, GX1], [GYcmd, GXcmd])
```

또는 G-code 계열로 보면:

```text
G0 GX1 GXcmd GY1 GYcmd
```

중요한 점은 `GXcmd/GYcmd`가 Stage Global X/Y가 아니라 Scanner field 내부 좌표라는 것이다.

## 5. AUX Feedback의 역할

외부 Stage Y축은 Aerotech 축이 아니라 다른 maker 제품이다. 그래서 Automation1이 Stage Y를 직접 `MoveAbsolute(Y, ...)`로 움직이는 구조가 아니다.

대신 외부 Stage encoder A/B 신호가 Scanner의 AUX 입력으로 들어간다. 이 값은 Stage가 실제로 얼마나 이동했는지를 알려준다.

```text
AuxFeedback_count = StageTravel_mm * ExternalEncoderCountsPerUnit
ExternalEncoderCountsPerUnit = 16000 cts/mm
```

이 AUX feedback은 `GX/GY` 좌표를 직접 만드는 값이 아니다. 역할은 “Stage가 어느 Y 위치까지 지나왔는지”를 알려주는 동기화 기준이다.

MOF 가공 흐름은 다음과 같다.

1. WPF가 Review/Recipe/Scanner geometry를 이용해 각 가공점의 `GXcmd/GYcmd`를 미리 계산한다.
2. 외부 Stage가 기판을 Y 방향으로 움직인다.
3. Automation1은 `StatusGetAxisItem(GYi, AxisStatusItem.AuxiliaryFeedback)`으로 Stage 이동량을 본다.
4. Stage가 특정 위치에 도달하면 `wait(AuxFeedback > thresholdCount)` 조건이 풀린다.
5. 그 순간 Scanner는 미리 계산된 `GXcmd/GYcmd`로 이동하고 Laser shot을 수행한다.

정리하면:

```text
GX/GY command = Scanner field 안에서 어디를 쏠지 결정하는 좌표
AUX feedback  = 움직이는 기판이 언제 그 위치에 도달했는지 알려주는 동기화 기준
Laser/PSO     = 그 타이밍에 실제 shot을 발생시키는 출력 기준
```

## 6. Aerotech 내부 MOF 보정 개념

Aerotech 쪽에서는 다음과 같은 설정이 외부 Stage encoder와 Scanner 보정 동작을 연결한다.

```text
DriveSetAuxiliaryFeedback(GYi, 0)
GalvoEncoderScaleFactorSet(GYi, scale)
StatusGetAxisItem(GYi, AxisStatusItem.AuxiliaryFeedback)
```

개념적으로는 외부 Stage encoder count를 Scanner 쪽에 알려주고, Scanner가 움직이는 기판 위에서 상대 위치를 맞출 수 있도록 해준다.

따라서 내부 동작은 다음처럼 이해하면 된다.

```text
Stage movement feedback enters through AUX.
Automation1 knows how far the moving board has traveled.
The scanner command tells where inside the scanner field to shoot.
The wait/feedback condition tells when to shoot that scanner coordinate.
```

## 7. 최종 결론

Global 가공 좌표를 GX/GY로 바꾸는 이론적 원리는 좌표계 변환이다.

```text
1. Stage Global target을 만든다.
2. 해당 target을 담당할 Scanner Head를 선택한다.
3. target에서 Scanner center를 빼서 Scanner local vector를 만든다.
4. Scanner 축 부호, scale, rotation, calibration을 적용한다.
5. 그 결과를 GX/GY command로 AeroScript에 쓴다.
6. AUX feedback은 해당 command를 실행할 Stage Y 타이밍을 결정한다.
```

즉:

```text
GX/GY = 어디를 쏠지
AUX   = 언제 쏠지
```
