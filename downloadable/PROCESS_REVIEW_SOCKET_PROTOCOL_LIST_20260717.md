# Process / Review Data I/F Protocol List

작성일: 2026-07-17  
대상: A3 LD Process SW 기준 Process PC ↔ Review/Vision PC 간 Socket 통신 프로토콜 목록 정리

## 1. 검토 기준

현재 코드와 지금까지 정리한 좌표계 자료를 기준으로 보면, Process / Review I/F는 다음 데이터를 주고받아야 한다.

- Process 쪽은 Recipe, Cell 좌표, Scanner 배치, DOE 기준, Process Plan, 가공 상태를 관리한다.
- Review 쪽은 AK 측정, 가공 후 측정점 결과, 오차, 보정용 Offset 계산에 필요한 원천 데이터를 제공한다.
- 현재 코드에는 `CInterfaceManager`, `CSocketComm`, `ST_INTERFACE_DATA`, `ST_INTERFACE_CONNECT_OPTION` 등 일반 통신 기반이 있다.
- Review 보정 Base 구조는 `ST_REVIEW_MEASUREMENT_POINT`, `ST_REVIEW_MEASUREMENT_BATCH`, `ST_REVIEW_OFFSET_POLICY`, `ST_REVIEW_HEAD_OFFSET`, `ST_REVIEW_CORRECTION_RESULT`로 정리되어 있다.
- WPF 샘플 기준 좌표 데이터는 `CoordinateInput`, `ScannerModel`, `DoeBeamModel`, `CellCommand`, `CoordinateResult` 구조로 설명된다.

따라서 프로토콜은 단순 문자열 명령보다, 메시지 헤더와 JSON Payload를 가진 명확한 Request / Response / Event 구조로 정의하는 것이 좋다.

## 2. 권장 Socket Rule

### 2.1 기본 방식

권장 방식:

```text
TCP Socket + UTF-8 JSON + Length Prefix
```

이유:

- 좌표 목록, Review 결과 목록은 데이터가 커질 수 있으므로 단순 `Read 1회` 또는 `4096 byte buffer` 방식으로는 부족하다.
- JSON은 사람이 로그로 읽기 쉽고, 장비 간 협의가 쉽다.
- Length Prefix를 붙이면 메시지 끝을 안정적으로 판단할 수 있다.

권장 Frame:

```text
[4 byte Length][UTF-8 JSON Body]
```

대안:

```text
STX + JSON + ETX
```

단, 좌표 데이터가 커질 가능성이 있으므로 최종 기준은 Length Prefix를 추천한다.

### 2.2 공통 Header

모든 메시지는 아래 Header를 가져야 한다.

```json
{
  "protocolVersion": "1.0",
  "messageId": "PRV-20260717-000001",
  "correlationId": "REQ-20260717-000001",
  "messageType": "PROCESS_PLAN_DOWNLOAD_REQ",
  "direction": "PROCESS_TO_REVIEW",
  "timestamp": "2026-07-17T10:30:00.000+09:00",
  "machineId": "A3_LD_01",
  "processId": "PROC_20260717_001",
  "recipeId": "RCP_A3_LD_6GH",
  "lotId": "LOT001",
  "panelId": "PNL001",
  "payload": {}
}
```

필수 Header 필드:

| Field | 필수 | 설명 |
| --- | --- | --- |
| `protocolVersion` | Y | 프로토콜 버전. 초기값 `1.0` |
| `messageId` | Y | 메시지 고유 ID |
| `correlationId` | Y | 요청/응답 연결 ID. 응답은 요청 correlationId를 그대로 사용 |
| `messageType` | Y | 명령/응답/이벤트 타입 |
| `direction` | Y | `PROCESS_TO_REVIEW`, `REVIEW_TO_PROCESS` |
| `timestamp` | Y | ISO-8601 시간 |
| `machineId` | Y | 장비 ID |
| `processId` | 조건부 | 공정 단위 ID |
| `recipeId` | 조건부 | Recipe ID |
| `lotId` | 조건부 | Lot ID |
| `panelId` | 조건부 | 기판/Panel ID |
| `payload` | Y | 실제 데이터 |

