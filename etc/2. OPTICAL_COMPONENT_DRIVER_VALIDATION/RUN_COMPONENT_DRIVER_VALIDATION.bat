@echo off
setlocal
pushd "%~dp0"
dotnet restore "TalonLaserDriverValidation.sln" --configfile "NuGet.config"
if errorlevel 1 goto :fail
dotnet build "TalonLaserDriverValidation.sln" -c Debug --no-restore
if errorlevel 1 goto :fail
start "A3 Component Driver Validation" "Talon.Driver.Wpf\bin\Debug\net8.0-windows\Talon.Driver.Wpf.exe"
popd
exit /b 0
:fail
echo Build failed. Review the messages above.
pause
popd
exit /b 1
