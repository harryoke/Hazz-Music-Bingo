@echo off
setlocal EnableExtensions

title Hazz Music Bingo - Build Standalone EXE

echo ============================================================
echo HAZZ MUSIC BINGO - BUILD STANDALONE WINDOWS EXE
echo ============================================================
echo.

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\HazzMusicBingo\HazzMusicBingo.csproj"
set "OUT=%ROOT%STANDALONE_EXE"

if not exist "%PROJECT%" (
    echo ERROR: Project file not found:
    echo %PROJECT%
    echo.
    pause
    exit /b 1
)

set "DOTNET="

where dotnet.exe >nul 2>nul
if not errorlevel 1 (
    for /f "delims=" %%D in ('where dotnet.exe') do (
        if not defined DOTNET set "DOTNET=%%D"
    )
)

if not defined DOTNET if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
)

if not defined DOTNET if defined ProgramFiles(x86) if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
    set "DOTNET=%ProgramFiles(x86)%\dotnet\dotnet.exe"
)

if not defined DOTNET (
    echo ERROR: The .NET 8 SDK could not be found.
    echo.
    echo In Visual Studio Installer, make sure the ".NET desktop development"
    echo workload and .NET 8 SDK are installed.
    echo.
    echo You can also publish from Visual Studio using:
    echo   StandaloneWin64
    echo under Publish Profiles.
    echo.
    pause
    exit /b 1
)

echo Using:
echo   "%DOTNET%"
echo.
echo Project:
echo   "%PROJECT%"
echo.
echo Output:
echo   "%OUT%"
echo.

mkdir "%OUT%" >nul 2>nul

echo Restoring packages...
"%DOTNET%" restore "%PROJECT%"
if errorlevel 1 goto :fail

echo.
echo Publishing self-contained single-file EXE...
"%DOTNET%" publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishTrimmed=false ^
  -p:PublishReadyToRun=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "%OUT%"

if errorlevel 1 goto :fail

echo.
if exist "%OUT%\HazzMusicBingo.exe" (
    echo ============================================================
    echo SUCCESS
    echo ============================================================
    echo.
    echo Standalone EXE created:
    echo   %OUT%\HazzMusicBingo.exe
    echo.
    echo This build includes the .NET runtime and does not require
    echo .NET 8 to be installed on the computer you run it on.
    echo.
    explorer "%OUT%"
    pause
    exit /b 0
)

echo ERROR: Publish completed but HazzMusicBingo.exe was not found.
goto :fail

:fail
echo.
echo ============================================================
echo BUILD FAILED
echo ============================================================
echo.
echo Copy the error text from this window or Visual Studio and send it
echo back so the build can be corrected.
echo.
pause
exit /b 1
