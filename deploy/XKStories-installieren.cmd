@echo off
chcp 65001 >nul
title XKStories installieren
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-xkstories.ps1"
echo.
pause