### 2.3 공통 응답 규칙

모든 Request는 ACK 또는 NACK를 받아야 한다.

```json
{
  "messageType": "ACK",
  "correlationId": "REQ-20260717-000001",
  "payload": {
    "accepted": true,
    "code": "OK",
    "message": "Accepted"
  }
}
```

```json
{
  "messageType": "NACK",
  "correlationId": "REQ-20260717-000001",
  "payload": {
    "accepted": false,
    "code": "INVALID_PAYLOAD",
    "message": "Cell target list is empty"
  }
}
```

기본 Timeout / Retry 권장:

| 항목 | 권장값 |
| --- | ---: |
| Connect Timeout | 3 sec |
| Command ACK Timeout | 2 sec |
| Result Timeout | 메시지별 별도. Review 측정은 30~300 sec |
| Retry Count | 3회 |
| Heartbeat Cycle | 1 sec |
| Heartbeat Timeout | 5 sec |

## 3. 프로토콜 메시지 목록 요약

### 3.1 공통/연결 관리

| No | Message | Direction | 목적 | 우선순위 |
| ---: | --- | --- | --- | --- |
| 1 | `HELLO_REQ` | 양방향 | 연결 직후 장비/프로그램/프로토콜 버전 교환 | 필수 |
| 2 | `HELLO_RSP` | 양방향 | 상대 정보 확인 및 호환성 판단 | 필수 |
| 3 | `HEARTBEAT` | 양방향 | 연결 생존 확인 | 필수 |
| 4 | `ACK` | 양방향 | 정상 수신/접수 응답 | 필수 |
| 5 | `NACK` | 양방향 | 수신 실패/검증 실패 응답 | 필수 |
| 6 | `ERROR_EVENT` | 양방향 | 비동기 오류 통보 | 필수 |
| 7 | `TIME_SYNC_REQ` | Process → Review | 시간 동기화 요청 | 권장 |
| 8 | `TIME_SYNC_RSP` | Review → Process | Review PC 기준 시간 응답 | 권장 |

### 3.2 Recipe / Process Plan 동기화

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 9 | `RECIPE_INFO_DOWNLOAD_REQ` | Process → Review | 현재 Recipe 기본 정보 전달 | recipeId, productId, boardSize, cell layout |
| 10 | `RECIPE_INFO_DOWNLOAD_RSP` | Review → Process | Recipe 수신/검증 결과 | accepted, reason |
| 11 | `PROCESS_PLAN_DOWNLOAD_REQ` | Process → Review | 이번 기판/공정의 Process Plan 전달 | processId, lotId, panelId, selected heads |
| 12 | `PROCESS_PLAN_DOWNLOAD_RSP` | Review → Process | Plan 수신/검증 결과 | accepted, missingFields |
| 13 | `CELL_TARGET_LIST_DOWNLOAD_REQ` | Process → Review | 모든 가공 대상 좌표 목록 전달 | cellCommands[] |
| 14 | `CELL_TARGET_LIST_DOWNLOAD_RSP` | Review → Process | 좌표 목록 수신 결과 | targetCount, accepted |
| 15 | `SCANNER_LAYOUT_SYNC_REQ` | Process → Review | Scanner 배치/Field 정보 전달 | scanners[] |
| 16 | `SCANNER_LAYOUT_SYNC_RSP` | Review → Process | Scanner 정보 수신 결과 | accepted |
| 17 | `DOE_CONFIG_SYNC_REQ` | Process → Review | DOE 16 Beam 기준 정보 전달 | beamPitchX/Y, beams[] |
| 18 | `DOE_CONFIG_SYNC_RSP` | Review → Process | DOE 설정 수신 결과 | accepted |

