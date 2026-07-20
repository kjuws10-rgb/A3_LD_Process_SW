@echo off
net session >nul 2>&1
if errorlevel 1 (
  echo Run this file as Administrator.
  pause
  exit /b 1
)

netsh advfirewall firewall delete rule name="A3 Automation1 Script Server TCP 46100" >nul 2>&1
netsh advfirewall firewall add rule name="A3 Automation1 Script Server TCP 46100" dir=in action=allow protocol=TCP localport=46100 profile=private
if errorlevel 1 (
  echo Firewall rule creation failed.
) else (
  echo Firewall rule created for inbound TCP 46100 on Private profile.
)
pause
