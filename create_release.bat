@echo off
echo ========================================
echo Building BladegamerGUI...
echo ========================================
call build.bat
if %ERRORLEVEL% NEQ 0 (
    echo Build failed. Aborting release creation.
    pause
    exit /b 1
)

echo.
echo ========================================
echo Packaging files into ZIP...
echo ========================================
set "RELEASE_DIR=ignore realse folder"
set "ZIP_NAME=BladegamerGUI_Release_V19.zip"
set "DEST_PATH=%RELEASE_DIR%\%ZIP_NAME%"

if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"

if exist "%DEST_PATH%" del "%DEST_PATH%"

powershell -nologo -noprofile -command "Compress-Archive -Path 'BladegamerGUI_V19.exe', 'Bladegamer_mapping.ino', 'assets', 'icon.ico', 'LICENSE.txt', 'arduino-cli.exe' -DestinationPath '%DEST_PATH%' -Force"

if exist "%DEST_PATH%" (
    echo.
    echo ========================================
    echo SUCCESS! Release created: %DEST_PATH%
    echo You can now upload this zip to GitHub.
    echo ========================================
) else (
    echo.
    echo ERROR: Failed to create release zip.
)

pause
