@echo off
echo [INFO] Starting TankGameServer C++ Server...

REM Check possible executable file locations
set SERVER_EXE=

REM 1. NMake Makefiles build (created in root folder)
if exist "%~dp0build\TankServer.exe" (
    set SERVER_EXE=%~dp0build\TankServer.exe
    cd %~dp0build
    echo [INFO] Found executable at build\TankServer.exe (NMake build)
    goto RUN_SERVER
)

REM 2. Visual Studio Debug build (created in Debug folder)
if exist "%~dp0build\Debug\TankServer.exe" (
    set SERVER_EXE=%~dp0build\Debug\TankServer.exe
    cd %~dp0build\Debug
    echo [INFO] Found executable at build\Debug\TankServer.exe (VS Debug build)
    goto RUN_SERVER
)

REM 3. Visual Studio Release build (created in Release folder)
if exist "%~dp0build\Release\TankServer.exe" (
    set SERVER_EXE=%~dp0build\Release\TankServer.exe
    cd %~dp0build\Release
    echo [INFO] Found executable at build\Release\TankServer.exe (VS Release build)
    goto RUN_SERVER
)



REM Executable file not found
echo [ERROR] Server executable not found! Please build the server first.
echo [ERROR] Expected locations:
echo [ERROR]   - %~dp0build\TankServer.exe (NMake build)
echo [ERROR]   - %~dp0build\Debug\TankServer.exe (VS Debug build)
echo [ERROR]   - %~dp0build\Release\TankServer.exe (VS Release build)
exit /b 1

:RUN_SERVER
REM Run the server
echo [INFO] Server is starting. Press 'q' to quit.
echo [INFO] Running: %SERVER_EXE%
"%SERVER_EXE%"

pause 