# 광학 컴포넌트 이론·동작·검증 가이드

## 1. 전체 광학계에서의 역할

```text
Talon Laser
  → Attenuator: 에너지 조절
  → Beam Expander: beam diameter/divergence 조절
  → Scanner/공정 광학계

Power Meter: 위 광경로의 출력과 위치를 독립 측정
XPS: 기판 또는 계측 위치를 stage 좌표로 이동
Picomotor: mirror/lens mount를 미세 조정하여 광축 정렬
```

이 장비들은 독립적으로 연결되지만 공정에서는 하나의 상태 흐름을 이룹니다. 레이저가 정상이어도 감쇠기 각도, expander lens 위치, stage 위치, 광축 정렬이 틀리면 가공점의 에너지와 형상이 달라집니다. 따라서 각 드라이버는 명령 성공만 기록하지 않고 실제 readback과 독립 계측 결과를 함께 저장해야 합니다.

## 2. 드라이버 설계 원칙

### 2.1 Profile과 Command Catalog 분리

Profile은 장비 자체의 고정 특성입니다. 연결 방식, 기본 endpoint, baud rate, terminator, 역할, 주의사항을 가집니다. Command Catalog는 기능 단위 데이터이며 명령 template, parameter 범위, 응답 type, 위험도를 가집니다. UI는 장비별 `switch`를 반복하지 않고 이 데이터를 읽어 자동으로 기능 목록과 설명을 구성합니다.

### 2.2 Transport 분리

- Serial: Talon, CONEX-AGP, Beam Expander, PowerMax
- TCP: XPS
- Vendor API: Picomotor CmdLib
- Simulator: 모든 장비 공통

Driver는 전송 매체를 모릅니다. 따라서 simulator에서 검증한 동일 command flow를 hardware transport로 교체할 수 있습니다.

### 2.3 명령 성공과 공정 성공 분리

통신 ACK는 장비가 문자열을 받아들였다는 의미입니다. 다음 항목은 별도 확인이 필요합니다.

1. Motion 명령 후 target/current position과 motion done 상태가 일치하는가.
2. 레이저 설정 후 내부 readback과 power meter 값이 허용 오차 안인가.
3. Beam expander step 조합이 calibration table의 배율/divergence와 맞는가.
4. Picomotor 정렬 후 beam position 또는 scanner 기준점이 개선되었는가.
5. Error/status history에 새로운 fault가 발생하지 않았는가.

## 3. 장비별 이론과 운용

### 3.1 Talon Laser

Talon은 pump diode가 laser medium에 에너지를 저장하고 Q-switch가 cavity loss를 순간적으로 낮춰 짧고 강한 pulse를 만듭니다. SHG와 THG crystal은 기본 파장을 2배/3배 주파수로 변환합니다. Crystal temperature와 spot 상태가 변환 효율에 직접 영향을 줍니다.

권장 순서:

1. 냉각수, analog interlock, area interlock, beam dump를 확인합니다.
2. `*IDN?`, `?F`, `?FH`, `*STB?`, 온도 query로 통신과 준비 상태를 확인합니다.
3. Emission OFF, shutter closed, gate closed 상태를 확인합니다.
4. PRF, Q mode, current limit를 조회하고 낮은 current부터 설정합니다.
5. Power meter가 준비된 뒤 필요한 gate/shutter/emission을 순서대로 허용합니다.
6. 내부 `?P`와 외부 power meter를 비교합니다.
7. 종료 시 gate close, shutter close, emission OFF를 확인하고 상태 이력을 저장합니다.

### 3.2 Attenuator

감쇠기는 회전 위치와 광량 사이에 비선형 관계가 있을 수 있습니다. 특히 편광 기반 감쇠는 각도와 광량이 단순 비례하지 않습니다. 따라서 위치 제어와 power calibration을 분리해야 합니다.

권장 순서:

1. Revision, error, state, current position을 조회합니다.
2. Home을 수행하고 software limit와 user unit를 확인합니다.
3. 안전한 저출력에서 작은 상대 이동으로 양의 방향이 감쇠 증가인지 감소인지 확인합니다.
4. 목표 power는 calibration curve에서 position으로 변환합니다.
5. 이동 후 TP/TS를 poll하고 power meter로 실제 값을 확인합니다.

### 3.3 Motorized Beam Expander

두 lens group의 위치는 서로 결합되어 있습니다. 한 축은 주로 divergence, 다른 축은 주로 magnification에 영향을 주지만 완전히 독립적이지 않을 수 있습니다. 원하는 광학 상태는 `(Motor1 step, Motor2 step)`의 쌍으로 관리해야 합니다.

권장 순서:

1. 전원 인가 후 반드시 `#I:`를 실행하고 `!`를 기다립니다.
2. `#7:`과 `#8:`로 현재 step counter를 읽습니다.
3. Recipe의 배율/divergence를 calibration table로 motor step 두 개로 변환합니다.
4. Motor 1 이동 후 ACK를 기다리고 Motor 2를 이동합니다.
5. 위치를 다시 조회하고 허용 오차를 검사합니다.
6. Far-field/spot 계측으로 실제 divergence와 beam size를 검증합니다.

