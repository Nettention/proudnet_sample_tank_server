@echo off
echo [INFO] Starting TankGameServer C++ Server...

REM Move to the execution directory
cd %~dp0build\Debug

REM Check if the server executable exists
if not exist TankGameServer.exe (
    echo [ERROR] TankGameServer.exe not found! Please build the server first.
    exit /b 1
)

REM Run the server
echo [INFO] Server is starting. Press 'q' to quit.
TankGameServer.exe

pause 