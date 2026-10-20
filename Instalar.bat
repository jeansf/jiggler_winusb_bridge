@echo off
chcp 65001 >nul
echo ====================================================================
echo   Instalador do Driver e Bridge 2.4G Mouse (VID_062A / PID_4101)
echo ====================================================================
echo.

:: Verifica privilegios de Administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Solicitando permissao de Administrador...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

set "INSTALL_DIR=%LOCALAPPDATA%\UsbMouseBridge"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [1/4] Copiando executavel para %INSTALL_DIR%...
taskkill /f /im UsbMouseBridge.exe >nul 2>&1
if not exist "%~dp0UsbMouseBridge.exe" (
    echo Compilando UsbMouseBridge.cs...
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:"%~dp0UsbMouseBridge.exe" "%~dp0UsbMouseBridge.cs"
)
copy /y "%~dp0UsbMouseBridge.exe" "%INSTALL_DIR%\UsbMouseBridge.exe" >nul

echo.
echo [2/4] Instalando certificado e driver WinUSB (Interface 1 - MI_01)...
certutil -addstore -f "Root" "%~dp0driver\2.4G_Mouse_(Interface_1).cat" >nul 2>&1
certutil -addstore -f "TrustedPublisher" "%~dp0driver\2.4G_Mouse_(Interface_1).cat" >nul 2>&1
pushd "%~dp0driver"
installer_x64.exe "2.4G_Mouse_(Interface_1).inf"
if %errorLevel% neq 0 (
    pnputil /add-driver "%~dp0driver\2.4G_Mouse_(Interface_1).inf" /install
)
popd

echo.
echo [3/4] Configurando inicializacao automatica no Windows...
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Startup') + '\UsbMouseBridge.lnk'); $s.TargetPath='%INSTALL_DIR%\UsbMouseBridge.exe'; $s.WorkingDirectory='%INSTALL_DIR%'; $s.Description='Driver Bridge para 2.4G Mouse (VID_062A&PID_4101)'; $s.Save()"

echo.
echo [4/4] Iniciando servico UsbMouseBridge em segundo plano...
start "" "%INSTALL_DIR%\UsbMouseBridge.exe"

echo.
echo ====================================================================
echo   Instalacao concluida com sucesso! O dispositivo ja esta ativo.
echo ====================================================================
pause
