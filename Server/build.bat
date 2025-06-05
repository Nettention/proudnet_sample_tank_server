@echo off
echo ========================================
echo Tank Server Build Script
echo ========================================

REM Set build configuration
set Configuration=Debug

echo Build configuration: %Configuration%

REM Build the project
echo Building Tank Server...
dotnet build --configuration %Configuration%

REM Check if build was successful
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo Build completed successfully!

REM Define paths
set PROUDNET_DOTNET_PATH=..\..\ProudNet\lib\DotNet
set PROUDNET_X64_PATH=..\..\ProudNet\lib\DotNet\x64
set BUILD_OUTPUT_PATH=bin\%Configuration%\net9.0

echo Using build output path: %BUILD_OUTPUT_PATH%

REM Check if build output directory exists
if not exist "%BUILD_OUTPUT_PATH%" (
    echo Error: Build output directory not found at %BUILD_OUTPUT_PATH%
    pause
    exit /b 1
)

echo Build output directory exists: %BUILD_OUTPUT_PATH%

REM Check if ProudNet directories exist
if not exist "%PROUDNET_DOTNET_PATH%" (
    echo Error: ProudNet DotNet directory not found at %PROUDNET_DOTNET_PATH%
    pause
    exit /b 1
)

echo ProudNet DotNet directory exists: %PROUDNET_DOTNET_PATH%

if not exist "%PROUDNET_X64_PATH%" (
    echo Error: ProudNet x64 directory not found at %PROUDNET_X64_PATH%
    pause
    exit /b 1
)

echo ProudNet x64 directory exists: %PROUDNET_X64_PATH%

echo ========================================
echo Copying ProudNet DLLs...
echo ========================================

REM Copy ProudNet DLLs to build output directory
echo Copying libcrypto-3-x64.dll...
copy "%PROUDNET_X64_PATH%\libcrypto-3-x64.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy libcrypto-3-x64.dll

echo Copying libssl-3-x64.dll...
copy "%PROUDNET_X64_PATH%\libssl-3-x64.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy libssl-3-x64.dll

echo Copying ProudDotNetClient.dll...
copy "%PROUDNET_DOTNET_PATH%\ProudDotNetClient.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudDotNetClient.dll

echo Copying ProudDotNetClient.pdb...
copy "%PROUDNET_DOTNET_PATH%\ProudDotNetClient.pdb" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudDotNetClient.pdb

echo Copying ProudDotNetServer.dll...
copy "%PROUDNET_DOTNET_PATH%\ProudDotNetServer.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudDotNetServer.dll

echo Copying ProudDotNetServer.pdb...
copy "%PROUDNET_DOTNET_PATH%\ProudDotNetServer.pdb" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudDotNetServer.pdb

echo Copying ProudNetClient.dll...
copy "%PROUDNET_X64_PATH%\ProudNetClient.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudNetClient.dll

echo Copying ProudNetClientPlugin.dll...
copy "%PROUDNET_X64_PATH%\ProudNetClientPlugin.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudNetClientPlugin.dll

echo Copying ProudNetServer.dll...
copy "%PROUDNET_X64_PATH%\ProudNetServer.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudNetServer.dll

echo Copying ProudNetServerPlugin.dll...
copy "%PROUDNET_X64_PATH%\ProudNetServerPlugin.dll" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudNetServerPlugin.dll

echo Copying ProudNetServerPlugin.pdb...
copy "%PROUDNET_X64_PATH%\ProudNetServerPlugin.pdb" "%BUILD_OUTPUT_PATH%\"
if %ERRORLEVEL% neq 0 echo Warning: Failed to copy ProudNetServerPlugin.pdb

echo ========================================
echo Build and DLL copy completed!
echo ========================================
echo.
echo Output directory: %BUILD_OUTPUT_PATH%
echo You can now run the Tank Server executable.
echo.
pause 