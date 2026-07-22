@echo off
setlocal

set "ROOT=%~dp0"
set "SOLUTION=Drilling.sln"
set "RUN_PROJECT=Drilling.UI\Drilling.UI.csproj"
set "FALLBACK_GIT=C:\Users\kjuws\Documents\Codex\2026-07-08\github-pr\work\PortableGit\cmd\git.exe"

pushd "%ROOT%"
if errorlevel 1 (
    echo Failed to enter project folder: %ROOT%
    exit /b 1
)

where git >nul 2>nul
if %errorlevel%==0 (
    set "GIT=git"
) else if exist "%FALLBACK_GIT%" (
    set "GIT=%FALLBACK_GIT%"
) else (
    echo Git was not found. Install Git for Windows or update FALLBACK_GIT in this file.
    popd
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo dotnet was not found. Install the .NET SDK before running this file.
    popd
    exit /b 1
)

echo [1/4] Pulling latest source...
"%GIT%" pull --ff-only origin main
if errorlevel 1 goto fail

echo [2/4] Restoring packages...
dotnet restore "%SOLUTION%"
if errorlevel 1 goto fail

echo [3/4] Building solution...
dotnet build "%SOLUTION%" -c Debug
if errorlevel 1 goto fail


popd
exit /b 0

:fail
echo.
echo Failed. Review the error output above.
popd
pause
exit /b 1