### 3.3 Align / AK 측정

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 19 | `AK_MEASURE_REQ` | Process → Review | AK1 또는 AK1~AK4 측정 요청 | akIds, expectedRoi, pixelScale |
| 20 | `AK_MEASURE_STARTED` | Review → Process | AK 측정 시작 이벤트 | batchId |
| 21 | `AK_MEASURE_RESULT` | Review → Process | AK 측정 결과 전달 | akResults[], thetaAlignDeg |
| 22 | `AK_MEASURE_FAIL` | Review → Process | AK 검출 실패 통보 | reason, imageId |
| 23 | `ALIGN_RESULT_CONFIRM_REQ` | Process → Review | Process가 계산한 AK1 Stage/Theta 확인 요청 | ak1Stage, theta |
| 24 | `ALIGN_RESULT_CONFIRM_RSP` | Review → Process | Review가 기준 좌표 수신 확인 | accepted |

### 3.4 Review 측정 요청/진행/결과

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 25 | `REVIEW_MEASURE_REQ` | Process → Review | 가공 후 Review 측정 요청 | mode, points[], tolerance |
| 26 | `REVIEW_POINT_LIST_DOWNLOAD_REQ` | Process → Review | Review 대상 좌표만 별도 전달 | reviewPoints[] |
| 27 | `REVIEW_MEASURE_START_CMD` | Process → Review | Review 측정 시작 명령 | batchId |
| 28 | `REVIEW_MEASURE_ABORT_CMD` | Process → Review | Review 측정 중지 명령 | batchId, reason |
| 29 | `REVIEW_MEASURE_STARTED` | Review → Process | Review 측정 시작 이벤트 | batchId |
| 30 | `REVIEW_MEASURE_PROGRESS` | Review → Process | 진행률/현재 측정점 통보 | measuredCount, totalCount |
| 31 | `REVIEW_POINT_RESULT` | Review → Process | 측정점 단위 결과 이벤트 | one point result |
| 32 | `REVIEW_MEASUREMENT_BATCH_RESULT` | Review → Process | 측정 Batch 전체 결과 | ST_REVIEW_MEASUREMENT_BATCH 대응 |
| 33 | `REVIEW_MEASURE_COMPLETED` | Review → Process | Review 측정 완료 이벤트 | batchId, resultSummary |
| 34 | `REVIEW_MEASURE_FAILED` | Review → Process | Review 측정 실패 이벤트 | batchId, reason |

### 3.5 Offset 계산/적용

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 35 | `OFFSET_POLICY_SET_REQ` | Process → Review | Offset 계산 정책 전달 | gain, maxOffset, minSample |
| 36 | `OFFSET_POLICY_SET_RSP` | Review → Process | 정책 수신 결과 | accepted |
| 37 | `OFFSET_CALC_REQ` | Process → Review | Review 측정 결과 기반 Offset 계산 요청 | batchId, policy |
| 38 | `OFFSET_CALC_RSP` | Review → Process | Head별 Offset 계산 결과 | headOffsets[] |
| 39 | `OFFSET_APPLY_REQ` | Process → Review | Process가 적용할 Offset 통보 | appliedOffsets[] |
| 40 | `OFFSET_APPLY_RSP` | Review → Process | Review가 적용값 저장/동기화 완료 | accepted |
| 41 | `OFFSET_ROLLBACK_REQ` | Process → Review | 보정값 되돌리기 요청 | batchId, reason |
| 42 | `OFFSET_ROLLBACK_RSP` | Review → Process | Rollback 처리 결과 | accepted |

### 3.6 공정 상태/동기화

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 43 | `PROCESS_READY_REQ` | Process → Review | Review가 공정 준비 가능한지 확인 | processId |
| 44 | `PROCESS_READY_RSP` | Review → Process | 준비 상태 응답 | ready, reason |
| 45 | `PROCESS_START_EVENT` | Process → Review | 가공 시작 통보 | processId, panelId |
| 46 | `PROCESS_STEP_EVENT` | Process → Review | 현재 공정 단계 통보 | stepName, stepNo |
| 47 | `PROCESS_COMPLETE_EVENT` | Process → Review | 가공 완료 통보 | processId |
| 48 | `PROCESS_ABORT_EVENT` | Process → Review | 가공 중단/알람 통보 | reason, alarmCode |
| 49 | `REVIEW_READY_EVENT` | Review → Process | Review 장비 준비 완료 통보 | status |
| 50 | `REVIEW_BUSY_EVENT` | Review → Process | Review 장비 측정 중 통보 | batchId |

