ApcCtrl é um branch do projeto apcupsd 3.14.x. Ele foi criado para dar
suporte aos nobreaks da APC no Brasil, que são modelos herdados da Microsol.
Essa aplicação não é oficial da APC Brasil. Foi desenvolvida por terceiros
e ela opera nos modelos da "GNU GENERAL PUBLIC LICENSE". Todo o código fonte
pode ser obtido no site do projeto http://www.apcctrl.com.br

Essa aplicação não exige uma máquina virtual Java. É escrita em liguagem C++
e pode ser cross-compilada para Windows.

A principal mudança em relação ao projeto original está no driver brazil. Esse
driver faz a interface entre os nobreaks da APC Brasil e a aplicação que
controla as ações em caso de eventos (falha de rede, carregamento da bateria,
etc) e os valores de tensão, corrente, agenda de desligamento e religamento,
etc.

Os modelos de nobreak da APC Brasil focados são:
  - BACK-UPS BR 1200VA (BZ1200BI-BR)
  - BACK-UPS BR 1500VA (BZ1500PBI-BR)
  - BACK-UPS BR 2200VA (BZ2200BI-BR e BZ2200I-BR)
  - SMART-UPS BR 1000VA (SUA1000BI-BR e SOLIS1000BI)
  - SMART-UPS BR 1500VA (SUA1500BI-BR e SOLIS1500BI)
  - SMART-UPS BR 2000VA (SUA2000BI-BR)
  - SMART-UPS BR 3000VA (SUA3000BI-BR)
  - STAY 800 (PS800)
  - STAY 700 (PS700)

A interface com o usuário se dá pelos executáveis:
  - apcctrl: daemon que gera as ações.
  - apctest: ferramenta de teste.
  - apcaccess: acessa os parâmetros em tempo de execução do apcctrl
  - smtp: cliente smtp simples (não é recomendado seu uso)   

Configuração:
  - /etc/apcctrl/apcctrl.conf
  
Controle de eventos:
  - /etc/apcctrl/apccontrol: controle de eventos. O daemon apcctrl chama
    esse script que pode ser alterado pelo usuário para definir como o
    servidor deve reagir para desligar, hibernar, enviar email, etc.

 

Compilação (Linux e macOS)
==========================

O projeto usa autotools (script `configure`). Em sistemas tipo Unix (Linux ou
macOS/Darwin) o fluxo geral é:

1. Instalar ferramentas de desenvolvimento.
2. Executar `./configure` com as opções desejadas.
3. Rodar `make`.
4. (Opcional) `sudo make install`.

Pré‑requisitos gerais (Linux):
  - Compilador C/C++ (gcc ou clang)
  - make, autoconf/automake (já gerados no repositório normalmente)
  - libusb (quando for usar comunicação USB/HID)
  - pkg-config (facilita a detecção de libs)

Pré‑requisitos macOS (Intel ou Apple Silicon):
  - Xcode Command Line Tools: `xcode-select --install`
  - Homebrew (https://brew.sh) para instalar dependências
  - `brew install libusb pkg-config gettext`
    (Instale `libusb-compat` apenas se o `./configure` reclamar.)

Diretório específico macOS: `platforms/darwin/` contém scripts/plists:
  - `org.apcctrl.apcctrl.plist.in` (modelo para launchd service)
  - Scripts de instalação (`apcctrl-start.in`, `apcctrl-uninstall.in`)

Exemplo de build no macOS:
```
chmod +x configure
make distclean 2>/dev/null || true
./configure --enable-nls --prefix=/usr/local
make -j$(sysctl -n hw.ncpu)
sudo make install
```

Observações macOS:
  - Em Apple Silicon, o prefixo do Homebrew é normalmente `/opt/homebrew`;
    se necessário exporte:
    `export CPPFLAGS="-I/opt/homebrew/include" LDFLAGS="-L/opt/homebrew/lib"`
  - Para executar como serviço em background, gere o plist final a partir de
    `org.apcctrl.apcctrl.plist.in` e coloque em `/Library/LaunchDaemons/` ou
    `$HOME/Library/LaunchAgents/` conforme o caso, depois `launchctl load -w`.
  - Acesso USB pode exigir execução com permissões elevadas.

Exemplo rápido de teste pós-build:
```
./src/apcctrl --version
./src/apcaccess status
```

Opções úteis de `./configure` (consulte `./configure --help`):
  --enable-nls           Ativa suporte a localização
  --enable-powerflute    Compila utilitário ncurses de monitoramento
  --with-included-gettext Usa a lib gettext inclusa no projeto
  --with-libwrap[=DIR]   Habilita libwrap para controle de acesso

Caso alguma opção aqui não exista na sua cópia, valide sempre com
`./configure --help`, pois novas modificações podem adicionar ou remover flags.

Se quiser contribuir com melhorias específicas para macOS (ex.: assinatura de
binários, universal build x86_64+arm64), abra uma issue descrevendo o caso.


Interface/gráfico (Windows e macOS)
===================================

- Windows (moderno):
  - Nova UI em `src/windows-modern/apctray2` (WPF, .NET 8) com tray icon e janelas
    de Status/Eventos/Configuração. Roadmap inclui tema escuro automático e Autoteste.
  - Build (no Windows, com .NET 8 SDK):
    powershell: `dotnet build src/windows-modern/apctray2/apctray2.csproj -c Release`

- macOS: menu bar app “apcagent” (Objective‑C/Cocoa)
  - Código em `src/apcagent/` com ícone na barra de menus, notificações (Notification Center),
    janelas de Status e Eventos e janela de Preferências (host/porta/intervalo/notificações).
  - Build (no macOS):
    1) Compilar o projeto normalmente (veja seção acima) para gerar as libs
    2) Criar o app: `make -C src/apcagent apcagent.app`
    3) Instalar no /Applications (opcional): `sudo make -C src/apcagent install-apcagent`
    4) Executar: `open src/apcagent/apcagent.app`

Paridade de recursos Windows ↔ macOS
- Ícone dinâmico de estado (OK/Carregando/Em bateria/Conexão perdida): presente no macOS (apcagent).
- Notificações de eventos: presente no macOS via Notification Center.
- Status/Eventos/Configuração: presentes no macOS (janelas nativas do apcagent).
- Autoteste: no Windows‑modern está no roadmap; no macOS ainda não exposto no menu. Pode ser
  adicionado em uma próxima iteração via integração com `apctest` ou extensão do protocolo NIS.



