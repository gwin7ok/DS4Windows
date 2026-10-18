@echo off
rem Remove DS4Windows\bin and DS4Windows\obj for a clean build.
rem 2026-10-19: made safer against half-deleted obj folders (WPF *_wpftmp builds then fail with
rem "InitializeComponent does not exist"):
rem   1. stop the MSBuild / compiler servers that may hold files open
rem   2. delete leftover *_wpftmp.csproj files in the project folder
rem   3. verify the folders are really gone; if not, report it and return an error code
setlocal
pushd "%~dp0\.."

echo Stopping build servers...
dotnet build-server shutdown >nul 2>&1

del /q "DS4Windows\*_wpftmp.csproj" >nul 2>&1

if exist "DS4Windows\bin" rd /s /q "DS4Windows\bin"
if exist "DS4Windows\obj" rd /s /q "DS4Windows\obj"

set REMOVE_BIN_RESULT=0
if exist "DS4Windows\bin" (
    echo ERROR: DS4Windows\bin could not be removed completely. A process may still be using files in it.
    set REMOVE_BIN_RESULT=1
)
if exist "DS4Windows\obj" (
    echo ERROR: DS4Windows\obj could not be removed completely. A process may still be using files in it.
    set REMOVE_BIN_RESULT=1
)
if "%REMOVE_BIN_RESULT%"=="0" echo bin and obj removed.

popd
endlocal & exit /b %REMOVE_BIN_RESULT%
