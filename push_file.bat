@echo off
setlocal

:: ==========================================
:: CONFIGURATION
:: ==========================================
set "TARGET_DIR=C:\Users\jwkang01\Downloads\A3_LD_Process_SW"
set "GIT_URL=https://github.com/kjuws10-rgb/A3_LD_Process_SW.git"
set "BRANCH_NAME=main"
set "ALT_GIT=C:\Users\kjuws\Documents\Codex\2026-07-08\github-pr\work\PortableGit\cmd\git.exe"

echo ==========================================
echo Starting Git Auto Push...
echo Target: %TARGET_DIR%
echo ==========================================

:: 1. Change Directory
pushd "%TARGET_DIR%"
if errorlevel 1 (
    echo [ERROR] Target directory not found: %TARGET_DIR%
    pause
    exit /b 1
)

:: 2. Find Git Executable
where git >nul 2>nul
if %errorlevel%==0 (
    set "GIT_CMD=git"
) else if exist "%ALT_GIT%" (
    set "GIT_CMD=%ALT_GIT%"
) else (
    echo [ERROR] Git was not found.
    popd
    pause
    exit /b 1
)

:: 3. Initialize or Setup Remote
if not exist ".git" (
    echo [INFO] Initializing git repository...
    "%GIT_CMD%" init
    "%GIT_CMD%" remote add origin %GIT_URL%
    "%GIT_CMD%" branch -M %BRANCH_NAME%
) else (
    "%GIT_CMD%" remote set-url origin %GIT_URL% >nul 2>&1
    if errorlevel 1 (
        "%GIT_CMD%" remote add origin %GIT_URL%
    )
)

:: 4. Pull Latest Changes
echo [1/3] Pulling latest changes from remote...
"%GIT_CMD%" pull origin %BRANCH_NAME% --allow-unrelated-histories

:: 5. Stage and Commit
echo [2/3] Staging and committing changes...
"%GIT_CMD%" add .

set "CDATE=%date%"
set "CTIME=%time%"
set "MSG=Auto commit: %CDATE% %CTIME%"

"%GIT_CMD%" diff --cached --quiet
if errorlevel 1 (
    "%GIT_CMD%" commit -m "%MSG%"
    
    :: 6. Push to Remote
    echo [3/3] Pushing to remote...
    "%GIT_CMD%" push -u origin %BRANCH_NAME%
    if errorlevel 1 goto fail
) else (
    echo [INFO] No changes to push.
)

echo ==========================================
echo Process completed successfully.
echo ==========================================
popd
pause
exit /b 0

:fail
echo.
echo ==========================================
echo [ERROR] Push failed. Please check the log above.
echo ==========================================
popd
pause
exit /b 1
