@echo off
set filename=Build\DeepSwarmClient-Debug\netcoreapp3.0\DeepSwarmClient.exe
if exist %filename% (
    %filename% %*
) else (
    echo Exe not found.
    echo Build DeepSwarmClient as Debug first
    echo TODO: repeat but in French
    pause
)