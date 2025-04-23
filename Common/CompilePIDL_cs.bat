@echo off
REM PIDL 컴파일러 경로 (ProudNet 설치 경로에 맞게 수정 필요)
set PIDL_PATH=C:\Users\wayne\Documents\proud_tank\ProudNet\util\PIDL.exe

REM CS 파일 생성
%PIDL_PATH% -cs .\Tank.PIDL -outdir ..\Server
%PIDL_PATH% -cs .\Tank.PIDL -outdir ..\Client

echo PIDL compile complete!
pause 