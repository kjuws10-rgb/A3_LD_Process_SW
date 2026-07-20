@echo off
setlocal
cd /d "%~dp0"

if "%A1_SCRIPT_API_KEY%"=="" set /p A1_SCRIPT_API_KEY=Enter the same API Key used by Client UI: 
if "%A1_SCRIPT_API_KEY%"=="" (
  echo API Key is required.
  pause
  exit /b 1
)

echo Starting Automation1Server on all interfaces, TCP 46100.
echo Keep this window open while the Client uses the server.
Automation1Server.exe --runtime=automation1 --mode-policy=HardwareOnly --bind=0.0.0.0 --port=46100 --api-key=%A1_SCRIPT_API_KEY%
if errorlevel 1 (
  echo.
  echo Server stopped with an error. Check the Automation1 .NET API assembly path and controller connection.
  pause
)
