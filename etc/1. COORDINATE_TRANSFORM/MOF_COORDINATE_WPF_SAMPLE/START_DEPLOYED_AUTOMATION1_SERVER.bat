@echo off
setlocal
cd /d "%~dp0"

if "%A1_SCRIPT_API_KEY%"=="" set /p A1_SCRIPT_API_KEY=Enter the same API Key used by Client UI: 
if "%A1_SCRIPT_API_KEY%"=="" (
  echo API Key is required.
  pause
  exit /b 1
)

echo Select the server policy that matches the Client Script Mode.
echo   1. VirtualOnly  - Virtual Wait Simulation, no Laser/PSO/HW Aux (default)
echo   2. HardwareOnly - Hardware Coordinate Program for validated equipment
set /p MODE_CHOICE=Mode [1/2]: 
set "MODE_POLICY=VirtualOnly"
if "%MODE_CHOICE%"=="2" set "MODE_POLICY=HardwareOnly"

echo Starting Automation1Server on all interfaces, TCP 46100, %MODE_POLICY%.
echo Keep this window open while the Client uses the server.
Automation1Server.exe --runtime=automation1 --mode-policy=%MODE_POLICY% --bind=0.0.0.0 --port=46100 --api-key=%A1_SCRIPT_API_KEY%
if errorlevel 1 (
  echo.
  echo Server stopped with an error. Check the Automation1 .NET API assembly path and controller connection.
  pause
)
