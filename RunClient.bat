@echo off
set filename=Build\ModrogClient-Debug\netcoreapp3.0\ModrogClient.exe
if exist %filename% (
    %filename% %*
) else (
    echo Executable not found. Build ModrogClient as Debug first
    pause
)
