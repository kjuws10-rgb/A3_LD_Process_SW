@echo off
setlocal
cd /d "%~dp0"
dotnet run --project ".\Talon.Driver.Verification\Talon.Driver.Verification.csproj" --configuration Release
set EXIT_CODE=%ERRORLEVEL%
echo.
if not "%EXIT_CODE%"=="0" echo [FAILED] Driver verification failed.
if "%EXIT_CODE%"=="0" echo [PASSED] All driver verification cases passed.
pause
exit /b %EXIT_CODE%
