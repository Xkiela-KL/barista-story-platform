@echo off
chcp 65001 >nul
title XKStories installieren (automatisch)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-xkstories.ps1" -NoPrompt
echo.
pause
