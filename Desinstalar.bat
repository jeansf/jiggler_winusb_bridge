@echo off
chcp 65001 >nul
echo ====================================================================
echo   Desinstalador do UsbMouseBridge (VID_062A / PID_4101)
echo ====================================================================
echo.

echo [1/3] Encerrando servico em segundo plano...
taskkill /f /im UsbMouseBridge.exe >nul 2>&1

echo [2/3] Removendo atalho da inicializacao do Windows...
del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\UsbMouseBridge.lnk" >nul 2>&1

echo [3/3] Removendo arquivos locais...
rmdir /s /q "%LOCALAPPDATA%\UsbMouseBridge" >nul 2>&1

echo.
echo Desinstalacao concluida!
pause
