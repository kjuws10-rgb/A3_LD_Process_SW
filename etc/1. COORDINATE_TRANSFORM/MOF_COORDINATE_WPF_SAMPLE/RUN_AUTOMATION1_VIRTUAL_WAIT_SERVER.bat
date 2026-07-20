@echo off
setlocal
cd /d "%~dp0"
if "%A1_SCRIPT_API_KEY%"=="" (
  echo Set A1_SCRIPT_API_KEY before starting the Automation1 Virtual Wait server.
  exit /b 1
)
echo This mode connects to an Automation1 Virtual controller and executes the generated Wait script.
echo Laser, PSO, Hardware Aux and Galvo calibration commands must remain disabled.
dotnet run --project "Automation1Server\Automation1Server.csproj" -- --runtime=automation1 --mode-policy=VirtualOnly --bind=0.0.0.0 --port=46100 --api-key=%A1_SCRIPT_API_KEY%
if errorlevel 1 pause
