@echo off
set filename=Build\ModrogServer-Debug\netcoreapp3.0\ModrogServer.exe
if exist %filename% (
    %filename% %*
) else (
    echo Executable not found. Build ModrogServer as Debug first
    pause
)
