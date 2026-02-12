@echo off
REM SVN Update Script
REM Usage: SvnUpdate.bat [ProjectRoot]

setlocal enabledelayedexpansion

set "ProjectRoot=%~1"
if "%ProjectRoot%"=="" (
    echo [ERROR] ProjectRoot is not specified.
    exit /b 1
)

echo ========================================
echo SVN Update Start
echo ProjectRoot: %ProjectRoot%
echo Time: %date% %time%
echo ========================================

cd /d "%ProjectRoot%"
if errorlevel 1 (
    echo [ERROR] Cannot change directory to: %ProjectRoot%
    exit /b 1
)

REM Cleanup locks
echo [Step 1] SVN Cleanup...
svn cleanup
if errorlevel 1 (
    echo [WARNING] SVN cleanup failed, continuing...
)

REM Update code
echo [Step 2] SVN Update...
svn update --accept theirs-full
if errorlevel 1 (
    echo [ERROR] SVN Update failed.
    exit /b 1
)

REM Get revision info
echo [Step 3] Get Revision info...
for /f "tokens=2" %%i in ('svn info ^| findstr "Revision:"') do set SVN_REVISION=%%i
echo Current Revision: %SVN_REVISION%

echo ========================================
echo SVN Update Complete
echo ========================================

exit /b 0
