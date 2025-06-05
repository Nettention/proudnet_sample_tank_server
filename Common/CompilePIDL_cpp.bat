@echo off
REM ProudNet PIDL compiler path (Update according to your ProudNet installation path)
set PIDL_PATH=..\..\ProudNet\util\PIDL.exe
set INCLUDE_PATH=..\..\ProudNet\include

REM Delete previously generated files
del ..\Server_CPP\include\Tank_common.*
del ..\Server_CPP\include\Tank_proxy.*
del ..\Server_CPP\include\Tank_stub.*

REM Generate C++ files with more specific options
echo [INFO] Generating C++ files from Tank.PIDL...
%PIDL_PATH% -cpp .\Tank.PIDL -outdir ..\Server_CPP\include -outname Tank -include_path "%INCLUDE_PATH%"
%PIDL_PATH% -cpp_proxy .\Tank.PIDL -outdir ..\Server_CPP\include -outname Tank -include_path "%INCLUDE_PATH%"
%PIDL_PATH% -cpp_stub .\Tank.PIDL -outdir ..\Server_CPP\include -outname Tank -include_path "%INCLUDE_PATH%"

echo [INFO] PIDL C++ compilation complete!
echo [INFO] Generated files in ..\Server_CPP\include directory.

pause 