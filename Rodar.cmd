@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Compilando a Tito neste PC...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%~dp0' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue; & '%~dp0Compilar.ps1'"
if errorlevel 1 goto falhou
start "" "%~dp0Tito.exe"
timeout /t 5 /nobreak >nul
tasklist /fi "imagename eq TitoGlass.exe" | find /i "TitoGlass.exe" >nul && goto fim
:falhou
echo.
echo A Tito nao abriu. Gerando diagnostico...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Diagnostico.ps1"
pause
:fim
