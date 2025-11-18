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
    private readonly MetricsStore _metricsStore = new();
    private string _lastState = "COMMLOST";
    private DateTime? _onBatteryStart = null;
    private double? _onBattStartCharge = null;
    private readonly DispatcherTimer _dailyLogTimer;
    private DateTime? _lastDailyLogDate = null;

    public TrayIcon()
    {
        SimpleLogger.Info("Creating TrayIcon and main window");
        _window = new MainWindow();

        _icon = new TaskbarIcon { ToolTipText = "apcctrl" };
        // Default icon at startup
        TrySetIconFromAssets("online");

    var menu = new ContextMenu();
    menu.Items.Add(new MenuItem { Header = "🔍 Status", Command = new RelayCommand(_ => _window.Show()) });
    menu.Items.Add(new MenuItem { Header = "📊 Dashboard Avançado", Command = new RelayCommand(_ => ShowAdvancedWindow()) });
    menu.Items.Add(new MenuItem { Header = "📝 Eventos", Command = new RelayCommand(_ => new EventsWindow(this).Show()) });
    menu.Items.Add(new MenuItem { Header = "⚙️ Configurações", Command = new RelayCommand(_ => new ConfigWindow(this).ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🔌 Detectar Nobreak (COM)", Command = new RelayCommand(_ => new PortDetectWindow().ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "📡 Selecionar Nobreak (NIS)", Command = new RelayCommand(_ => SelectUpsProfile()) });
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
        
        // Timer para log diário do Telegram
        _dailyLogTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _dailyLogTimer.Tick += (s, e) => CheckAndSendDailyLog();
        _dailyLogTimer.Start();
    }

    private AdvancedWindow? _advancedWindow = null;
    
    private void ShowAdvancedWindow()
    {
        try
        {
            SimpleLogger.Info("ShowAdvancedWindow invoked");
            if (_advancedWindow == null || !_advancedWindow.IsVisible)
            {
                SimpleLogger.Info("Creating new AdvancedWindow instance");
                _advancedWindow = new AdvancedWindow();
                _advancedWindow.Closed += (s, e) => _advancedWindow = null;
                _advancedWindow.Show();
            }
            else
            {
                SimpleLogger.Info("Reusing existing AdvancedWindow instance");
                _advancedWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "Failed to show AdvancedWindow");
            _advancedWindow = null;
            MessageBox.Show($"Não foi possível abrir o dashboard avançado.\n\nDetalhes: {ex.Message}",
                "apcctrl", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectUpsProfile()
    {
        // Por enquanto, apenas permite digitar/ajustar o host NIS;
        // no futuro, podemos listar múltiplos perfis.
        var current = Settings.Current.Host;
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Informe o host/IP do servidor NIS (apcctrl):",
            "Selecionar Nobreak",
            string.IsNullOrWhiteSpace(current) ? "127.0.0.1" : current);

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        Settings.Current.Host = input.Trim();
        Settings.Current.Save();

        // Recria o cliente NIS com o novo host
        _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
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
        _metricsStore?.Dispose();
        _icon.Dispose();
    }

    private System.Collections.Generic.List<string> _eventsCache = new();

    public System.Collections.Generic.IReadOnlyList<string> GetEventsCache() => _eventsCache;

    private async System.Threading.Tasks.Task TickAsync()
    {
        try
        {
            SimpleLogger.Info("TickAsync: calling GetStatusAsync");
            var map = await _client.GetStatusAsync();
            SimpleLogger.Info($"TickAsync: status map received with {map.Count} keys");
            
            // Adicionar flags de anomalia
            var status = map.GetValueOrDefault("STATUS", "");
            var lineV = ParseDouble(map.GetValueOrDefault("LINEV"));
            var freq = ParseDouble(map.GetValueOrDefault("LINEFREQ"));
            
            // Detectar anomalias
            var gridAlert = (lineV.HasValue && (lineV < Settings.Current.MinVoltage || lineV > Settings.Current.MaxVoltage))
                || (freq.HasValue && (freq < Settings.Current.MinFrequency || freq > Settings.Current.MaxFrequency))
                || status.Contains("TRIM", StringComparison.OrdinalIgnoreCase)
                || status.Contains("BOOST", StringComparison.OrdinalIgnoreCase);
            
            var battAlert = status.Contains("LOWBATT", StringComparison.OrdinalIgnoreCase)
                || status.Contains("REPLACEBATT", StringComparison.OrdinalIgnoreCase);
            
            var upsAlert = status.Contains("OVERLOAD", StringComparison.OrdinalIgnoreCase)
                || status.Contains("COMMFAULT", StringComparison.OrdinalIgnoreCase);
            
            map["GRID_ALERT"] = gridAlert ? "1" : "0";
            map["BATT_ALERT"] = battAlert ? "1" : "0";
            map["UPS_ALERT"] = upsAlert ? "1" : "0";
            
            // Rastreamento de ciclos
            var isOnBatt = status.Contains("ONBATT", StringComparison.OrdinalIgnoreCase);
            if (isOnBatt && _lastState != "ONBATT")
            {
                Settings.Current.CycleCount++;
                Settings.Current.OnBattStartEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _onBatteryStart = DateTime.Now;
                _onBattStartCharge = ParseDouble(map.GetValueOrDefault("BCHARGE"));
                Settings.Current.Save();
            }
            else if (!isOnBatt && _lastState == "ONBATT" && _onBatteryStart.HasValue)
            {
                Settings.Current.LastOnBattSeconds = (DateTime.Now - _onBatteryStart.Value).TotalSeconds;
                Settings.Current.Save();
                _onBatteryStart = null;
            }
            
            _lastState = status;
            
            // Calcular TIMELEFT estimado
            double? timeLeftEst = null;
            if (isOnBatt && _onBatteryStart.HasValue && _onBattStartCharge.HasValue)
            {
                var elapsed = (DateTime.Now - _onBatteryStart.Value).TotalMinutes;
                var currentCharge = ParseDouble(map.GetValueOrDefault("BCHARGE"));
                if (elapsed > 0 && currentCharge.HasValue && currentCharge < _onBattStartCharge)
                {
                    var discharged = _onBattStartCharge.Value - currentCharge.Value;
                    var ratePerMin = discharged / elapsed;
                    if (ratePerMin > 0.01)
                    {
                        var remaining = currentCharge.Value - 10.0;
                        timeLeftEst = Math.Max(0, remaining / ratePerMin);
                        map["TIMELEFT_EST"] = $"{timeLeftEst:F0} min";
                    }
                }
            }
            
            // Adicionar sample às métricas
            var sample = new MetricSample
            {
                Time = DateTime.Now,
                Charge = ParseDouble(map.GetValueOrDefault("BCHARGE")),
                Load = ParseDouble(map.GetValueOrDefault("LOADPCT")),
                LineV = lineV,
                Freq = freq,
                TimeLeft = ParseDouble(map.GetValueOrDefault("TIMELEFT")),
                TimeLeftEst = timeLeftEst
            };
            _metricsStore.Append(sample);
            
            // Atualiza janela
            SimpleLogger.Info("TickAsync: applying status map to MainWindow");
            _window.ApplyStatusMap(map);

            // Atualiza ícone (red tint se houver alertas)
            if (map.TryGetValue("STATUS", out var st))
            {
                var hasAlert = gridAlert || battAlert || upsAlert;
                var state = st.Contains("COMMLOST", StringComparison.OrdinalIgnoreCase) ? "COMMLOST"
                           : st.Contains("ONBATT", StringComparison.OrdinalIgnoreCase) ? "ONBATT"
                           : st.Contains("ONLINE", StringComparison.OrdinalIgnoreCase) ? "ONLINE"
                           : "CHARGING";
                
                // Se houver alerta, usar ícone de alerta (pode criar alert.ico ou modificar online.ico)
                SetStateIcon(hasAlert ? "commlost" : state);
            }

            // Eventos
            SimpleLogger.Info("TickAsync: calling GetEventsAsync");
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
    
    private static double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && double.TryParse(parts[0], out var result))
            return result;
        return null;
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
    
    private void CheckAndSendDailyLog()
    {
        if (!Settings.Current.TelegramEnabled) return;
        
        var now = DateTime.Now;
        var hour = now.Hour;
        var today = now.Date;
        
        // Enviar uma vez por dia no horário configurado
        if (hour == Settings.Current.DailyLogHour && 
            (_lastDailyLogDate == null || _lastDailyLogDate.Value.Date < today))
        {
            _lastDailyLogDate = now;
            _ = SendDailyLogAsync();
        }
    }
    
    private async System.Threading.Tasks.Task SendDailyLogAsync()
    {
        try
        {
            var upsName = "UPS";
            var todayEvents = _eventsCache
                .Where(e => e.Contains(DateTime.Now.ToString("yyyy-MM-dd")))
                .ToList();
            
            var log = TelegramService.BuildDailyLog(
                upsName,
                Settings.Current.CycleCount,
                Settings.Current.EstimatedCapacityAh,
                Settings.Current.EstimatedCapacitySamples,
                todayEvents
            );
            
            var success = await TelegramService.SendMessageAsync(
                Settings.Current.TelegramBotToken,
                Settings.Current.TelegramChatId,
                log
            );
            
            System.Diagnostics.Debug.WriteLine($"[DailyLog] Telegram send {(success ? "success" : "failed")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DailyLog] Error: {ex.Message}");
        }
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