### 3.7 상태 조회/알람/로그

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 51 | `STATUS_REQ` | 양방향 | 상대 시스템 상태 조회 | requestedItems |
| 52 | `STATUS_RSP` | 양방향 | 상태 응답 | online, mode, alarm |
| 53 | `ALARM_EVENT` | 양방향 | 알람 발생 통보 | alarmCode, level, message |
| 54 | `ALARM_CLEAR_EVENT` | 양방향 | 알람 해제 통보 | alarmCode |
| 55 | `LOG_EVENT` | 양방향 | 주요 동작 로그 공유 | category, message |

### 3.8 대용량/분할 전송

좌표 목록 또는 Review Point 결과가 많으면 한 메시지가 커진다. 이 경우 분할 전송 메시지를 정의해야 한다.

| No | Message | Direction | 목적 |
| ---: | --- | --- | --- |
| 56 | `BULK_TRANSFER_BEGIN` | 양방향 | 대용량 전송 시작 |
| 57 | `BULK_TRANSFER_CHUNK` | 양방향 | 분할 데이터 전송 |
| 58 | `BULK_TRANSFER_END` | 양방향 | 대용량 전송 완료 |
| 59 | `BULK_TRANSFER_ABORT` | 양방향 | 대용량 전송 중단 |

### 3.9 장비 Geometry / Calibration 동기화

Review Camera와 Scanner 사이의 물리 오프셋은 Recipe 데이터나 가공 후 Review 보정값이 아니라 장비 고유 Calibration 데이터다. Process와 Review가 서로 다른 값을 사용하면 모든 Scanner 가공좌표가 동일한 크기만큼 체계적으로 틀어질 수 있으므로 운전 시작 전에 버전과 값을 반드시 비교한다.

| No | Message | Direction | 목적 | 주요 Payload |
| ---: | --- | --- | --- | --- |
| 60 | `EQUIPMENT_GEOMETRY_SYNC_REQ` | Process → Review | Camera→Scanner 고정 물리 Offset과 Scanner 배치 Geometry 전달 | calibrationId, reviewCenter, reviewToFirstScannerOffset, scannerPitch, evenYOffset, axisSign |
| 61 | `EQUIPMENT_GEOMETRY_SYNC_RSP` | Review → Process | Calibration 버전 및 값 일치 여부 응답 | accepted, calibrationId, checksum, mismatchFields |

## 4. 핵심 Payload 정의 초안

### 4.0 `EQUIPMENT_GEOMETRY_SYNC_REQ`

고정 설비 Geometry를 동기화한다. `physicalOffset`과 `dynamicReviewCorrection`은 절대 같은 필드로 합치지 않는다.

| Field | 설명 |
| --- | --- |
| `calibrationId` | 승인된 장비 Geometry/Calibration 버전 ID |
| `reviewCenterStageX/Y` | Review Camera 광학 중심의 Stage 좌표 |
| `reviewToFirstScannerOffsetX/Y` | Review Camera 중심에서 H1 Scanner 중심까지의 고정 물리 벡터 |
| `scannerPitchX` | 인접 Scanner Head 중심의 X 설계 간격 |
| `evenScannerYOffset` | 짝수 Head의 Zigzag Y 설계 Offset |
| `axisSignX/Y` | 실제 Stage 축 방향과 좌표식 부호 정의 |
| `scannerCenters[]` | 위 Geometry에서 계산한 Head별 Stage 중심 검증값 |
| `checksum` | 양측 Geometry 데이터 동일성 확인값 |

검증식:

