# Talon Laser Manual Protocol Analysis

## 분석 기준

- 자료: `manual/1._Laser_[기술자료_662339_20280617].zip`
- 내부 문서: `Talon Users Manual Rev. C 90065281.pdf`
- 적용 범위: Chapter 4 Serial Communication, Chapter 9 System Event Codes, Appendix A Programming Reference Guide

## 물리/직렬 통신

| 항목 | 매뉴얼 기준 | 드라이버 반영 |
| --- | --- | --- |
| 권장 Port | RS-232 | `CSerialTalonTransport` |
| Baud Rate | 115200 default | UI 기본 115200, 허용값 검사 |
| Parity | None | `Parity.None` |
| Data Bits | 8 | 8 |
| Stop Bits | 1 | `StopBits.One` |
| Flow Control | None | `Handshake.None` |
| TX 종료 | CR, `0x0D` | 모든 명령 뒤 `\r` 전송 |
| RX 종료 | CR 또는 LF | 두 종료문자를 모두 인식 |
| Cable | Straight-through DB9 | 설치 체크 항목으로 문서화 |
| Case | 대소문자 비구분 | 카탈로그는 매뉴얼 표기 사용 |

## Query와 Set 명령의 차이

Query는 응답을 반환하지만 매뉴얼의 Set/Action 예제는 별도 ACK 응답을 정의하지 않는다. 따라서 기존 공통 Serial 코드처럼 모든 명령에서 `ReadLine`을 기다리면 정상 Set 명령도 Timeout으로 기록될 수 있다.

검증 드라이버는 다음과 같이 분리한다.

```text
Query        → CR 전송 → CR/LF 응답 대기 → Parsing → Status 반영
Set/Action   → CR 전송 → Flush 완료 → SENT 반환 → 필요 시 Query로 Readback
```

실제 설비 운영에서는 설정 명령 뒤 대응 Query를 보내 Readback을 비교하는 것이 안전하다.

## 핵심 명령

| 기능 | Set/Action | Query | 응답/범위 |
| --- | --- | --- | --- |
| 식별 | - | `*IDN?` | 제조사, 모델, 시리얼, 버전 |
| 상태 | - | `?F` | Event String |
| 상태 이력 | - | `?FH` | 최신 순서 16개 Event Code |
| 상태 비트 | - | `*STB?` | Decimal bit field |
| Emission | `ON`, `OFF` | `?D` | 0/1 |
| Diode Current | `C1:<f>` | `?C1`, `?CS1`, `?DCL1` | A, DCL 이하 |
| Internal PRF | `Q:<n>` | `?Q` | 0~2,000,000 Hz, 0=external trigger |
| EPRF | `EPRF:<f>` | `?EPRF` | 모델별 최소~500,000 Hz |
| Q Mode | `QMODE:<n>` | `QMODE?` | 0, 1, 2 |
| Gate | `G:<n>` | `?G` | OPEN/CLOSED |
| External Gate | `GEXT:<n>` | `?GEXT` | 0/1 |
| Shutter | `SHT:<n>` | `?SHT` | 0/1 |
| Output Power | - | `?P` | W |
| Diode Temp | - | `?T1` | °C |
| Tower Temp | - | `?TT` | °C |
| Chassis Temp | - | `?CT` | °C |
| Warm-up | - | `?WARMUPTIME` | seconds |
| SHG | `SHG:<n>` | `?SHG` | 20,000~65,535 count |
| SHG Autotune | `SAUTO:<n>` | `?SAUTO` | 0/1 |
| THG Spot | `MTR:TSPOT:<n>` | `?MTR:TSPOT` | 1~15 |
| THG Spot Hours | - | `?MTR:THR` | hours |
| 설정 저장 | `SAVE` | - | Flash 기본값 변경 |

## 상태비트

`*STB?`의 Decimal 응답을 bit field로 해석한다.

| Bit | 의미 |
| --- | --- |
| 0 | Emission |
| 1 | Shutter state |
| 2 | Gate |
| 3 | SHG warming up |
| 4 | External gate |
| 5 | System fault |
| 6 | SHG autotune |
| 7 | THG autotune |
| 9 | Motor moving |

## 주요 Event Code

| Code | Event | 운영 판단 |
| --- | --- | --- |
| 000 | System Ready | ON 검토 가능 |
| 011 | SYS ILK | System interlock 확인, 출력 금지 |
| 013 | KEY ILK | Enable key 확인, 출력 금지 |
| 031 | DIODE 1 TEMP ERROR | 냉각조건 확인, 출력 금지 |
| 036 | TOWER TEMP ERROR | Chiller/Crystal tower 확인 |
| 037 | CHASSIS TEMP ERROR | 장착/방열 확인 |
| 070 | THG SPOT HRS WARN 1 | Spot 교체 계획 |
| 071 | THG SPOT HRS WARN 2 ERROR | 새 Spot 이동 필요 |
| 072 | THG SPOT HRS SHUTDOWN | 즉시 새 Spot 이동, 재점등 차단 |
| 128 | TOWER TEMP WARNING | 온도조건 확인 |
| 134 | SHUTTER IN UNEXPECTED STATE | Shutter 설치/배선/설정 확인 |
| 135 | SHG Hardware Problem | 전원 재기동 후 Service 검토 |
| 137 | THG RECOVERY | Motor recovery 완료 대기 |
| 181 | DIODE TEMP WARNING | 냉각조건 확인 |
| 201 | SN DOES NOT MATCH | Configuration/flash 확인 |
| 207 | SHUTTER INTERLOCK | Analog pin 6/15 확인 |

## 기존 코드와의 차이

| 기존 구현 | 매뉴얼 분석 결과 | 개선 방향 |
| --- | --- | --- |
| `GetQMode => ?QMODE` | Rev. C 명령은 `QMODE?` | 카탈로그에서 `QMODE?` 사용 |
| `?FH`를 `ReadInt`로 처리 | 16개 code의 세미콜론 목록 | `IReadOnlyList<int>`로 Parsing |
| 모든 Serial 명령에서 응답 대기 | Set/Action은 ACK가 정의되지 않음 | Query만 응답 대기 |
| `ReadLine` 종료문자 1개 | 응답은 CR 또는 LF | byte 단위 CR/LF 동시 처리 |
| Parameter 범위 일부만 표현 | 명령별 범위/위험도가 다름 | Metadata Catalog에서 선검증 |
| Simulation 결과가 단순 문자열 | 상태 전이/오류 주입 부족 | Stateful simulator와 단발 오류 주입 |
| 출력 명령의 별도 안전 Context 없음 | ON은 System Ready와 Interlock 전제 | 네 조건 안전 잠금 및 Ready 검사 |

## 실제 장비 검증 순서

1. Straight-through RS-232 cable과 `115200/N/8/1/None`을 확인한다.
2. `*IDN?`, `?BAUDRATE`로 장비와 속도를 확인한다.
3. `?F`, `?FH`, `*STB?`로 Ready/Fault 상태를 확인한다.
4. `?T1`, `?TT`, `?CT`, `?WARMUPTIME`으로 열 안정화를 확인한다.
5. `?SHT`, `?G`, `?GEXT`, `?D`로 출력경로 상태를 확인한다.
6. 위 단계가 모두 통과하기 전에는 `ON` 또는 출력 증가 명령을 실행하지 않는다.
7. Set 명령은 대응 Query로 Readback을 검증하고 Trace를 보관한다.
8. `SAVE`, THG Spot 이동, Autotune은 일상 Poll과 분리하여 승인된 유지보수 절차에서만 실행한다.
