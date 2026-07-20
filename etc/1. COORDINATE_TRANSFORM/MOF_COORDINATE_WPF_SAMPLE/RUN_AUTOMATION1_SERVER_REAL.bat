@echo off
setlocal
cd /d "%~dp0"
if "%A1_SCRIPT_API_KEY%"=="" (
  echo Set A1_SCRIPT_API_KEY before starting the real server.
  exit /b 1
)
dotnet run --project "Automation1Server\Automation1Server.csproj" -- --runtime=automation1 --bind=0.0.0.0 --port=46100 --api-key=%A1_SCRIPT_API_KEY%
if errorlevel 1 pause

