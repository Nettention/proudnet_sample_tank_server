@echo off
REM Batch file to copy PIDL generated files to Unity project
REM Execution location: tank_server directory

echo ===== ProudNet Generated Files Unity Copy Script =====

REM Set directory paths
set SERVER_DIR=Server
set COMMON_DIR=Common
set UNITY_DIR=..\unity_tank_client\Assets\Proundnet_Demo\ProudNetScripts
set UNITY_GENERATED_DIR=%UNITY_DIR%\Generated
set UNITY_COMMON_DIR=%UNITY_DIR%\Common

REM Create Unity directories if they don't exist
if not exist "%UNITY_DIR%" (
    echo Creating Unity directory...
    mkdir "%UNITY_DIR%"
)

if not exist "%UNITY_GENERATED_DIR%" (
    echo Creating Unity Generated directory...
    mkdir "%UNITY_GENERATED_DIR%"
)

if not exist "%UNITY_COMMON_DIR%" (
    echo Creating Unity Common directory...
    mkdir "%UNITY_COMMON_DIR%"
)

REM Copy Tank_stub.cs, Tank_proxy.cs, Tank_common.cs files
echo Copying PIDL generated files...
copy /Y "%SERVER_DIR%\Tank_stub.cs" "%UNITY_GENERATED_DIR%\"
copy /Y "%SERVER_DIR%\Tank_proxy.cs" "%UNITY_GENERATED_DIR%\"
copy /Y "%SERVER_DIR%\Tank_common.cs" "%UNITY_GENERATED_DIR%\"

REM Copy Vars.cs file
echo Copying common files...
copy /Y "%COMMON_DIR%\Vars.cs" "%UNITY_COMMON_DIR%\"

echo.
echo Copy completed! The following files have been copied to Unity project:
echo - %UNITY_GENERATED_DIR%\Tank_stub.cs
echo - %UNITY_GENERATED_DIR%\Tank_proxy.cs
echo - %UNITY_GENERATED_DIR%\Tank_common.cs
echo - %UNITY_COMMON_DIR%\Vars.cs
echo.

REM Wait for confirmation
echo Press any key to continue...
pause > nul 