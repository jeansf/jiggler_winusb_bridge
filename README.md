# Jiggler WinUSB Bridge (`VID_062A&PID_4101`)

Solução completa de driver em modo usuário (WinUSB + Bridge C#) para dongles USB **2.4G Mouse / Mouse Jiggler** baseados no chipset **MosArt Semiconductor (`VID_062A&PID_4101`)** que falham no Windows 10/11 com **Erro Código 10**.

---

## O Problema

Ao conectar este dispositivo no Windows 11, a **Interface 1 (`USB\VID_062A&PID_4101&MI_01`)** falha ao iniciar no Gerenciador de Dispositivos com a mensagem:

> **Código 10:** *"Falha na validação do Descritor de Relatório HID. Um item principal não constante foi declarado sem um uso correspondente."*

### Causa Técnica (Bug no Firmware de Fábrica)
No descritor HID de 168 bytes gravado na memória ROM do dispositivo, na coleção de `System Control` (`Report ID 2`), o fabricante gravou o byte **`0x1D`** em vez de **`0x19`** (`USAGE_MINIMUM (0x81)`):
```text
05 01 09 80 A1 01 85 02 [1D 81] 29 83 25 01 75 01 95 03 81 02
```
* **No Android / Linux:** O parser HID ignora a tag inválida e carrega o mouse normalmente.
* **No Windows 11:** O parser `hidparse.sys` aplica validação estrita e bloqueia a interface inteira.

---

## Como Funciona Esta Solução

1. **Driver WinUSB (`driver/2.4G_Mouse_(Interface_1).inf`):** Substitui o driver `HidUsb.sys` nativo da Microsoft na Interface 1 (`MI_01`) pelo driver `WinUSB.sys`, contornando o validador `hidparse.sys`.
2. **Serviço Bridge (`UsbMouseBridge.exe` / `UsbMouseBridge.cs`):**
   * Abre o pipe de interrupção `Endpoint 0x82` via `winusb.dll`.
   * Lê os pacotes proprietários de **7 bytes** com coordenadas X/Y assinadas de **12 bits** (`Report ID 3`).
   * Injeta os movimentos e cliques nativamente no Windows via `user32.dll` (`mouse_event`).
   * Roda silenciosamente em segundo plano (sem janela de console) e reconecta automaticamente ao plugar/desplugar o USB.

---

## Instalação Rápida (1 Clique)

1. Baixe o arquivo **`jiggler_winusb_bridge_windows.zip`** na aba [**Releases**](../../releases) e extraia em uma pasta.
2. Conecte o dispositivo USB no PC.
3. Dê dois cliques em **`Instalar.bat`** e confirme a permissão de Administrador.

O script irá:
* Instalar o certificado e o driver WinUSB para `USB\VID_062A&PID_4101&MI_01`.
* Copiar o `UsbMouseBridge.exe` para `%LOCALAPPDATA%\UsbMouseBridge`.
* Configurar o atalho na Inicialização do Windows (`shell:startup`).
* Iniciar o serviço imediatamente.

### Desinstalação
Para encerrar o serviço e remover da inicialização automática do Windows, execute **`Desinstalar.bat`**.
