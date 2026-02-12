@echo off
REM SCP Upload Script
REM Usage: ScpUpload.bat [LocalDir] [SshServer] [SshPort] [SshUser] [SshPass] [RemoteDir]

setlocal enabledelayedexpansion

set "LocalDir=%~1"
set "SshServer=%~2"
set "SshPort=%~3"
set "SshUser=%~4"
set "SshPass=%~5"
set "RemoteDir=%~6"

if "%LocalDir%"=="" (
    echo [ERROR] LocalDir is empty
    exit /b 1
)
if "%SshServer%"=="" (
    echo [ERROR] SshServer is empty
    exit /b 1
)
if "%SshPort%"=="" set "SshPort=22"
if "%SshUser%"=="" (
    echo [ERROR] SshUser is empty
    exit /b 1
)
if "%RemoteDir%"=="" set "RemoteDir=/var/www/html/hotfix"

echo ========================================
echo SCP Upload Start
echo LocalDir: %LocalDir%
echo Server: %SshUser%@%SshServer%:%SshPort%
echo RemoteDir: %RemoteDir%
echo Time: %date% %time%
echo ========================================

if not exist "%LocalDir%" (
    echo [ERROR] Local directory does not exist: %LocalDir%
    exit /b 1
)

REM Use PowerShell script for upload
powershell -ExecutionPolicy Bypass -File "%~dp0ScpUploadPowerShell.ps1" -LocalDir "%LocalDir%" -SshServer "%SshServer%" -SshPort "%SshPort%" -SshUser "%SshUser%" -SshPass "%SshPass%" -RemoteDir "%RemoteDir%"

if errorlevel 1 (
    echo [ERROR] SCP Upload failed.
    exit /b 1
)

echo ========================================
echo SCP Upload Complete
echo ========================================

exit /b 0
