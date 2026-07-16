@echo off
setlocal
cd /d "%~dp0"
echo Building MOF Coordinate WPF Demo...
dotnet build MofCoordinateDemo.csproj
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
echo Running MOF Coordinate WPF Demo...
dotnet run --project MofCoordinateDemo.csproj
endlocal
