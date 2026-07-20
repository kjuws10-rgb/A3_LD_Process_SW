@echo off
setlocal
pushd "%~dp0"
dotnet restore "TalonLaserDriverValidation.sln" --configfile "NuGet.config"
if errorlevel 1 goto :fail
dotnet run --project "Equipment.Driver.Verification\Equipment.Driver.Verification.csproj" -c Debug --no-restore
if errorlevel 1 goto :fail
echo All component driver tests passed.
pause
popd
exit /b 0
:fail
echo Verification failed.
pause
popd
exit /b 1
