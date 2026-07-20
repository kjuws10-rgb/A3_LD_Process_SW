@echo off
setlocal
cd /d "%~dp0"
echo Protocol simulation only: AeroScript syntax and Automation1 Wait behavior are not executed.
dotnet run --project "Automation1Server\Automation1Server.csproj" -- --runtime=simulation --mode-policy=Any --bind=0.0.0.0 --port=46100 --api-key=change-this-key
if errorlevel 1 pause
