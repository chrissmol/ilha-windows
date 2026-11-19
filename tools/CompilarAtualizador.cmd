@echo off
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:"%~dp0AtualizarTito.exe" "%~dp0AtualizarTito.cs"
