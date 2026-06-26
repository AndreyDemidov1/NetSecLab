@echo off
cd /d "%~dp0src\NetSecLab.App"
dotnet restore
dotnet run
pause
