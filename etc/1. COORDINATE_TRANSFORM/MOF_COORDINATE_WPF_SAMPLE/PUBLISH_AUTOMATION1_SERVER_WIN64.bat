@echo off
setlocal
cd /d "%~dp0"

set "OUT=%~dp0ServerDeployment\win-x64"
echo Publishing self-contained Automation1Server to:
echo %OUT%

dotnet publish "Automation1Server\Automation1Server.csproj" -c Release -r win-x64 --self-contained true -o "%OUT%"
if errorlevel 1 (
  echo Publish failed.
  pause
  exit /b 1
)

copy /y "START_DEPLOYED_AUTOMATION1_SERVER.bat" "%OUT%\START_AUTOMATION1_SERVER.bat" >nul
copy /y "OPEN_SERVER_FIREWALL_PORT_46100_ADMIN.bat" "%OUT%\OPEN_SERVER_FIREWALL_PORT_46100_ADMIN.bat" >nul
copy /y "SERVER_PC_CONNECTION_GUIDE.md" "%OUT%\SERVER_PC_CONNECTION_GUIDE.md" >nul
copy /y "CLIENT_SERVER_ASCRIPT_OPERATION_PROCEDURE.md" "%OUT%\CLIENT_SERVER_ASCRIPT_OPERATION_PROCEDURE.md" >nul
copy /y "CLIENT_SERVER_ASCRIPT_OPERATION_FLOW.svg" "%OUT%\CLIENT_SERVER_ASCRIPT_OPERATION_FLOW.svg" >nul

echo.
echo Publish complete. Copy the entire folder below to Server PC 192.168.10.10:
echo %OUT%
explorer "%OUT%"
pause
