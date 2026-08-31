@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "SOURCE=%~dp0src\NetSecSetupClassic\Program.cs"
set "MANIFEST=%~dp0src\NetSecSetupClassic\app.manifest"
set "JSON=%~dp0src\NetSecSetupClassic\packages.json"
set "OUTPUT=%~dp0publish"

if not exist "%CSC%" (
  echo .NET Framework compiler was not found on this Windows installation.
  exit /b 1
)

if not exist "%OUTPUT%" mkdir "%OUTPUT%"
"%CSC%" /nologo /target:winexe /out:"%OUTPUT%\NetSecSetup.exe" /win32manifest:"%MANIFEST%" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /win32icon:"%~dp0assets\logo.ico" "%SOURCE%"
if errorlevel 1 exit /b %errorlevel%

copy "%JSON%" "%OUTPUT%\packages.json" >nul
if errorlevel 1 exit /b %errorlevel%

copy "%~dp0assets\logo.png" "%OUTPUT%\logo.png" >nul
if errorlevel 1 exit /b %errorlevel%

echo.
echo Build completed: %OUTPUT%\NetSecSetup.exe
