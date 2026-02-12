@echo off
REM Auto Increment ResourceVersion Script
REM Usage: AutoIncrementResourceVersion.bat [ConfigFile]

setlocal enabledelayedexpansion

set "ConfigFile=%~1"
if "%ConfigFile%"=="" set "ConfigFile=%ProjectRoot%\Tools\Jenkins\BuildResourceConfig.json"

echo ========================================
echo Auto Increment ResourceVersion
echo ConfigFile: %ConfigFile%
echo ========================================

if not exist "%ConfigFile%" (
    echo [INFO] Config file not found, using default version 1.
    set "CURRENT_VERSION=0"
    goto :increment
)

REM Read ResourceVersion from JSON using PowerShell
for /f "usebackq delims=" %%i in (`powershell -Command "(Get-Content '%ConfigFile%' | ConvertFrom-Json).ResourceVersion"`) do set "CURRENT_VERSION=%%i"

if "%CURRENT_VERSION%"=="" set "CURRENT_VERSION=0"

:increment
set /a NEW_VERSION=%CURRENT_VERSION%+1

echo Current Version: %CURRENT_VERSION%
echo New Version: %NEW_VERSION%

REM Export to file for next steps
echo %NEW_VERSION%>"%~dp0ResourceVersion.txt"

echo ========================================
echo ResourceVersion Updated to: %NEW_VERSION%
echo ========================================

endlocal & set "ResourceVersion=%NEW_VERSION%"
exit /b 0
