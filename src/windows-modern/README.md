# windows-modern

Protótipo da nova UI para Windows (tray + janelas) baseada em .NET, mantendo o serviço `apcctrl` existente.

- Projeto: `apctray2` (WPF, .NET 8)
- Comunicação: protocolo NIS do apcctrl (comandos `status` e `events`).

## Pré-requisitos

- Windows 10/11
- .NET SDK 8.0+

## Como compilar

```powershell
dotnet build src/windows-modern/apctray2/apctray2.csproj -c Release
```

## Como executar

```powershell
./src/windows-modern/apctray2/bin/Release/net8.0-windows/apctray2.exe
```

Por padrão conecta em `localhost:3551`. Ajuste no menu Configurações.

## Roadmap curto

- [ ] Tray com ícone dinâmico e menu de contexto (Status, Eventos, Config, Autoteste, Sair)
- [ ] Tema escuro automático
- [ ] Janela de Status com cartões e gráficos
- [ ] Eventos + notificações (toasts)
- [ ] Autoteste (apctest ou novo comando NIS)
