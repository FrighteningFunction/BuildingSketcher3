@echo off
REM Copy PaperPlugin.dll to Unity Plugins folder

set "SRC=NativePlugins\build\Debug\PaperPlugin1.dll"
set "DST=Assets\Plugins\x86_64\PaperPlugin1.dll"

echo Copying %SRC% to %DST%...
copy /Y "%SRC%" "%DST%"

if %ERRORLEVEL% NEQ 0 (
    echo Failed to copy the DLL. Make sure it was built.
    exit /b 1
)

echo Done.