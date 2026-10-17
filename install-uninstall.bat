@echo off
setlocal

set "PROFILE=C:\Users\jerem\AppData\Roaming\com.kesomannen.gale\valheim\profiles\ValPlusKevin"
set "PLUGIN_DIR=%PROFILE%\BepInEx\plugins\BossResetOnDeath"
set "CONFIG_FILE=%PROFILE%\BepInEx\config\com.jerem.valheim.bossresetondeath.cfg"
set "PROJECT=%~dp0BossResetOnDeath.csproj"
set "BUILD_DLL=%~dp0bin\Release\netstandard2.1\BossResetOnDeath.dll"

if /i "%~1"=="install" goto install
if /i "%~1"=="uninstall" goto uninstall

 echo Boss Reset On Death
 echo.
 echo 1. Build and install
 echo 2. Uninstall and restore vanilla boss behavior
 echo 3. Cancel
 echo.
 choice /c 123 /n /m "Choose an option: "
 if errorlevel 3 exit /b 0
 if errorlevel 2 goto uninstall
 goto install

:install
if not exist "%PROFILE%\BepInEx\plugins" (
    echo ERROR: The ValPlusKevin BepInEx profile was not found.
    exit /b 1
)

echo Building Boss Reset On Death...
dotnet build "%PROJECT%" -c Release
if errorlevel 1 (
    echo ERROR: Build failed; nothing was installed.
    exit /b 1
)

if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"
copy /y "%BUILD_DLL%" "%PLUGIN_DIR%\BossResetOnDeath.dll" >nul
if errorlevel 1 (
    echo ERROR: Could not copy the plugin. Close Valheim and try again.
    exit /b 1
)

echo Installed to "%PLUGIN_DIR%".
echo Restart Valheim if it is currently running.
exit /b 0

:uninstall
if exist "%PLUGIN_DIR%\BossResetOnDeath.dll" del /q "%PLUGIN_DIR%\BossResetOnDeath.dll"
if exist "%CONFIG_FILE%" del /q "%CONFIG_FILE%"
if exist "%PLUGIN_DIR%" rmdir "%PLUGIN_DIR%" 2>nul

echo Boss Reset On Death was uninstalled. Vanilla boss persistence is restored.
echo Restart Valheim if it is currently running.
exit /b 0
