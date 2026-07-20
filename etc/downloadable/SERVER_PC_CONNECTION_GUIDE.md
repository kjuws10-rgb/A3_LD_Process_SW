# Automation1 Server PC 연결 가이드

## 현재 오류의 의미

`대상 컴퓨터에서 연결을 거부했습니다`는 Client가 `192.168.10.10:46100`까지 도달했지만 해당 TCP Port에서 수신 중인 프로그램이 없거나 방화벽이 차단한 상태를 뜻한다. MDK 설치, 라이선스 인증, Ping 성공은 이 프로젝트의 `Automation1Server` 실행 여부와 별개이다.

## Server PC 준비

1. 개발 PC에서 `PUBLISH_AUTOMATION1_SERVER_WIN64.bat`를 실행한다.
2. 생성된 `ServerDeployment\win-x64` 폴더 전체를 Server PC `192.168.10.10`으로 복사한다.
3. Server PC에서 `OPEN_SERVER_FIREWALL_PORT_46100_ADMIN.bat`를 관리자 권한으로 한 번 실행한다.
4. Server PC에서 `START_AUTOMATION1_SERVER.bat`를 실행하고 Client UI와 동일한 API Key를 입력한다.
5. 콘솔에 `Automation1 Script Server: 0.0.0.0:46100`이 표시된 상태로 창을 계속 열어 둔다.

실제 Automation1 .NET API DLL 자동 탐색이 실패하면 Server 실행 전에 `AUTOMATION1_NET_DLL` 환경 변수에 `Aerotech.Automation1.dll`의 전체 경로를 지정한다.

## Client PC 확인

1. `CHECK_SERVER_PORT_FROM_CLIENT.bat`를 실행한다.
2. `TcpTestSucceeded : True`인지 확인한다. Ping 성공만으로는 부족하다.
3. WPF의 Server PC Host를 `192.168.10.10`, Port를 `46100`, API Key를 Server와 동일하게 입력한다.
4. `Server 연결 확인`을 먼저 누른다.
5. 성공 후 `Script 생성`, `Server 전송`, `Server 실행`, `상태 조회` 순서로 확인한다.

## 경로 구분

- `Local Script File`: Client PC에 실제 생성되는 `.ascript` 파일이다.
- `Controller File`: 전송 후 Server가 Automation1 Controller File System에 기록하는 경로이다.
- `programs/mof_generated.ascript`는 Client의 `D:\` 파일 경로가 아니다.

## 좌표 구분

- `Process Gx/Gy`: Scanner 축 명령에 사용하는 좌표이며 `G0 GX ... GY ...`에 기록된다.
- `Review Coordinate`: 선택 Head/DOE 기준의 Review 측정 및 표시 좌표이다.
- 두 좌표는 기준 원점과 Camera-Scanner 물리 Offset이 다르므로 값이 서로 달라야 한다.
