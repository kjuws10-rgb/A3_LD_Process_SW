@echo off
setlocal
cd /d "%~dp0"
if "%A1_SCRIPT_API_KEY%"=="" (
  echo Set A1_SCRIPT_API_KEY before starting the real server.
  exit /b 1
)
echo HardwareOnly policy: Virtual Wait Simulation jobs will be rejected.
dotnet run --project "Automation1Server\Automation1Server.csproj" -- --runtime=automation1 --mode-policy=HardwareOnly --bind=0.0.0.0 --port=46100
if errorlevel 1 pause
