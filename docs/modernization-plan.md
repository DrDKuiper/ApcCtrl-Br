# Plano de Modernização da UI do Windows

Este documento descreve a estratégia para atualizar a interface (tray e janelas) do apcctrl no Windows, mantendo o serviço/daemon e drivers atuais.

## Objetivos
- Preservar todas as funcionalidades existentes: serviço do Windows, auto‑start, ícone de tray dinâmico, notificações, diálogos de status/eventos/config.
- Adicionar: tema escuro, UI responsiva, gráficos de carga/bateria, visualização de logs/eventos, botão de auto‑teste, melhor UX.
- Reduzir acoplamento entre UI e core C (daemon), via protocolo NIS já existente (porta do apcctrl).

## Arquitetura proposta
- Manter o serviço `apcctrl` (C) como está (src/win32/winservice.cpp).
- Nova UI em C# (.NET 8): `apctray2` (WPF/WinUI3) fazendo polling via NIS ("status" e "events").
  - Tray moderno com tema claro/escuro e menu de contexto.
  - Janela principal com cartões de métricas, gráficos (p.ex. LiveCharts2/ScottPlot), abas para eventos/logs.
  - Notificações: Toasts nativos (Windows 10+) via `Microsoft.WindowsAppSDK` ou `Microsoft.Toolkit.Uwp.Notifications`.
- Compatibilidade com o atual `apctray.exe`: manter switches `/install`, `/remove`, `/kill` para auto‑start.
- IPC: reusar NIS existente (StatMgr hoje já usa comandos "status" e "events").
- Autoteste: fase 2 — duas opções:
  1) invocar utilitário existente (apctest) quando disponível;
  2) estender NIS para aceitar comando "selftest" (patch no daemon + cliente).

## Milestones
1) Descoberta e mapeamento (feito):
   - Tray antigo: `src/win32/apctray.cpp`, UI: `wintray.cpp`, `winstat.cpp`, `winevents.cpp`, `winconfig.cpp`.
   - Serviço: `src/win32/winservice.cpp`, entrada: `winmain.cpp`.
   - Build Win32: `src/win32/Makefile`, `README.mingw32`, `README.win32`.
   - Protocolo/status: `include/statmgr.h` e `src/lib/statmgr.cpp` (comandos "status"/"events").
2) Scaffold da nova UI (este PR): projeto `src/windows-modern/apctray2` com cliente NIS e janela básica.
3) Tray + polling + status básico + tema escuro.
4) Eventos e notificações (toasts), logs.
5) Gráficos de bateria/carga em tempo real.
6) Autoteste (integrar apctest ou comando NIS novo) + botões de ações.
7) Remoção gradual da UI Win32 antiga ou convivência side‑by‑side (switch de build/installer).

## Considerações de build/installer
- Não alterar o serviço/daemon nem drivers.
- Adicionar etapa opcional de build .NET (dotnet 8 SDK) para `apctray2`.
- Auto‑start: registrar `apctray2.exe` em HKLM\...\Run (compatível com `/install`/`/remove`).
- MSI/NSIS/Inno Setup: incluir novo executável e dependências.

## Riscos e mitigação
- Toasts requerem AppUserModelID e registro de COM: usar pacote Windows App SDK (opcional) ou fallback para balões antigos.
- Ambientes sem .NET: fornecer fallback (apctray antigo) e/ou publicar `apctray2` self‑contained.
- Permissões: eventos/autostart exigem admin; tratar UAC quando necessário.

## Próximos passos
- Implementar polling e UI mínima (feito neste commit: scaffold).
- Definir conjunto de ícones para estados (online, bateria, carregando, commlost) com variantes light/dark.
- Prototipar gráficos (ScottPlot/LiveCharts2) e viewer de eventos.
