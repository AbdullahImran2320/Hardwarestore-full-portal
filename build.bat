@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Hardware Store Portal - Build

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "FRONTEND=%ROOT%\Frontend\hardware-store-frontend"
set "BACKEND_DIR=%ROOT%\Backend"

echo.
echo ============================================================
echo              HARDWARE STORE PORTAL - BUILD
echo ============================================================
echo.

REM ---------- Find Angular project ----------
if not exist "%FRONTEND%\package.json" (
    echo [ERROR] Angular project not found at:
    echo         "%FRONTEND%"
    goto :FAILED
)
if not exist "%FRONTEND%\angular.json" (
    echo [ERROR] angular.json not found at:
    echo         "%FRONTEND%"
    goto :FAILED
)

REM ---------- Find .NET project ----------
set "CSPROJ="
for /f "delims=" %%F in ('dir /b /s /a-d "%BACKEND_DIR%\*.csproj" 2^>nul') do (
    if not defined CSPROJ set "CSPROJ=%%F"
)

if not defined CSPROJ (
    echo [ERROR] ASP.NET Core .csproj not found under:
    echo         "%BACKEND_DIR%"
    goto :FAILED
)

for %%F in ("%CSPROJ%") do set "API_EXE=%%~nF.exe"

set "BUILD_DIR=%ROOT%\setup\Build"
set "PUBLISH_DIR=%BUILD_DIR%\Backend"

echo Root     : "%ROOT%"
echo Frontend : "%FRONTEND%"
echo Project  : "%CSPROJ%"
echo API EXE  : "%API_EXE%"
echo.

echo [1/5] Checking required software...

where node >nul 2>&1 || (echo [ERROR] Node.js not found.&goto :FAILED)
where npm >nul 2>&1 || (echo [ERROR] npm not found.&goto :FAILED)
where dotnet >nul 2>&1 || (echo [ERROR] .NET SDK not found.&goto :FAILED)

echo [OK] Required tools found.
echo.

echo [2/5] Building Angular frontend...
pushd "%FRONTEND%" || goto :FAILED

if exist package-lock.json (
    call npm ci
) else (
    call npm install
)

if errorlevel 1 (
    popd
    echo [ERROR] npm dependency installation failed.
    goto :FAILED
)

call npm run build
if errorlevel 1 (
    popd
    echo [ERROR] Angular build failed.
    goto :FAILED
)
popd

echo [OK] Angular build completed.
echo.

echo [3/5] Locating Angular browser output...

set "ANGULAR_WEBROOT="
if exist "%FRONTEND%\dist\index.html" set "ANGULAR_WEBROOT=%FRONTEND%\dist"

if not defined ANGULAR_WEBROOT if exist "%FRONTEND%\dist" (
    for /f "delims=" %%F in ('dir /b /s /a-d "%FRONTEND%\dist\index.html" 2^>nul') do (
        if not defined ANGULAR_WEBROOT set "ANGULAR_WEBROOT=%%~dpF"
    )
)

if not defined ANGULAR_WEBROOT (
    echo [ERROR] Angular index.html was not found.
    goto :FAILED
)

if "!ANGULAR_WEBROOT:~-1!"=="\" set "ANGULAR_WEBROOT=!ANGULAR_WEBROOT:~0,-1!"

echo [OK] Browser output:
echo      "%ANGULAR_WEBROOT%"
echo.

echo [4/5] Publishing ASP.NET Core backend...

if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%PUBLISH_DIR%" >nul 2>&1

REM Important: do NOT use --no-restore here.
REM The runtime-specific restore for win-x64 is required.
call dotnet publish "%CSPROJ%" -c Release -r win-x64 --self-contained true -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo [ERROR] dotnet publish failed.
    goto :FAILED
)

echo [OK] Backend published.
echo.

echo [5/5] Combining frontend and backend...

if exist "%PUBLISH_DIR%\wwwroot" rmdir /s /q "%PUBLISH_DIR%\wwwroot"
mkdir "%PUBLISH_DIR%\wwwroot" >nul 2>&1

xcopy "%ANGULAR_WEBROOT%\*" "%PUBLISH_DIR%\wwwroot\" /E /I /Y /Q >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy Angular files into wwwroot.
    goto :FAILED
)

if not exist "%PUBLISH_DIR%\wwwroot\index.html" (
    echo [ERROR] wwwroot\index.html is missing.
    goto :FAILED
)

if not exist "%ROOT%\LaunchHardwareStorePortal.ps1" (
    echo [ERROR] LaunchHardwareStorePortal.ps1 is missing from the repo root.
    goto :FAILED
)

copy /Y "%ROOT%\LaunchHardwareStorePortal.ps1" "%PUBLISH_DIR%\LaunchHardwareStorePortal.ps1" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy launcher.
    goto :FAILED
)

echo [OK] Frontend copied into wwwroot.
echo [OK] Launcher copied.
echo.

echo ============================================================
echo                    BUILD SUCCESSFUL
echo ============================================================
echo.
echo Published app ready at:
echo "%PUBLISH_DIR%"
echo.
echo Next step: run compile-installer.bat to create the installer.
echo.
pause
exit /b 0

:FAILED
echo.
echo ============================================================
echo                      BUILD FAILED
echo ============================================================
echo.
pause
exit /b 1
