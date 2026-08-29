@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Hardware Store Portal - Compile Installer

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "PUBLISH_DIR=%ROOT%\setup\Build\Backend"
set "OUTPUT_DIR=%ROOT%\setup\Output"
set "ISS_FILE=%ROOT%\installer.iss"

echo.
echo ============================================================
echo         HARDWARE STORE PORTAL - COMPILE INSTALLER
echo ============================================================
echo.

if not exist "%PUBLISH_DIR%\wwwroot\index.html" (
    echo [ERROR] Published build not found at:
    echo         "%PUBLISH_DIR%"
    echo Run build.bat first.
    goto :FAILED
)

REM ---------- Find the API exe name ----------
set "API_EXE="
for /f "delims=" %%F in ('dir /b "%PUBLISH_DIR%\*.exe" 2^>nul') do (
    if not defined API_EXE set "API_EXE=%%F"
)
if not defined API_EXE (
    echo [ERROR] Could not find a published .exe under:
    echo         "%PUBLISH_DIR%"
    goto :FAILED
)

echo API EXE  : "%API_EXE%"
echo.

echo [1/2] Checking for Inno Setup 6...

set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC (
    echo [ERROR] Inno Setup 6 was not found.
    goto :FAILED
)

echo [OK] Found Inno Setup at:
echo      "%ISCC%"
echo.

echo [2/2] Compiling installer...

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%" >nul 2>&1
if exist "%OUTPUT_DIR%\HardwareStorePortal_Setup.exe" del /q "%OUTPUT_DIR%\HardwareStorePortal_Setup.exe"

"%ISCC%" "%ISS_FILE%" /DAPIEXE="%API_EXE%"
if errorlevel 1 (
    echo [ERROR] Inno Setup compilation failed.
    goto :FAILED
)

if not exist "%OUTPUT_DIR%\HardwareStorePortal_Setup.exe" (
    echo [ERROR] Installer EXE was not created.
    goto :FAILED
)

echo.
echo ============================================================
echo                    INSTALLER READY
echo ============================================================
echo.
echo "%OUTPUT_DIR%\HardwareStorePortal_Setup.exe"
echo.
pause
exit /b 0

:FAILED
echo.
echo ============================================================
echo                 INSTALLER COMPILE FAILED
echo ============================================================
echo.
pause
exit /b 1
