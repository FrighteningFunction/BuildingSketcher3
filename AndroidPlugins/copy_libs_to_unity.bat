@echo off
rem ------------------------------------------------------------
rem  Copy libPaperPlugin.so (debug build) into the Unity project
rem ------------------------------------------------------------
rem  Adjust these two lines if your folder layout changes
set UNITY_ROOT=..
set SRC_BASE=app\build\intermediates\merged_native_libs\debug\mergeDebugNativeLibs\out\lib

for %%A in (arm64-v8a armeabi-v7a) do (
    if not exist "%SRC_BASE%\%%A\libPaperPlugin.so" (
        echo Source file not found: %SRC_BASE%\%%A\libPaperPlugin.so
    ) else (
        if not exist "%UNITY_ROOT%\Assets\Plugins\Android\libs\%%A" (
            mkdir "%UNITY_ROOT%\Assets\Plugins\Android\libs\%%A"
        )
        copy /y "%SRC_BASE%\%%A\libPaperPlugin.so" ^
                "%UNITY_ROOT%\Assets\Plugins\Android\libs\%%A\" >nul
        echo Copied %%A\libPaperPlugin.so
    )
)