# ApcCtrl-Br
Versão atualizada para parametros e tecnologias novas do antigo apcctrl

Branch do projeto apcctrl/apcupsd com foco nos nobreaks APC Brasil. Esta versão
atualiza parâmetros e tecnologias do antigo apcctrl, com destaque para o driver
"Brasileiro".

## Modelos foco

- BACK-UPS BR (BZ1200BI-BR, BZ1500PBI-BR, BZ2200BI-BR, BZ2200I-BR)
- SMART-UPS BR (SUA1000BI-BR, SOLIS1000BI, SUA1500BI-BR, SOLIS1500BI, SUA2000BI-BR, SUA3000BI-BR)
- STAY 700/800 (PS700/PS800)

Consulte `README.txt` para lista completa e detalhes.

## Executáveis principais

- `apcctrl`: daemon de monitoramento/ações
- `apcaccess`: consulta parâmetros
- `apctest`: testes de comunicação

## Interfaces modernas (Windows e macOS)

O projeto inclui front‑ends modernos para monitoramento em tempo real, além dos
executáveis clássicos:

- **Windows (WPF)** – pasta `src/windows-modern/apctray2/`
	- Tray moderno com cards de bateria, carga, tensão, temperatura e fluxo de energia.
	- **Saúde da bateria**: estima a capacidade em Ah em relação à nominal e exibe
		um percentual de "health" (verde/amarelo/vermelho) com detalhes de ciclos e idade.
	- **Ciclos e recarga completa**: registra quando o nobreak entra em bateria,
		quanto tempo ficou em bateria e quanto tempo levou para recarregar até 100%
		após voltar para a rede (status mostrado na janela avançada).

- **macOS (SwiftUI + AppKit)** – pasta `src/macos-modern/`
	- Ícone na barra de menus com tooltip detalhado de status (status, bateria,
		tempo em bateria, etc.).
	- Janela de status em SwiftUI com cards de bateria, carga, ciclos, capacidade
		estimada e diagrama de fluxo de energia.
	- **Ciclos de bateria**: conta automaticamente cada transição para `ONBATT`
		e exibe o último ciclo em bateria.
	- **Recarga pós‑bateria**: ao sair de bateria e voltar para a rede, acompanha
		a recarga até ~100% de `BCHARGE`, registra o tempo gasto e mostra no tooltip
		como "Última recarga completa".
	- **Notificações/Telegram**: integra com `UserNotifications` e Telegram
		(bot/Chat ID configuráveis na UI) para enviar alertas de eventos, incluindo
		aviso quando a bateria volta a 100% após um ciclo em bateria.

## Build rápido (Linux/macOS)

Pré‑requisitos:

```bash
# Linux (Debian/Ubuntu)
sudo apt install build-essential libusb-1.0-0-dev pkg-config gettext

# macOS
xcode-select --install
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
brew install libusb pkg-config gettext
```

Compilar e instalar:

```bash
chmod +x configure
make distclean 2>/dev/null || true
./configure --enable-nls --prefix=/usr/local
make -j"$(getconf _NPROCESSORS_ONLN 2>/dev/null || sysctl -n hw.ncpu)"
sudo make install
```

Teste rápido:

```bash
./src/apcctrl --version
./src/apcaccess status
```

### Notas macOS

- Recursos em `platforms/darwin/` (scripts, pkg, plist `launchd`).
- Apple Silicon: se necessário exporte:

```bash
export CPPFLAGS="-I/opt/homebrew/include" LDFLAGS="-L/opt/homebrew/lib"
```

- Empacotamento/compatibilidade avançada: ver `platforms/darwin/build-notes.txt`.

### Opções comuns do ./configure (confirme com `./configure --help`)

- `--enable-nls` localização
- `--enable-powerflute` monitor ncurses
- `--with-included-gettext` usa gettext interna
- `--with-libwrap[=DIR]` libwrap

## Licença

GPL (ver `COPYING`).