```text
ScannerCenter_i = ReviewCenter + CameraToScannerPhysicalOffset_i
ScannerRelative = (ProcessStage - ReviewCenter) - CameraToScannerPhysicalOffset_i
                = ProcessStage - ScannerCenter_i
```

### 4.1 `CELL_TARGET_LIST_DOWNLOAD_REQ`

Process가 Review에 전체 또는 Review 대상 가공 좌표를 전달한다.

`CellCommand`와 직접 매핑된다.

필수 필드:

| Field | 설명 |
| --- | --- |
| `targetId` | 고유 좌표 ID. 예: `Cell#1_A1` |
| `cellBlock` | Cell# 번호 |
| `cellBlockColumn` / `cellBlockRow` | Cell# 묶음의 기판 내 행/열 |
| `column` / `row` | Cell 내부 가공점 열/행 |
| `localX` / `localY` | AK1 기준 Recipe Local 좌표 |
| `designStageX` / `designStageY` | 설계 기준 Stage 좌표 |
| `processStageX` / `processStageY` | Offset 반영 후 Process Stage 좌표 |
| `reviewCameraRelativeX` / `reviewCameraRelativeY` | Process Stage 좌표를 Review Camera 중심 기준으로 표현한 값 |
| `scannerPhysicalOffsetX` / `scannerPhysicalOffsetY` | Review Camera에서 담당 Scanner 중심까지의 고정 물리 Offset |
| `scannerRelativeX` / `scannerRelativeY` | 물리 Offset을 적용한 Scanner 상대좌표 |
| `scannerIndex` / `scannerName` | 담당 Scanner |
| `scannerType` | Odd/Even |
| `gx` / `gy` | Scanner 가공 명령 좌표 |
| `reviewBasisHead` / `reviewBasisBeam` | Review 좌표 기준 |
| `reviewX` / `reviewY` | Review 좌표계 결과 |

예시:

```json
{
  "messageType": "CELL_TARGET_LIST_DOWNLOAD_REQ",
  "payload": {
    "coordinateUnit": "mm",
    "targetCount": 2,
    "targets": [
      {
        "targetId": "Cell#1_A1",
        "cellBlock": 1,
        "column": 0,
        "row": 0,
        "localX": 60.0,
        "localY": 35.0,
        "designStageX": 165.1695,
        "designStageY": 1235.1524,
        "processStageX": 165.1695,
        "processStageY": 1235.1524,
        "scannerIndex": 1,
        "scannerName": "H1",
        "scannerType": "Odd",
        "gx": 314.5305,
        "gy": -404.9476,
        "reviewBasisHead": 5,
        "reviewBasisBeam": 1,
        "reviewX": -714.3,
        "reviewY": -404.7
      }
    ]
  }
}
```

### 4.2 `AK_MEASURE_RESULT`

Review가 AK 측정 결과를 Process에 전달한다.

필수 필드:

| Field | 설명 |
| --- | --- |
| `akId` | `AK1`, `AK2`, `AK3`, `AK4` |
| `pixelU` / `pixelV` | Review 영상 좌표 |
| `stageX` / `stageY` | Review에서 환산한 Stage 좌표. Process 계산값과 비교 가능 |
| `score` | 검출 신뢰도 |
| `thetaAlignDeg` | 기판 회전 보정각 |
| `imageId` | 추적용 이미지 ID |

### 4.3 `REVIEW_MEASUREMENT_BATCH_RESULT`

`ST_REVIEW_MEASUREMENT_BATCH`와 직접 매핑된다.

필수 필드:

| Field | 설명 |
| --- | --- |
| `batchId` | Review 측정 묶음 ID |
| `processId` | Process ID |
| `recipeId` | Recipe ID |
| `productId` | Product ID |
| `mode` | `ZeroLineDefense`, `FineCorrection`, `RoughCorrection`, `SimpleCorrection`, `ApcCorrection`, `RemoteInspection` |
| `source` | `ReviewCamera`, `ApcFile`, `ManualKeyIn`, `AlignResult`, `Simulation` |
| `points[]` | 측정점 목록 |

