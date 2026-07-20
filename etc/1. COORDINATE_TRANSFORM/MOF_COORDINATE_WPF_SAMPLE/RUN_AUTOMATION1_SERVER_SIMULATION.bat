@echo off
setlocal
cd /d "%~dp0"
dotnet run --project "Automation1Server\Automation1Server.csproj" -- --runtime=simulation --bind=0.0.0.0 --port=46100 --api-key=change-this-key
if errorlevel 1 pause

