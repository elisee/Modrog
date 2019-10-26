@echo off
set filename=Build\ModrogEditor-Debug\netcoreapp3.0\ModrogEditor.exe
if exist %filename% (
    %filename% %*
) else (
    echo Executable not found. Build ModrogEditor as Debug first
    pause
)
