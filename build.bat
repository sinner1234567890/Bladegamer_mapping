@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo csc.exe not found! Please ensure .NET Framework is installed.
    exit /b 1
)

echo Compiling BladegamerGUI...
"%CSC%" /target:winexe /win32icon:icon.ico /out:BladegamerGUI_V14.exe /reference:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll,System.Management.dll BladegamerGUI.cs

if %ERRORLEVEL% equ 0 (
    echo Build successful: BladegamerGUI.exe
) else (
    echo Build failed!
)
