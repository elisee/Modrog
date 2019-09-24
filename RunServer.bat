@echo off
set filename=Build\DeepSwarmServer-Debug\netcoreapp3.0\DeepSwarmServer.exe
if exist %filename% (
    %filename% %*
) else (
    echo Executable not found. Build DeepSwarmServer as Debug first
    pause
)