@echo off
setlocal enabledelayedexpansion

echo [INFO] =========================================
echo [INFO] Starting TankGameServer C++ Build...
echo [INFO] =========================================

REM Build type setting
REM set BUILD_TYPE=Debug
REM Uncomment the line below to build Release
set BUILD_TYPE=Release

REM ProudNet path setting
set PROUDNET_PATH=C:\Users\wayne\Documents\proud_tank\ProudNet

REM Automatic Visual Studio version detection
echo [INFO] Detecting Visual Studio installation...
set VS_FOUND=0
set VS_VERSION=
set VS_GENERATOR=

REM Check VS2022 first - Use simple NMake
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    echo [INFO] Visual Studio 2022 Community found
    set VS_FOUND=1
    set VS_VERSION=2022
    
    REM Set to NMake generator
    set VS_GENERATOR=NMake Makefiles
    
    REM VS developer environment setup - Execute first and continue with the rest
    echo [INFO] Setting up VS2022 environment...
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" (
    echo [INFO] Visual Studio 2022 Professional found
    set VS_FOUND=1
    set VS_VERSION=2022
    
    REM Set to NMake generator
    set VS_GENERATOR=NMake Makefiles
    
    REM VS developer environment setup
    echo [INFO] Setting up VS2022 environment...
    call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" (
    echo [INFO] Visual Studio 2022 Enterprise found
    set VS_FOUND=1
    set VS_VERSION=2022
    
    REM Set to NMake generator
    set VS_GENERATOR=NMake Makefiles
    
    REM VS developer environment setup
    echo [INFO] Setting up VS2022 environment...
    call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" x64
)

REM Check VS2019 (if VS2022 is not found)
if %VS_FOUND% EQU 0 (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" (
        echo [INFO] Visual Studio 2019 Community found
        set VS_FOUND=1
        set VS_VERSION=2019
        
        REM Set to NMake generator
        set VS_GENERATOR=NMake Makefiles
        
        REM VS developer environment setup
        echo [INFO] Setting up VS2019 environment...
        call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvarsall.bat" (
        echo [INFO] Visual Studio 2019 Professional found
        set VS_FOUND=1
        set VS_VERSION=2019
        
        REM Set to NMake generator
        set VS_GENERATOR=NMake Makefiles
        
        REM VS developer environment setup
        echo [INFO] Setting up VS2019 environment...
        call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" (
        echo [INFO] Visual Studio 2019 Enterprise found
        set VS_FOUND=1
        set VS_VERSION=2019
        
        REM Set to NMake generator
        set VS_GENERATOR=NMake Makefiles
        
        REM VS developer environment setup
        echo [INFO] Setting up VS2019 environment...
        call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" x64
    )
)

REM Check Visual Studio 2017
if !VS_FOUND! EQU 0 (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.exe" (
        set VS_FOUND=1
        set VS_VERSION=2017
        set VS_GENERATOR=Visual Studio 15 2017 Win64
        set VS_ARCH=
        echo [INFO] Visual Studio 2017 Community found
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe" (
        set VS_FOUND=1
        set VS_VERSION=2017
        set VS_GENERATOR=Visual Studio 15 2017 Win64
        set VS_ARCH=
        echo [INFO] Visual Studio 2017 Professional found
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe" (
        set VS_FOUND=1
        set VS_VERSION=2017
        set VS_GENERATOR=Visual Studio 15 2017 Win64
        set VS_ARCH=
        echo [INFO] Visual Studio 2017 Enterprise found
    )
)

if !VS_FOUND! EQU 0 (
    echo [WARNING] No Visual Studio installation found. Using default CMake generator.
    set VS_GENERATOR=
) else (
    echo [INFO] Using Visual Studio %VS_VERSION% with generator: %VS_GENERATOR%
)

REM Check ProudNet path
if not exist "%PROUDNET_PATH%" (
    echo [ERROR] ProudNet path not found: %PROUDNET_PATH%
    echo [ERROR] Please set the correct PROUDNET_PATH variable in build.bat
    exit /b 1
)

REM If Visual Studio was not found
if %VS_FOUND% EQU 0 (
    echo [ERROR] No compatible Visual Studio installation found.
    echo [ERROR] Please install Visual Studio 2019 or 2022 with C++ development components.
    exit /b 1
)

echo [INFO] ProudNet Path: %PROUDNET_PATH%
echo [INFO] Build Type: %BUILD_TYPE%
echo [INFO] Using: Visual Studio %VS_VERSION% with %VS_GENERATOR%

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
if %ERRORLEVEL% NEQ 0 (
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
if exist build (
    echo [INFO] Removing existing build directory...
    rmdir /s /q build
)
mkdir build
cd build

REM Configure CMake with VS generator if found
echo [INFO] Configuring CMake...
if defined VS_GENERATOR (
    if "%VS_GENERATOR%" == "NMake Makefiles" (
        echo [INFO] Using NMake Makefiles generator
        cmake -G "%VS_GENERATOR%" -DCMAKE_BUILD_TYPE=%BUILD_TYPE% -DPROUDNET_PATH=%PROUDNET_PATH% -DVS_VERSION=%VS_VERSION% ..
    ) else (
        echo [INFO] Using generator: %VS_GENERATOR%
        cmake -G "%VS_GENERATOR%" -DCMAKE_BUILD_TYPE=%BUILD_TYPE% -DPROUDNET_PATH=%PROUDNET_PATH% -DVS_VERSION=%VS_VERSION% ..
    )
) else (
    cmake -DCMAKE_BUILD_TYPE=%BUILD_TYPE% -DPROUDNET_PATH=%PROUDNET_PATH% ..
)

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] CMake configuration failed!
    exit /b 1
)

REM Build project
echo [INFO] Building project...
if "%VS_GENERATOR%" == "NMake Makefiles" (
    cmake --build .
) else (
    cmake --build . --config %BUILD_TYPE% --verbose
)

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed!
    exit /b 1
)

echo [INFO] Build successful!
echo [INFO] Executable: %~dp0build\%BUILD_TYPE%\TankGameServer.exe

REM Copy DLL files
echo [INFO] Copying required DLL files...
if "%VS_GENERATOR%" == "NMake Makefiles" (
    REM NMake outputs directly to root folder
    set DLL_DEST_DIR=%~dp0build
) else (
    REM Visual Studio outputs to Debug/Release subfolder
    set DLL_DEST_DIR=%~dp0build\%BUILD_TYPE%
)

if not exist "%DLL_DEST_DIR%" mkdir "%DLL_DEST_DIR%"

copy "%DLL_PATH%\ProudNetServer.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\ProudNetClient.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\libcrypto-3-x64.dll" "%DLL_DEST_DIR%\" > nul
copy "%DLL_PATH%\libssl-3-x64.dll" "%DLL_DEST_DIR%\" > nul

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to copy DLL files!
    exit /b 1
)

echo [INFO] =========================================
echo [INFO] Build completed!
echo [INFO] Executable: %DLL_DEST_DIR%\TankServer.exe
echo [INFO] =========================================

pause 