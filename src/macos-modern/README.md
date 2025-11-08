# macos-modern

Protótipo de agente moderno para macOS em Swift. Complementa o `apcagent` legado (Objective-C) oferecendo uma base mais simples para evoluções (SwiftUI, gráfico de carga, etc.).

## Objetivos

- Ícone na barra de menus com mudança de acordo com estado (ONLINE, ONBATT, CHARGING, COMMLOST)
- Menu com: Status, Eventos (com notificação), Configuração (persistência), Autoteste (placeholder), Sair
- Consulta periódica ao daemon `apcctrl` via protocolo NIS (porta 3551 por padrão)
- Estrutura pronta para migrar para SwiftUI (janela de status futura)

## Build

Requer macOS 12+ e Xcode (ou apenas toolchain Swift).

```bash
cd src/macos-modern
swift build -c release
```

Executar:

```bash
swift run
```

(Para criar um app .app futuramente: gerar Package, adicionar Info.plist e empacotar com `swift build --configuration release` + `mkdir -p MyApp.app/Contents/MacOS` etc.)

## Roadmap

- [x] Eventos (consulta + notificação simples)
- [x] Persistência de host/porta/intervalo via UserDefaults + diálogo
- [x] Janela de Eventos (scroll, auto-atualização)
- [x] Janela de Status SwiftUI (cards com métricas principais)
- [ ] Gráfico de carga/bateria na janela Status
- [ ] Ícones customizados (SF Symbols refinados ou assets) para estados adicionais (LOWBATT, OVERLOAD)
- [ ] Filtro/exportar eventos
- [ ] Autoteste (integrar com comando NIS ou wrapper apctest; relatório modal)
- [ ] Assinatura e notarização

## Licença

GPLv2 (igual ao restante do projeto).
