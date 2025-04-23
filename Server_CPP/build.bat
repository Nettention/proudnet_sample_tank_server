@echo off
setlocal enabledelayedexpansion

echo [INFO] =========================================
echo [INFO] Starting TankGameServer C++ Build...
echo [INFO] =========================================

REM Build type setting
set BUILD_TYPE=Debug
REM Uncomment the line below to build Release
REM set BUILD_TYPE=Release

REM ProudNet path setting
set PROUDNET_PATH=C:\Users\wayne\Documents\proud_tank\ProudNet

REM Check ProudNet path
if not exist "%PROUDNET_PATH%" (
    echo [ERROR] ProudNet path not found: %PROUDNET_PATH%
    echo [ERROR] Please set the correct PROUDNET_PATH variable in build.bat
    exit /b 1
)

echo [INFO] ProudNet Path: %PROUDNET_PATH%
echo [INFO] Build Type: %BUILD_TYPE%

REM Check if libraries and DLLs exist
set LIB_PATH=%PROUDNET_PATH%\lib\x64\v140\%BUILD_TYPE%
set DLL_PATH=%PROUDNET_PATH%\lib\x64\dll\Release

if not exist "%LIB_PATH%\ProudNetServer.lib" (
    echo [ERROR] ProudNetServer.lib not found: %LIB_PATH%\ProudNetServer.lib
    echo [ERROR] Please check if ProudNet is properly installed.
    exit /b 1
)

if not exist "%LIB_PATH%\ProudNetClient.lib" (
    echo [ERROR] ProudNetClient.lib not found: %LIB_PATH%\ProudNetClient.lib
    echo [ERROR] Please check if ProudNet is properly installed.
    exit /b 1
)

if not exist "%DLL_PATH%\ProudNetServer.dll" (
    echo [ERROR] ProudNetServer.dll not found: %DLL_PATH%\ProudNetServer.dll
    echo [ERROR] Please check if ProudNet is properly installed.
    exit /b 1
)

REM Check and generate PIDL files
echo [INFO] Checking and generating PIDL files...
if not exist "%~dp0..\Common\CompilePIDL_CPP.bat" (
    echo [ERROR] CompilePIDL_CPP.bat file not found!
    exit /b 1
)

if not exist "%~dp0..\Common\Tank.PIDL" (
    echo [ERROR] Tank.PIDL file not found!
    exit /b 1
)

REM Check PIDL compiler exists
if not exist "%PROUDNET_PATH%\util\PIDL.exe" (
    echo [ERROR] PIDL.exe file not found: %PROUDNET_PATH%\util\PIDL.exe
    echo [ERROR] Please check if ProudNet is properly installed.
    exit /b 1
)

REM Run PIDL compilation
cd %~dp0..\Common
echo [INFO] Compiling PIDL files...
call CompilePIDL_CPP.bat
if !ERRORLEVEL! NEQ 0 (
    echo [ERROR] Failed to generate PIDL files!
    exit /b 1
)

REM Check generated files
cd %~dp0
if not exist "include\Tank_stub.h" (
    echo [ERROR] Tank_stub.h file was not generated after PIDL compilation!
    echo [ERROR] Please check the PIDL compiler output.
    exit /b 1
)

REM Create build directory
echo [INFO] Creating build directory...
if not exist build mkdir build
cd build

REM Configure CMake
echo [INFO] Configuring CMake...
cmake -DCMAKE_BUILD_TYPE=%BUILD_TYPE% -DPROUDNET_PATH=%PROUDNET_PATH% ..

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] CMake configuration failed!
    exit /b 1
)

REM Build project
echo [INFO] Building project...
cmake --build . --config %BUILD_TYPE% --verbose

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed!
    exit /b 1
)

echo [INFO] Build successful!
echo [INFO] Executable: %~dp0build\%BUILD_TYPE%\TankGameServer.exe

REM Copy DLL files
echo [INFO] Copying required DLL files...
set DLL_DEST_DIR=%~dp0build\%BUILD_TYPE%

if not exist "%DLL_DEST_DIR%" mkdir "%DLL_DEST_DIR%"

copy "%DLL_PATH%\ProudNetServer.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\ProudNetClient.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\libcrypto-1_1-x64.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\libssl-1_1-x64.dll" "%DLL_DEST_DIR%\" > nul

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to copy DLL files!
    exit /b 1
)

echo [INFO] =========================================
echo [INFO] Build completed!
echo [INFO] Executable: %DLL_DEST_DIR%\TankGameServer.exe
echo [INFO] =========================================

pause 