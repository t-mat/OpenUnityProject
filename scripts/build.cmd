@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"
powershell .\build.ps1