### 3.4 Power Meter

Power meter의 숫자는 sensor 종류, wavelength correction, range, zero, saturation, averaging에 따라 달라집니다. 통신이 정상이어도 잘못된 wavelength나 포화된 range에서는 공정 판단에 사용할 수 없습니다.

권장 순서:

1. Hardware description, serial, calibration date를 기록합니다.
2. 레이저 파장 355 nm를 `3.55E-7 m`로 설정하고 다시 읽습니다.
3. Beam이 없는 상태에서 zero/baseline을 확인합니다.
4. 낮은 출력부터 측정하고 over-range/saturation을 확인합니다.
5. 정해진 sample count와 interval로 평균, 최소, 최대, 표준편차를 계산합니다.
6. Streaming 종료를 보장하고 최종 값을 recipe/result와 연결합니다.

### 3.5 XPS

XPS는 단순 좌표 송신기가 아니라 group state machine입니다. 초기화되지 않은 group은 바로 이동할 수 없고, home이 끝나야 stage 좌표가 의미를 가집니다. API return code 0은 함수가 받아들여졌다는 의미이며 이동 완료는 status와 position으로 확인해야 합니다.

권장 순서:

1. TCP connection과 firmware/controller status를 확인합니다.
2. 실제 configuration의 group/positioner 이름과 축 수를 읽습니다.
3. GroupInitialize 후 GroupHomeSearch를 실행합니다.
4. GroupStatusGet이 ready 상태인지 확인합니다.
5. software travel limit 안에서 절대 또는 상대 이동합니다.
6. position, velocity, motion status를 poll합니다.
7. timeout 또는 following error 시 abort하고 error string을 기록합니다.

### 3.6 Picomotor

Picomotor는 piezo 충격으로 나사를 미세 회전시키기 때문에 open-loop step과 실제 각도 이동이 항상 동일하지 않습니다. 하중, 마찰, 방향 전환, mount 상태가 반복성에 영향을 줍니다. 8743 closed-loop 모델은 encoder count와 deadband, update interval, following error 설정이 추가됩니다.

권장 순서:

1. CmdLib constructor로 충분한 discovery 시간을 주고 device key를 얻습니다.
2. Master를 open하고 address, model, serial, slave address를 조회합니다.
3. Motor 번호와 open/closed-loop capability를 확인합니다.
4. 작은 relative move 후 motion done, error, position을 반복 조회합니다.
5. Beam position 또는 alignment mark를 독립 계측하여 보정량을 계산합니다.
6. Stop, Close, Shutdown 순서로 background task까지 정리합니다.

## 4. 통합 검증 시나리오

### 4.1 Software-only

1. 전체 component simulator 연결
2. 모든 read-only 명령의 formatting과 response type 검사
3. parameter 최소/최대와 범위 초과 차단 검사
4. timeout과 invalid response가 trace와 UI에 전달되는지 검사
5. motion simulator에서 명령 후 readback 변화 검사
6. 안전 잠금 없이 write/motion/output 명령이 차단되는지 검사

### 4.2 Hardware commissioning

1. 장비 한 대씩 전원을 분리하여 read-only 통신 확인
2. Serial framing 또는 TCP return code 확인
3. 안전한 범위의 최소 motion/설정 변경
4. 장비 readback과 독립 계측 비교
5. 통신 단절, timeout, emergency stop 복구 확인
6. 전체 광경로를 연결하고 sequence/interlock 검증

## 5. 데이터 기록 권장 구조

```text
EquipmentTransaction
├─ Timestamp
├─ EquipmentType / SerialNumber / Firmware
├─ RecipeId / LotId / BoardId
├─ CommandId / CommandText / Parameter
├─ ResponseRaw / ParsedValue / Unit
├─ Result(Pass, Fail, Timeout, InterlockBlocked)
├─ ElapsedMs
└─ SafetyContext

EquipmentSnapshot
├─ TalonStatus
├─ AttenuatorPosition
├─ BeamExpanderMotor1/2
├─ PowerMeterValue/Wavelength/Range
├─ XpsGroupStatus/Position
└─ PicomotorAddress/Motor/Position/Error
```

공정 결과를 재현하려면 recipe 값만으로 부족합니다. 실제 장비 readback, serial/firmware, calibration 식별자, 명령 trace를 같은 timestamp 기준으로 함께 보존해야 합니다.

## 6. 현재 코드와 production 연결 지점

- `CConex_AGP`: 공통 catalog의 CONEX 명령·profile과 대응합니다.
- `CBeamExpander`: `#I:`, `#1:`, `#2:`, `#7:`, `#8:` 흐름과 대응합니다.
- `CPowerMeter`: legacy PowerMax ASCII 명령과 대응합니다.
- `CTalonLaser`: 기존 구현의 query/set 응답 차이, `QMODE?`, event history 처리를 Talon 전용 driver가 보완합니다.
- XPS/Picomotor: production 코드에 전용 adapter를 추가할 때 본 예제의 `IEquipmentTransport` 또는 동일 역할 interface를 사용하면 simulator와 UI를 재사용할 수 있습니다.

이 예제는 production root 프로젝트를 직접 변경하지 않는 독립 검증 자료입니다.
