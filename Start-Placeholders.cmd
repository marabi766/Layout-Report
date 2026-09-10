@echo off
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0Build-and-Run.ps1" -BuildOnly
if errorlevel 1 exit /b %errorlevel%
start "" "%~dp0bin\ReportLayout.exe" --image-placeholders
