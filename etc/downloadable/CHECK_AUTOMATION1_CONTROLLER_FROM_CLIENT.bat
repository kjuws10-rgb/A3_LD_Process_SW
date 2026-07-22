@echo off
setlocal
set HOST=192.168.10.10
set PORT=12200
ping -n 2 %HOST%
powershell -NoProfile -Command "Test-NetConnection -ComputerName '%HOST%' -Port %PORT% | Format-List ComputerName,RemoteAddress,RemotePort,PingSucceeded,TcpTestSucceeded"
echo Use A1 Studio or the WPF direct-connect button for Automation1 protocol validation.
pause
