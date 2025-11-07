using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace apctray2;

public sealed class TrayIcon : IDisposable
{
    public static TrayIcon? Current { get; set; }
    private readonly TaskbarIcon _icon;
    private readonly MainWindow _window;
    private NisClient _client = new(Settings.Current.Host, Settings.Current.Port);
    private readonly DispatcherTimer _timer;
    private string _lastEventLine = string.Empty;

    public TrayIcon()
    {
        _window = new MainWindow();

        _icon = new TaskbarIcon { ToolTipText = "apcctrl" };
        // Default icon at startup
        TrySetIconFromAssets("online");

    var menu = new ContextMenu();
    menu.Items.Add(new MenuItem { Header = "🔍 Status", Command = new RelayCommand(_ => _window.Show()) });
    menu.Items.Add(new MenuItem { Header = "📊 Dashboard Avançado", Command = new RelayCommand(_ => new AdvancedWindow().Show()) });
    menu.Items.Add(new MenuItem { Header = "📝 Eventos", Command = new RelayCommand(_ => new EventsWindow(this).Show()) });
    menu.Items.Add(new MenuItem { Header = "⚙️ Configurações", Command = new RelayCommand(_ => new ConfigWindow(this).ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🔌 Detectar Nobreak (COM)", Command = new RelayCommand(_ => new PortDetectWindow().ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🛠️ Autoteste", Command = new RelayCommand(_ => _window.SelfTest_Relay()) });
    
    MenuItem autoStartItem = null!;
    autoStartItem = new MenuItem 
    { 
        Header = "🚀 " + (AutoStartManager.IsEnabled() ? "✓" : "") + " Iniciar com o Windows",
        Command = new RelayCommand(_ => 
        {
            if (AutoStartManager.IsEnabled())
            {
                AutoStartManager.Disable();
                autoStartItem.Header = "🚀 Iniciar com o Windows";
            }
            else
            {
                AutoStartManager.Enable();
                autoStartItem.Header = "🚀 ✓ Iniciar com o Windows";
            }
        })
    };
    menu.Items.Add(autoStartItem);
    
    menu.Items.Add(new Separator());
    menu.Items.Add(new MenuItem { Header = "Sair", Command = new RelayCommand(_ => Application.Current.Shutdown()) });
        _icon.ContextMenu = menu;

        _icon.TrayMouseDoubleClick += (s, e) => _window.Show();

        // Polling de status/eventos
    _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, Settings.Current.RefreshSeconds)) };
        _timer.Tick += async (s, e) => await TickAsync();
        _timer.Start();
    }

    public void SetStateIcon(string state)
    {
        var name = state switch
        {
            "ONLINE" => "online",
            "ONBATT" => "onbatt",
            "COMMLOST" => "commlost",
            _ => "charging"
        };
        TrySetIconFromAssets(name);
    }

    private void TrySetIconFromAssets(string name)
    {
        try
        {
            // Tentar primeiro no diretório Assets
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", $"{name}.ico");
            if (!File.Exists(path))
            {
                // Fallback: procurar no diretório raiz
                path = Path.Combine(AppContext.BaseDirectory, $"{name}.ico");
            }
            
            if (File.Exists(path))
            {
                // Usar Icon do System.Drawing para compatibilidade
                using var iconStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var icon = new System.Drawing.Icon(iconStream);
                _icon.Icon = icon;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Ícone não encontrado: {path}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar ícone '{name}': {ex.Message}");
        }
    }

    public void Dispose()
    {
        _icon.Dispose();
    }

    private System.Collections.Generic.List<string> _eventsCache = new();

    public System.Collections.Generic.IReadOnlyList<string> GetEventsCache() => _eventsCache;

    private async System.Threading.Tasks.Task TickAsync()
    {
        try
        {
            var map = await _client.GetStatusAsync();
            // Atualiza janela
            _window.ApplyStatusMap(map);

            // Atualiza ícone
            if (map.TryGetValue("STATUS", out var st))
            {
                var state = st.Contains("COMMLOST", StringComparison.OrdinalIgnoreCase) ? "COMMLOST"
                           : st.Contains("ONBATT", StringComparison.OrdinalIgnoreCase) ? "ONBATT"
                           : st.Contains("ONLINE", StringComparison.OrdinalIgnoreCase) ? "ONLINE"
                           : "CHARGING";
                SetStateIcon(state);
            }

            // Eventos
            var ev = await _client.GetEventsAsync();
            _eventsCache = ev;
            if (ev.Count > 0)
            {
                var last = ev[^1];
                if (!string.Equals(last, _lastEventLine, StringComparison.Ordinal))
                {
                    _lastEventLine = last;
                    ShowEventBalloon(last);
                }
            }
        }
        catch
        {
            // Silencioso por enquanto
        }
    }

    public void ShowEventBalloon(string text)
    {
        _icon.ShowBalloonTip("APC UPS", text, BalloonIcon.Info);
    }

    public void RefreshClientFromSettings()
    {
        _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, Settings.Current.RefreshSeconds));
    }
}

public sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    { _execute = execute; _canExecute = canExecute; }
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
