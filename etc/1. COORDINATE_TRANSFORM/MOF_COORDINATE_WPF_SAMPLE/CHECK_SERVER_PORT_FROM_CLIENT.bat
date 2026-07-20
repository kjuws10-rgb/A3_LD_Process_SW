@echo off
powershell -NoProfile -Command "Test-NetConnection 192.168.10.10 -Port 46100 | Format-List ComputerName,RemoteAddress,RemotePort,TcpTestSucceeded"
pause
