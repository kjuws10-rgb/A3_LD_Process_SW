@echo off
setlocal
set HOST=192.168.10.10
set PORT=12200

echo [1/2] Ping %HOST%
ping -n 2 %HOST%

echo.
echo [2/2] Automation1 endpoint TCP reachability %HOST%:%PORT%
powershell -NoProfile -Command "Test-NetConnection -ComputerName '%HOST%' -Port %PORT% | Format-List ComputerName,RemoteAddress,RemotePort,PingSucceeded,TcpTestSucceeded"

echo.
echo TcpTestSucceeded only verifies network reachability.
echo Use A1 Studio or the WPF 'Automation1 direct connect' button for protocol validation.
pause
