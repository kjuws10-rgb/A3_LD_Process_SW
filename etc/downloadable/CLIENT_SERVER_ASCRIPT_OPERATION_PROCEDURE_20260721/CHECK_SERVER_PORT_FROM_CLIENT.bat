@echo off
echo [1/2] Automation1 Controller native endpoint 12200
powershell -NoProfile -Command "Test-NetConnection 192.168.10.10 -Port 12200 | Format-List ComputerName,RemoteAddress,RemotePort,TcpTestSucceeded"
echo.
echo [2/2] A3 AeroScript Gateway 46100 - Client WPF must connect here
powershell -NoProfile -Command "Test-NetConnection 192.168.10.10 -Port 46100 | Format-List ComputerName,RemoteAddress,RemotePort,TcpTestSucceeded"
echo.
echo Both ports have different protocols. Do not enter 12200 in the Client WPF Script Gateway Port.
pause
