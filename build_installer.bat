@echo off
setlocal
echo =======================================================
echo Building Pak Android Bloatware Remover Setup (.exe)
echo =======================================================
echo.

REM 1. Build C# Release
echo [1/2] Building C# .NET WPF App in Release mode...
dotnet build "%~dp0AndroidDebloater.csproj" -c Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] dotnet build failed!
    pause
    exit /b %ERRORLEVEL%
)

REM 2. Find Inno Setup Compiler
set ISCC="C:\Users\%USERNAME%\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files\Inno Setup 6\ISCC.exe"

if not exist %ISCC% (
    echo [ERROR] Inno Setup compiler (ISCC.exe) not found!
    echo Please install Inno Setup from https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

REM 3. Compile Installer
echo.
echo [2/2] Compiling Setup Installer with Inno Setup...
%ISCC% "%~dp0installer.iss"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Inno Setup compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo =======================================================
echo SUCCESS! Setup Installer created in:
echo %~dp0..\installer_output\Pak_Android_Bloatware_Remover_Setup_v1.0.0.exe
echo =======================================================
pause
