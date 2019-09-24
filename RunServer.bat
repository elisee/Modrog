@echo off
set filename=Build\DeepSwarmServer-Debug\netcoreapp3.0\DeepSwarmServer.exe
if exist %filename% (
    %filename% %*
) else (
    echo Exe not found.
    echo Build DeepSwarmServer as Debug first
    echo TODO: repeat but in French
    pause
)