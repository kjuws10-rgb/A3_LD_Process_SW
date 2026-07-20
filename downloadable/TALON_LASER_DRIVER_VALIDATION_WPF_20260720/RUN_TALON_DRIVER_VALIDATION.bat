@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] .NET 8 SDK or Runtime is required.
  pause
  exit /b 1
)

dotnet run --project ".\Talon.Driver.Wpf\Talon.Driver.Wpf.csproj" --configuration Release
if errorlevel 1 (
  echo [ERROR] Talon driver validation UI failed.
  pause
  exit /b 1
)

endlocal