Point 필드:

| Field | 설명 |
| --- | --- |
| `pointId` | 좌표 ID |
| `headNo` | Scanner Head 번호 |
| `cellId` | Cell 위치 ID |
| `designX` / `designY` | 설계 좌표 |
| `processX` / `processY` | Process 좌표 |
| `reviewX` / `reviewY` | Review 기준 좌표 |
| `measuredX` / `measuredY` | 실제 측정 좌표 |
| `errorX` / `errorY` | 측정 오차 |
| `toleranceX` / `toleranceY` | 허용 오차 |
| `beamNo` | DOE Beam 번호 |
| `isUsed` | Offset 계산 사용 여부 |
| `measuredAt` | 측정 시간 |

### 4.4 `OFFSET_CALC_RSP`

`ST_REVIEW_CORRECTION_RESULT`, `ST_REVIEW_HEAD_OFFSET`과 직접 매핑된다.

필수 필드:

| Field | 설명 |
| --- | --- |
| `batchId` | 어떤 Review 결과로 계산했는지 |
| `isApplicable` | 실제 적용 가능한지 |
| `message` | 계산 결과 설명 |
| `policy` | Gain, MaxOffset, MinSample 정책 |
| `headOffsets[]` | Head별 Offset |

Head Offset 필드:

| Field | 설명 |
| --- | --- |
| `headNo` | Head 번호 |
| `offsetX` / `offsetY` | 다음 Process Plan에 반영할 Offset |
| `theta` | 추후 회전 보정 확장용 |
| `sampleCount` | 계산에 사용한 Review 샘플 수 |
| `source` | ReviewCamera/APC/Manual 등 |
| `createdAt` | 생성 시간 |

## 5. 구현 우선순위 제안

### Phase 1. 연결과 상태 안정화

먼저 만들어야 할 최소 메시지:

1. `HELLO_REQ`
2. `HELLO_RSP`
3. `HEARTBEAT`
4. `ACK`
5. `NACK`
6. `STATUS_REQ`
7. `STATUS_RSP`
8. `ERROR_EVENT`

이유:

- 통신 연결 자체가 안정적이어야 이후 Plan/Review 데이터가 안전하게 오간다.
- 로그와 장애 분석을 위해 messageId/correlationId가 반드시 필요하다.

### Phase 2. 좌표/Plan 전달

다음으로 필요한 메시지:

1. `RECIPE_INFO_DOWNLOAD_REQ/RSP`
2. `PROCESS_PLAN_DOWNLOAD_REQ/RSP`
3. `SCANNER_LAYOUT_SYNC_REQ/RSP`
4. `DOE_CONFIG_SYNC_REQ/RSP`
5. `CELL_TARGET_LIST_DOWNLOAD_REQ/RSP`

이유:

- Review가 무엇을 측정해야 하는지 알려면 Process Plan, Scanner 기준, DOE 기준, 좌표 목록이 필요하다.

### Phase 3. Review 측정 결과

다음으로 필요한 메시지:

1. `AK_MEASURE_REQ`
2. `AK_MEASURE_RESULT`
3. `REVIEW_MEASURE_REQ`
4. `REVIEW_MEASURE_PROGRESS`
5. `REVIEW_POINT_RESULT`
6. `REVIEW_MEASUREMENT_BATCH_RESULT`
7. `REVIEW_MEASURE_COMPLETED`
8. `REVIEW_MEASURE_FAILED`

이유:

- Review 기능의 핵심은 측정 요청과 측정 결과 수신이다.
- Point 단위 결과와 Batch 단위 결과를 둘 다 정의해야 실시간 표시와 최종 보정을 모두 만족한다.

### Phase 4. Offset 보정

최종적으로 필요한 메시지:

1. `OFFSET_POLICY_SET_REQ/RSP`
2. `OFFSET_CALC_REQ/RSP`
3. `OFFSET_APPLY_REQ/RSP`
4. `OFFSET_ROLLBACK_REQ/RSP`

