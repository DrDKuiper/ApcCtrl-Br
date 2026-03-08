using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace apctray2;

public sealed class TrayIcon : IDisposable
{
    public static TrayIcon? Current { get; set; }
    private readonly TaskbarIcon _icon;
    private MainWindow _window;
    private NisClient _client = new(Settings.Current.Host, Settings.Current.Port);
    private readonly DispatcherTimer _timer;
    private string _lastEventLine = string.Empty;
    private readonly MetricsStore _metricsStore = new();
    private string _lastState = "COMMLOST";
    private string _lastStatusText = "";
    private string _lastStatusOnly = "--";
    private string _lastTrayState = "commlost";
    private MenuItem? _summaryItem;
    private TextBlock? _summaryTitleText;
    private TextBlock? _summaryEndpointText;
    private TextBlock? _summaryStatusText;
    private TextBlock? _summaryBatteryText;
    private TextBlock? _summaryLoadText;
    private TextBlock? _summaryLineText;
    private DateTime? _onBatteryStart = null;
    private double? _onBattStartCharge = null;
    private readonly DispatcherTimer _dailyLogTimer;
    private DateTime? _lastDailyLogDate = null;

    public TrayIcon()
    {
        SimpleLogger.Info("Creating TrayIcon and main window");
        _window = new MainWindow();

        _icon = new TaskbarIcon { ToolTipText = "" };
        // Default icon at startup
        SetStateIcon("online");

    var menu = new ContextMenu();
    ApplyDarkMenuStyle(menu);

    // Resumo no topo do menu do tray
    _summaryItem = new MenuItem { IsEnabled = true, IsHitTestVisible = false, StaysOpenOnClick = true };
    _summaryItem.Header = BuildTraySummaryHeader();
    _summaryItem.Padding = new Thickness(0);
    _summaryItem.Margin = new Thickness(0);
    _summaryItem.BorderThickness = new Thickness(0);
    _summaryItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    _summaryItem.Background = GetBrush("BgBaseBrush", System.Windows.Media.Brushes.Black);
    _summaryItem.Template = BuildSummaryMenuItemTemplate();
    menu.Items.Add(_summaryItem);
    menu.Items.Add(new Separator());

    menu.Items.Add(new MenuItem { Header = "📊 Dashboard Avançado", Command = new RelayCommand(_ => ShowAdvancedWindow()) });
    menu.Items.Add(new MenuItem { Header = "📅 Eventos", Command = new RelayCommand(_ => new EventsWindow(this).Show()) });
    menu.Items.Add(new MenuItem { Header = "⚙️ Configurações", Command = new RelayCommand(_ => new ConfigWindow(this).ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🔌 Detectar Nobreak (COM)", Command = new RelayCommand(_ => new PortDetectWindow().ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🏢 Gerenciar Nobreaks (NIS)", Command = new RelayCommand(_ => new UpsProfilesWindow(this).ShowDialog()) });
    menu.Items.Add(new MenuItem { Header = "🧪 Autoteste", Command = new RelayCommand(_ => _window.SelfTest_Relay()) });
    
    MenuItem autoStartItem = null!;
    autoStartItem = new MenuItem 
    { 
        Header = (AutoStartManager.IsEnabled() ? "✅ " : "⬜ ") + "Iniciar com o Windows",
        Command = new RelayCommand(_ => 
        {
            if (AutoStartManager.IsEnabled())
            {
                AutoStartManager.Disable();
                autoStartItem.Header = "⬜ Iniciar com o Windows";
            }
            else
            {
                AutoStartManager.Enable();
                autoStartItem.Header = "✅ Iniciar com o Windows";
            }
        })
    };
    menu.Items.Add(autoStartItem);
    
    menu.Items.Add(new Separator());
    menu.Items.Add(new MenuItem { Header = "❌ Sair", Command = new RelayCommand(_ => Application.Current.Shutdown()) });
        _icon.ContextMenu = menu;

        _icon.TrayMouseDoubleClick += (s, e) => ShowAdvancedWindow();

        // Polling de status/eventos
    _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, Settings.Current.RefreshSeconds)) };
        _timer.Tick += async (s, e) => await TickAsync();
        _timer.Start();
        
        // Timer para log diário do Telegram
        _dailyLogTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _dailyLogTimer.Tick += (s, e) => CheckAndSendDailyLog();
        _dailyLogTimer.Start();
        
        // Sincronizar com o perfil ativo
        RefreshClientFromSettings();
    }
    
    public void RefreshClientFromSettings()
    {
        var activeProfile = Settings.Current.ProfileManager.GetActiveProfile();
        if (activeProfile != null)
        {
            _client = new NisClient(activeProfile.Host, activeProfile.Port);
            Settings.Current.Host = activeProfile.Host;
            Settings.Current.Port = activeProfile.Port;
            SimpleLogger.Info($"Cliente NIS atualizado para: {activeProfile.Name} ({activeProfile.Host}:{activeProfile.Port})");
        }
        else
        {
            // Fallback para configurações legadas
            _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
            SimpleLogger.Info($"Cliente NIS usando config legado: {Settings.Current.Host}:{Settings.Current.Port}");
        }

        _window.RefreshClientFromSettings();
        _window.ReloadProfiles();
        _advancedWindow?.RefreshClientFromSettings();
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, Settings.Current.RefreshSeconds));
        _ = TickAsync();
    }

    private void ShowStatusWindow()
    {
        try
        {
            _window.ReloadProfiles();
            _window.RefreshClientFromSettings();

            if (!_window.IsVisible)
                _window.Show();

            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;

            _window.Activate();
        }
        catch (InvalidOperationException)
        {
            _window = new MainWindow();
            _window.Show();
            _window.Activate();
        }
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
    
    public void SetStateIcon(string state)
    {
        var name = state switch
        {
            "ONLINE" => "online",
            "ONBATT" => "onbatt",
            "COMMLOST" => "commlost",
            "OVERLOAD" => "overload",
            "COMMFAULT" => "commfault",
            "LOWBATT" => "lowbatt",
            "ALERT" => "alert",
            _ => "charging"
        };

        _lastTrayState = name;

        if (!TrySetIconFromAssets(name))
        {
            // Force refresh do ícone no tray
            _icon.Icon = null;
            _icon.Icon = CreateStateIcon(name);
        }

        UpdateTrayTooltip();
        UpdateTraySummaryText();
    }

    private bool TrySetIconFromAssets(string name)
    {
        try
        {
            // Usar apenas ícones modernos em Assets (ignorar ícones legados na raiz)
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", $"{name}.ico");
            
            if (File.Exists(path))
            {
                // Usar Icon do System.Drawing para compatibilidade
                using var iconStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var icon = new System.Drawing.Icon(iconStream);
                _icon.Icon = icon;
                return true;
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

        return false;
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

            _lastStatusText = BuildStatusSummary(map);
            _lastStatusOnly = map.GetValueOrDefault("STATUS", "--");
            UpdateTrayTooltip();
            UpdateTraySummary(map);
            
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
                           : st.Contains("OVERLOAD", StringComparison.OrdinalIgnoreCase) ? "OVERLOAD"
                           : st.Contains("COMMFAULT", StringComparison.OrdinalIgnoreCase) ? "COMMFAULT"
                           : st.Contains("LOWBATT", StringComparison.OrdinalIgnoreCase) ? "LOWBATT"
                           : st.Contains("REPLACEBATT", StringComparison.OrdinalIgnoreCase) ? "LOWBATT"
                           : st.Contains("ONBATT", StringComparison.OrdinalIgnoreCase) ? "ONBATT"
                           : st.Contains("ONLINE", StringComparison.OrdinalIgnoreCase) ? "ONLINE"
                           : "CHARGING";

                // Se houver alerta de qualidade de energia, usar estado ALERT
                if (hasAlert && state == "ONLINE")
                    state = "ALERT";

                SetStateIcon(state);
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

    private void UpdateTrayTooltip()
    {
        // Tooltip desativado: usamos o resumo no menu do tray
        _icon.ToolTipText = string.Empty;
    }

    private UIElement BuildTraySummaryHeader()
    {
        var fgPrimary = GetBrush("TextPrimaryBrush", System.Windows.Media.Brushes.White);
        var fgSecondary = GetBrush("TextSecondaryBrush", System.Windows.Media.Brushes.LightGray);
        var fgMuted = GetBrush("TextMutedBrush", System.Windows.Media.Brushes.Gray);
        var accent = GetBrush("AccentBrush", System.Windows.Media.Brushes.DeepSkyBlue);

        // Outer container: no visible card border, just padding
        var outer = new DockPanel { Margin = new Thickness(6, 4, 6, 2), LastChildFill = true };

        // Left accent bar (thin vertical line)
        var accentBar = new Border
        {
            Background = accent,
            Width = 3,
            CornerRadius = new CornerRadius(1.5),
            Margin = new Thickness(0, 1, 8, 1),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        DockPanel.SetDock(accentBar, Dock.Left);
        outer.Children.Add(accentBar);

        var stack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };

        // Título + endpoint
        _summaryTitleText = new TextBlock
        {
            Text = "Nobreak",
            FontWeight = FontWeights.SemiBold,
            Foreground = fgPrimary,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _summaryEndpointText = new TextBlock
        {
            Text = "--",
            Foreground = fgMuted,
            FontSize = 9.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 5)
        };

        stack.Children.Add(_summaryTitleText);
        stack.Children.Add(_summaryEndpointText);

        // Grid 2x2 de stats
        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _summaryStatusText = new TextBlock
        {
            Text = "Status: --",
            Foreground = fgSecondary,
            FontSize = 10.5,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _summaryBatteryText = new TextBlock
        {
            Text = "Bat: --",
            Foreground = fgSecondary,
            FontSize = 10.5,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _summaryLoadText = new TextBlock
        {
            Text = "Carga: --",
            Foreground = fgSecondary,
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _summaryLineText = new TextBlock
        {
            Text = "Entrada: --",
            Foreground = fgSecondary,
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        Grid.SetRow(_summaryStatusText, 0);
        Grid.SetColumn(_summaryStatusText, 0);
        Grid.SetRow(_summaryBatteryText, 0);
        Grid.SetColumn(_summaryBatteryText, 1);
        Grid.SetRow(_summaryLoadText, 1);
        Grid.SetColumn(_summaryLoadText, 0);
        Grid.SetRow(_summaryLineText, 1);
        Grid.SetColumn(_summaryLineText, 1);

        grid.Children.Add(_summaryStatusText);
        grid.Children.Add(_summaryBatteryText);
        grid.Children.Add(_summaryLoadText);
        grid.Children.Add(_summaryLineText);

        stack.Children.Add(grid);
        outer.Children.Add(stack);
        return outer;
    }

    private void UpdateTraySummary(Dictionary<string, string> map)
    {
        if (_summaryTitleText == null) return;

        var active = Settings.Current.ProfileManager.GetActiveProfile();
        var profileName = active?.Name ?? "Nobreak";
        var endpoint = active != null ? $"{active.Host}:{active.Port}" : $"{Settings.Current.Host}:{Settings.Current.Port}";
        _summaryTitleText.Text = profileName;
        _summaryEndpointText!.Text = endpoint;

        var statusText = $"Status: {map.GetValueOrDefault("STATUS", "--")}";
        var batteryText = $"Bat: {map.GetValueOrDefault("BCHARGE", "--")}";
        var loadText = $"Carga: {map.GetValueOrDefault("LOADPCT", "--")}";
        var lineText = $"Entrada: {map.GetValueOrDefault("LINEV", "--")}";

        _summaryStatusText!.Text = statusText;
        _summaryBatteryText!.Text = batteryText;
        _summaryLoadText!.Text = loadText;
        _summaryLineText!.Text = lineText;

        _summaryTitleText!.ToolTip = profileName;
        _summaryEndpointText!.ToolTip = endpoint;
        _summaryStatusText!.ToolTip = statusText;
        _summaryBatteryText!.ToolTip = batteryText;
        _summaryLoadText!.ToolTip = loadText;
        _summaryLineText!.ToolTip = lineText;
    }

    private void UpdateTraySummaryText()
    {
        if (_summaryTitleText == null) return;
        _summaryStatusText!.Text = $"Status: {_lastStatusOnly}";
    }

    private static MediaBrush GetBrush(string key, MediaBrush fallback)
    {
        if (Application.Current?.Resources.Contains(key) == true)
        {
            if (Application.Current.Resources[key] is MediaBrush brush)
                return brush;
        }

        return fallback;
    }

    private static UIElement CreateMenuIcon(string pathData, MediaBrush? brush = null)
    {
        var fg = brush ?? GetBrush("TextSecondaryBrush", System.Windows.Media.Brushes.LightGray);
        var path = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(pathData),
            Fill = fg,
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 8, 0)
        };
        return path;
    }

    private static void UpdateAutoStartIcon(MenuItem item, bool enabled)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        if (enabled)
        {
            // Checkmark icon
            panel.Children.Add(CreateMenuIcon("M3,12 L9,18 L21,6", GetBrush("AccentBrush", System.Windows.Media.Brushes.DeepSkyBlue)));
        }
        else
        {
            // Empty checkbox icon
            panel.Children.Add(CreateMenuIcon("M3,3 L21,3 L21,21 L3,21 Z", GetBrush("TextMutedBrush", System.Windows.Media.Brushes.Gray)));
        }
        panel.Children.Add(new TextBlock { Text = "Iniciar com o Windows", VerticalAlignment = VerticalAlignment.Center });
        item.Header = panel;
    }

    private void ApplyDarkMenuStyle(ContextMenu menu)
    {
        try
        {
            var bg = GetBrush("BgBaseBrush", System.Windows.Media.Brushes.Black);
            var fg = GetBrush("TextPrimaryBrush", System.Windows.Media.Brushes.White);
            var fgMuted = GetBrush("TextSecondaryBrush", System.Windows.Media.Brushes.LightGray);
            var hover = GetBrush("BgHoverBrush", System.Windows.Media.Brushes.DimGray);
            var border = GetBrush("BorderSubtleBrush", System.Windows.Media.Brushes.Gray);

            menu.Background = bg;
            menu.BorderBrush = border;
            menu.Foreground = fg;

            var fontFamily = Application.Current?.Resources["AppFont"] as System.Windows.Media.FontFamily
                ?? new System.Windows.Media.FontFamily("Segoe UI");

            var itemStyle = new Style(typeof(MenuItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, bg));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, fg));
            itemStyle.Setters.Add(new Setter(Control.FontFamilyProperty, fontFamily));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 6, 8, 6)));
            itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, hover));
            itemStyle.Triggers.Add(hoverTrigger);

            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5));
            disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, fgMuted));
            itemStyle.Triggers.Add(disabledTrigger);

            var separatorStyle = new Style(typeof(Separator));
            separatorStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(8, 4, 8, 4)));
            separatorStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, 1.0));
            separatorStyle.Setters.Add(new Setter(Separator.BackgroundProperty, border));

            menu.Resources[typeof(MenuItem)] = itemStyle;
            menu.Resources[typeof(Separator)] = separatorStyle;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "ApplyDarkMenuStyle");
        }
    }

    private static ControlTemplate BuildSummaryMenuItemTemplate()
    {
        var template = new ControlTemplate(typeof(MenuItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

        border.AppendChild(presenter);
        template.VisualTree = border;

        return template;
    }

    private static string BuildStatusSummary(Dictionary<string, string> map)
    {
        var status = map.GetValueOrDefault("STATUS", "--");
        var charge = map.GetValueOrDefault("BCHARGE", "--");
        var load = map.GetValueOrDefault("LOADPCT", "--");
        var linev = map.GetValueOrDefault("LINEV", "--");

        return $"Status: {status} | Bat: {charge} | Carga: {load} | Entrada: {linev}";
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
        var balloonIcon = _lastTrayState switch
        {
            "commlost" => BalloonIcon.Error,
            "commfault" => BalloonIcon.Error,
            "overload" => BalloonIcon.Error,
            "lowbatt" => BalloonIcon.Warning,
            "onbatt" => BalloonIcon.Warning,
            "alert" => BalloonIcon.Warning,
            _ => BalloonIcon.Info
        };

        _icon.ShowBalloonTip("APC UPS", text, balloonIcon);
    }

    private static readonly Dictionary<string, System.Drawing.Icon> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    private static System.Drawing.Icon CreateStateIcon(string state)
    {
        if (_iconCache.TryGetValue(state, out var cached))
            return cached;

        var size = 32;
        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var baseColor = state switch
        {
            "online" => System.Drawing.Color.FromArgb(16, 185, 129),
            "onbatt" => System.Drawing.Color.FromArgb(245, 158, 11),
            "commlost" => System.Drawing.Color.FromArgb(239, 68, 68),
            "lowbatt" => System.Drawing.Color.FromArgb(249, 115, 22),
            "overload" => System.Drawing.Color.FromArgb(220, 38, 38),
            "commfault" => System.Drawing.Color.FromArgb(107, 114, 128),
            "alert" => System.Drawing.Color.FromArgb(234, 179, 8),
            _ => System.Drawing.Color.FromArgb(59, 130, 246)
        };

        var lightColor = Lighten(baseColor, 0.25f);

        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new System.Drawing.Rectangle(0, 0, size, size),
            lightColor,
            baseColor,
            System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);

        using var borderPen = new System.Drawing.Pen(Darken(baseColor, 0.25f), 1.5f);

        var rect = new RectangleF(2, 2, size - 4, size - 4);
        using var path = RoundedRect(rect, 7f);
        g.FillPath(brush, path);
        g.DrawPath(borderPen, path);

        // Bolt icon
        using var boltBrush = new SolidBrush(System.Drawing.Color.White);
        var bolt = new PointF[]
        {
            new(15, 6), new(8, 18), new(14, 18), new(11, 27), new(24, 13), new(17, 13)
        };
        g.FillPolygon(boltBrush, bolt);

        if (state is "commlost" or "lowbatt" or "overload" or "commfault" or "alert")
        {
            using var badgeBrush = new SolidBrush(System.Drawing.Color.White);
            g.FillEllipse(badgeBrush, 20, 3, 9, 9);
            using var exBrush = new SolidBrush(System.Drawing.Color.FromArgb(64, 64, 64));
            g.FillRectangle(exBrush, 24, 5, 1.5f, 4.5f);
            g.FillEllipse(exBrush, 24, 10.5f, 1.5f, 1.5f);
        }

        var hIcon = bmp.GetHicon();
        var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);

        _iconCache[state] = icon;
        return icon;
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static System.Drawing.Color Lighten(System.Drawing.Color color, float amount)
        => System.Drawing.Color.FromArgb(color.A,
            (int)Math.Min(255, color.R + 255 * amount),
            (int)Math.Min(255, color.G + 255 * amount),
            (int)Math.Min(255, color.B + 255 * amount));

    private static System.Drawing.Color Darken(System.Drawing.Color color, float amount)
        => System.Drawing.Color.FromArgb(color.A,
            (int)Math.Max(0, color.R - 255 * amount),
            (int)Math.Max(0, color.G - 255 * amount),
            (int)Math.Max(0, color.B - 255 * amount));

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
    
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
