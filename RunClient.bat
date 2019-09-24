@echo off
set filename=Build\DeepSwarmClient-Debug\netcoreapp3.0\DeepSwarmClient.exe
if exist %filename% (
    %filename% %*
) else (
    echo Executable not found. Build DeepSwarmClient as Debug first
    pause
)