이유:

- Review 측정 결과를 단순 표시로 끝내지 않고 다음 Process Plan에 반영하려면 Offset 계산/적용/되돌리기까지 표준화해야 한다.

## 6. Process / Review 간 책임 분리

### Process PC 책임

- Recipe와 Process Plan의 원본 관리
- Cell Target 좌표 생성
- Stage 좌표와 Scanner Gx/Gy 계산
- Scanner Layout/DOE Config 기준 관리
- Review 측정 요청
- Review 결과 수신 후 Offset 적용 여부 결정
- 최종 Process Plan 업데이트

### Review PC 책임

- Camera/Algorithm 기반 AK 측정
- 지정된 Review Point 측정
- 측정 좌표, 오차, Score, 이미지 ID 제공
- 필요 시 Offset 계산 보조
- Review 상태/알람/진행률 통보

### 공동으로 맞춰야 할 기준

- 좌표 단위: `mm`
- Pixel 좌표 단위: `pixel`
- 시간 포맷: ISO-8601
- 좌표 소수점 자리수: 최소 0.001 mm, 내부 계산은 가능하면 0.000001 mm
- Head 번호: 1부터 시작
- DOE Beam 번호: 1~16, 4x4 Row-major 기준
- Cell Column/Row: 내부 계산은 0부터, 화면 표시는 A/B/C 및 1/2/3
- Error 정의: `Measured - Target`
- Offset 정의: 기본 `-Error * Gain`

## 7. 현재 코드 기준 보완 필요점

현재 `CSocketComm`는 UTF-8 문자열을 그대로 쓰고, 4096 byte buffer로 한 번 응답을 읽는 구조다. Process/Review 좌표 목록과 Review Batch Result는 4096 byte를 쉽게 넘을 수 있으므로 아래 보완이 필요하다.

1. Length Prefix 기반 메시지 프레이밍 추가
2. 대용량 좌표 목록 분할 전송 지원
3. `messageId`, `correlationId` 기반 Request/Response 매칭
4. ACK/NACK 표준 처리
5. Timeout/Retry 정책 표준화
6. JSON Schema 또는 DTO class 추가
7. Interface 로그에 `messageType`, `processId`, `panelId`, `batchId` 기록
8. Simulation Review PC 또는 Mock Socket Server 작성

## 8. 최종 프로토콜 목록 Short List

실제 협의용 최소 목록만 뽑으면 아래 24개가 핵심이다.

1. `HELLO_REQ`
2. `HELLO_RSP`
3. `HEARTBEAT`
4. `ACK`
5. `NACK`
6. `ERROR_EVENT`
7. `STATUS_REQ`
8. `STATUS_RSP`
9. `RECIPE_INFO_DOWNLOAD_REQ`
10. `PROCESS_PLAN_DOWNLOAD_REQ`
11. `SCANNER_LAYOUT_SYNC_REQ`
12. `EQUIPMENT_GEOMETRY_SYNC_REQ`
13. `EQUIPMENT_GEOMETRY_SYNC_RSP`
14. `DOE_CONFIG_SYNC_REQ`
15. `CELL_TARGET_LIST_DOWNLOAD_REQ`
16. `AK_MEASURE_REQ`
17. `AK_MEASURE_RESULT`
18. `REVIEW_MEASURE_REQ`
19. `REVIEW_MEASURE_PROGRESS`
20. `REVIEW_POINT_RESULT`
21. `REVIEW_MEASUREMENT_BATCH_RESULT`
22. `REVIEW_MEASURE_COMPLETED`
23. `REVIEW_MEASURE_FAILED`
24. `OFFSET_POLICY_SET_REQ`
25. `OFFSET_CALC_RSP`
26. `OFFSET_APPLY_REQ`

이 26개를 먼저 표준화하면 고정 설비 Geometry 확인부터 Process / Review 기본 운용, 측정, 결과 수신, 동적 보정 반영까지 한 사이클을 닫을 수 있다.
