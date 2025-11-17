using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace apctray2;

public partial class AdvancedWindow : Window, INotifyPropertyChanged
{
    private readonly NisClient _client;
    private readonly DispatcherTimer _updateTimer;
    
    // Data series para gráficos
    private readonly ObservableCollection<double> _batteryData = new();
    private readonly ObservableCollection<double> _loadData = new();
    private readonly ObservableCollection<double> _voltageData = new();
    private readonly ObservableCollection<double> _wattsData = new();
    
    // Todas as amostras de métricas (range completo)
    private readonly ObservableCollection<double> _allBatteryData = new();
    private readonly ObservableCollection<double> _allLoadData = new();
    private readonly ObservableCollection<double> _allVoltageData = new();
    private readonly ObservableCollection<double> _allWattsData = new();
    
    private const int MaxDataPoints = 60; // 1 minuto a 1seg/ponto
    private TimeSpan _currentRange = TimeSpan.FromHours(1); // Default 1 hora

    // Monitoramento de tensão e bateria
    private const double MinVoltage = 190.0;
    private const double MaxVoltage = 240.0;
    private bool _wasOnBattery = false;
    private DateTime? _batteryModeStartTime = null;
    private TimeSpan _totalBatteryTime = TimeSpan.Zero;
    private double _lastBatteryPercent = 100;
    private DateTime? _lastChargeTime = null;
    private bool _chargingAfterBattery = false;
    private DateTime? _chargeStartTime = null;

    public ObservableCollection<string> LogEntries { get; } = new();

    // Propriedades vinculadas
    private string _upsName = "<unknown>";
    public string UpsName { get => _upsName; set { _upsName = value; OnPropertyChanged(nameof(UpsName)); } }

    private string _status = "";
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
            UpdateModeIndicator();
        }
    }

    private string _timeLeft = "--";
    public string TimeLeft { get => _timeLeft; set { _timeLeft = value; OnPropertyChanged(nameof(TimeLeft)); } }

    private int _batteryPercent;
    public int BatteryPercent { get => _batteryPercent; set { _batteryPercent = value; OnPropertyChanged(nameof(BatteryPercent)); } }

    private int _loadPercent;
    public int LoadPercent { get => _loadPercent; set { _loadPercent = value; OnPropertyChanged(nameof(LoadPercent)); } }

    private string _loadWatts = "--";
    public string LoadWatts { get => _loadWatts; set { _loadWatts = value; OnPropertyChanged(nameof(LoadWatts)); } }

    private string _inputVoltage = "--";
    public string InputVoltage { get => _inputVoltage; set { _inputVoltage = value; OnPropertyChanged(nameof(InputVoltage)); } }

    private string _outputVoltage = "--";
    public string OutputVoltage { get => _outputVoltage; set { _outputVoltage = value; OnPropertyChanged(nameof(OutputVoltage)); } }

    private string _lineFreq = "--";
    public string LineFreq { get => _lineFreq; set { _lineFreq = value; OnPropertyChanged(nameof(LineFreq)); } }

    private string _temperature = "--";
    public string Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(nameof(Temperature)); } }

    private string _voltageStatus = "";
    public string VoltageStatus { get => _voltageStatus; set { _voltageStatus = value; OnPropertyChanged(nameof(VoltageStatus)); } }

    private string _batteryTimeInfo = "N/A";
    public string BatteryTimeInfo { get => _batteryTimeInfo; set { _batteryTimeInfo = value; OnPropertyChanged(nameof(BatteryTimeInfo)); } }

    private string _chargeEstimate = "N/A";
    public string ChargeEstimate { get => _chargeEstimate; set { _chargeEstimate = value; OnPropertyChanged(nameof(ChargeEstimate)); } }

    private string _lastFullChargeInfo = "";
    public string LastFullChargeInfo { get => _lastFullChargeInfo; set { _lastFullChargeInfo = value; OnPropertyChanged(nameof(LastFullChargeInfo)); } }

    // Saúde da bateria
    private string _batteryHealthText = "--";
    public string BatteryHealthText { get => _batteryHealthText; set { _batteryHealthText = value; OnPropertyChanged(nameof(BatteryHealthText)); } }

    private string _batteryHealthDetails = "";
    public string BatteryHealthDetails { get => _batteryHealthDetails; set { _batteryHealthDetails = value; OnPropertyChanged(nameof(BatteryHealthDetails)); } }

    private Brush _batteryHealthBrush = Brushes.White;
    public Brush BatteryHealthBrush { get => _batteryHealthBrush; set { _batteryHealthBrush = value; OnPropertyChanged(nameof(BatteryHealthBrush)); } }

    public AdvancedWindow()
    {
        _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
        InitializeComponent();
        DataContext = this;
        
        SetupCharts();
        
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += async (s, e) => await UpdateDataAsync();
        
        // Adiar carregamento de dados para depois da janela abrir
        Loaded += async (s, e) =>
        {
            _updateTimer.Start();
            await UpdateDataAsync();
            await LoadLogsAsync();
        };
    }

    private void SetupCharts()
    {
        // Gráfico de bateria
        BatteryChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _batteryData,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };
        
        // Gráfico de carga (% e Watts)
        LoadChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Carga %",
                Values = _loadData,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5,
                ScalesYAt = 0
            },
            new LineSeries<double>
            {
                Name = "Watts",
                Values = _wattsData,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Yellow) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5,
                ScalesYAt = 1
            }
        };
        
        // Configurar dois eixos Y para o gráfico de carga
        LoadChart.YAxes = new Axis[]
        {
            new Axis // Eixo esquerdo: %
            {
                Name = "%",
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                LabelsPaint = new SolidColorPaint(SKColors.Orange),
                SeparatorsPaint = new SolidColorPaint(SKColors.DarkGray) { StrokeThickness = 1 }
            },
            new Axis // Eixo direito: Watts
            {
                Name = "W",
                Position = LiveChartsCore.Measure.AxisPosition.End,
                LabelsPaint = new SolidColorPaint(SKColors.Yellow),
                ShowSeparatorLines = false
            }
        };
        
        // Gráfico de tensão com linha de referência 127V
        VoltageChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _voltageData,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };
        
        // Adicionar linha de referência 127V
        VoltageChart.Sections = new RectangularSection[]
        {
            new RectangularSection
            {
                Yi = 127.0,
                Yj = 127.0,
                Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 }
            }
        };

        // Gráfico de consumo (Watts)
        WattsChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _wattsData,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Gold) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        // Configurar eixos escuros
        var darkAxis = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.DarkGray) { StrokeThickness = 1 }
            }
        };
        
        BatteryChart.YAxes = darkAxis;
        VoltageChart.YAxes = darkAxis;
        WattsChart.YAxes = darkAxis;
    }

    private async System.Threading.Tasks.Task UpdateDataAsync()
    {
        try
        {
            var map = await _client.GetStatusAsync();
            
            if (map.TryGetValue("UPSNAME", out var name)) UpsName = name;
            if (map.TryGetValue("STATUS", out var st)) Status = st;
            if (map.TryGetValue("TIMELEFT", out var tl))
            {
                // TIMELEFT vem em minutos, ex: "45.0 Minutes"
                var parts = tl.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var minutes))
                {
                    TimeLeft = $"{minutes:F0} min";
                }
                else
                {
                    TimeLeft = tl.Contains(" ") ? tl.Split(' ')[0] : tl;
                }
            }
            
            if (map.TryGetValue("BCHARGE", out var bc))
            {
                var parts = bc.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var bpd))
                {
                    BatteryPercent = (int)bpd;
                    _allBatteryData.Add(bpd);
                }
            }
            
            if (map.TryGetValue("LOADPCT", out var lp))
            {
                var parts = lp.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var lpd))
                {
                    LoadPercent = (int)lpd;
                    _allLoadData.Add(lpd);
                }
            }
            
            if (map.TryGetValue("NOMPOWER", out var nomPower) && map.TryGetValue("LOADPCT", out var loadPct))
            {
                var nomParts = nomPower.Split(new[] { ' ', 'W', 'a', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                var loadParts = loadPct.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (nomParts.Length > 0 && double.TryParse(nomParts[0], out var nomW) &&
                    loadParts.Length > 0 && double.TryParse(loadParts[0], out var loadP))
                {
                    var watts = nomW * loadP / 100.0;
                    LoadWatts = $"{(int)watts} W";
                    _allWattsData.Add(watts);
                }
            }
            
            if (map.TryGetValue("LINEV", out var linev))
            {
                var parts = linev.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    InputVoltage = $"{v:F1} V";
                    _allVoltageData.Add(v);
                    
                    // Detectar oscilação de tensão
                    if (v < MinVoltage)
                    {
                        VoltageStatus = $"⚠️ SUB-TENSÃO ({v:F1}V < {MinVoltage}V)";
                        AddLogEntry($"⚠️ Sub-tensão detectada: {v:F1}V");
                    }
                    else if (v > MaxVoltage)
                    {
                        VoltageStatus = $"⚠️ SOBRE-TENSÃO ({v:F1}V > {MaxVoltage}V)";
                        AddLogEntry($"⚠️ Sobre-tensão detectada: {v:F1}V");
                    }
                    else
                    {
                        VoltageStatus = $"✓ Normal ({v:F1}V)";
                    }
                }
                else
                {
                    InputVoltage = linev;
                }
            }
            
            // Aplicar filtro de range após todos os dados
            ApplyRangeFilter();
            
            if (map.TryGetValue("OUTPUTV", out var outputv))
            {
                var parts = outputv.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    OutputVoltage = $"{v:F1} V"; // Formato xxx,x V
                }
                else
                {
                    OutputVoltage = outputv;
                }
            }
            
            if (map.TryGetValue("LINEFREQ", out var freq))
                LineFreq = freq.Contains("Hz") ? freq : $"{freq} Hz";
            
            if (map.TryGetValue("ITEMP", out var temp))
                Temperature = temp.Contains("C") ? temp : $"{temp} °C";

            // Saúde da bateria baseada em capacidade estimada e nominal
            UpdateBatteryHealth();
            
            // Rastrear tempo em modo bateria
            bool isOnBattery = Status.Contains("ONBATT", StringComparison.OrdinalIgnoreCase);
            if (isOnBattery && !_wasOnBattery)
            {
                // Entrou em modo bateria
                _batteryModeStartTime = DateTime.Now;
                _chargingAfterBattery = false;
                _chargeStartTime = null;
                AddLogEntry("🔋 Entrando em modo bateria");
            }
            else if (!isOnBattery && _wasOnBattery && _batteryModeStartTime.HasValue)
            {
                // Saiu do modo bateria
                var duration = DateTime.Now - _batteryModeStartTime.Value;
                _totalBatteryTime += duration;
                _batteryModeStartTime = null;
                _chargingAfterBattery = true;
                _chargeStartTime = DateTime.Now;
                AddLogEntry($"🔌 Retornando ao modo rede (tempo em bateria: {duration.TotalMinutes:F1} min)");
            }
            
            // Atualizar informação de tempo em bateria
            if (_batteryModeStartTime.HasValue)
            {
                var currentDuration = DateTime.Now - _batteryModeStartTime.Value;
                BatteryTimeInfo = $"Atual: {currentDuration.TotalMinutes:F1} min | Total: {_totalBatteryTime.TotalMinutes:F1} min";
            }
            else
            {
                BatteryTimeInfo = _totalBatteryTime.TotalMinutes > 0 
                    ? $"Total acumulado: {_totalBatteryTime.TotalMinutes:F1} min" 
                    : "N/A";
            }
            _wasOnBattery = isOnBattery;
            
            // Estimar tempo de recarga
            if (map.TryGetValue("BCHARGE", out var bcharge))
            {
                var parts = bcharge.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var currentCharge))
                {
                    if (_lastChargeTime.HasValue && currentCharge > _lastBatteryPercent)
                    {
                        // Calculando taxa de recarga
                        var timeDiff = (DateTime.Now - _lastChargeTime.Value).TotalHours;
                        var chargeDiff = currentCharge - _lastBatteryPercent;
                        
                        if (timeDiff > 0 && chargeDiff > 0)
                        {
                            var chargeRate = chargeDiff / timeDiff; // % por hora
                            var remainingCharge = 100 - currentCharge;
                            var estimatedHours = remainingCharge / chargeRate;
                            
                            ChargeEstimate = estimatedHours < 24 
                                ? $"{estimatedHours:F1} horas restantes" 
                                : $"{(estimatedHours / 24):F1} dias restantes";
                        }
                    }
                    
                    _lastBatteryPercent = currentCharge;
                    _lastChargeTime = DateTime.Now;
                    
                    if (currentCharge >= 99.9)
                    {
                        ChargeEstimate = "✓ Carga completa";
                        if (_chargingAfterBattery && _chargeStartTime.HasValue)
                        {
                            var elapsed = DateTime.Now - _chargeStartTime.Value;
                            var msg = $"Bateria 100% carregada em {elapsed.TotalMinutes:F1} minutos";
                            LastFullChargeInfo = msg;
                            AddLogEntry($"✅ {msg}");
                            _chargingAfterBattery = false;
                            _chargeStartTime = null;
                        }
                    }
                    else if (!Status.Contains("ONLINE", StringComparison.OrdinalIgnoreCase))
                    {
                        ChargeEstimate = "Aguardando modo rede...";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AddLogEntry($"❌ Erro: {ex.Message}");
        }
    }

    private void UpdateBatteryHealth()
    {
        var estAh = Settings.Current.EstimatedCapacityAh;
        var nomAh = Settings.Current.BatteryNominalCapacityAh;
        var cycles = Settings.Current.CycleCount;
        var replacedEpoch = Settings.Current.BatteryReplacedEpoch;

        if (estAh <= 0 || nomAh <= 0)
        {
            BatteryHealthText = "Sem dados suficientes";
            BatteryHealthDetails = "Execute alguns ciclos completos para estimar a capacidade.";
            BatteryHealthBrush = Brushes.Gray;
            return;
        }

        var health = Math.Max(0, Math.Min(100, (int)Math.Round(estAh / nomAh * 100.0)));
        BatteryHealthText = $"{health}%";

        // Cor conforme faixas
        if (health < 60)
            BatteryHealthBrush = Brushes.Red;
        else if (health < 80)
            BatteryHealthBrush = Brushes.Gold;
        else
            BatteryHealthBrush = Brushes.LimeGreen;

        // Detalhes: ciclos e idade
        string ageText = "";
        if (replacedEpoch > 0)
        {
            var replaced = DateTimeOffset.FromUnixTimeSeconds((long)replacedEpoch).DateTime;
            var age = DateTime.Now - replaced;
            var years = (int)(age.TotalDays / 365);
            var months = (int)((age.TotalDays % 365) / 30);
            ageText = years > 0
                ? $", idade: {years}a {months}m"
                : $", idade: {months}m";
        }

        BatteryHealthDetails = $"Ciclos: {cycles}{ageText}";
    }

    private void AddDataPoint(ObservableCollection<double> series, double value)
    {
        series.Add(value);
        if (series.Count > MaxDataPoints)
            series.RemoveAt(0);
    }
    
    private void ApplyRangeFilter()
    {
        var startIndex = Math.Max(0, _allBatteryData.Count - (int)(_currentRange.TotalSeconds / Settings.Current.RefreshSeconds));
        
        _batteryData.Clear();
        _loadData.Clear();
        _voltageData.Clear();
        _wattsData.Clear();
        
        foreach (var val in _allBatteryData.Skip(startIndex)) _batteryData.Add(val);
        foreach (var val in _allLoadData.Skip(startIndex)) _loadData.Add(val);
        foreach (var val in _allVoltageData.Skip(startIndex)) _voltageData.Add(val);
        foreach (var val in _allWattsData.Skip(startIndex)) _wattsData.Add(val);
    }
    
    private void RangeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RangeComboBox == null) return;
        
        switch (RangeComboBox.SelectedIndex)
        {
            case 0: _currentRange = TimeSpan.FromHours(1); break;
            case 1: _currentRange = TimeSpan.FromHours(6); break;
            case 2: _currentRange = TimeSpan.FromHours(24); break;
            case 3: // Personalizado
                StartDatePicker.Visibility = Visibility.Visible;
                DateSeparator.Visibility = Visibility.Visible;
                EndDatePicker.Visibility = Visibility.Visible;
                return;
        }
        
        StartDatePicker.Visibility = Visibility.Collapsed;
        DateSeparator.Visibility = Visibility.Collapsed;
        EndDatePicker.Visibility = Visibility.Collapsed;
        
        ApplyRangeFilter();
    }
    
    private void CustomRange_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
        {
            _currentRange = EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value;
            ApplyRangeFilter();
        }
    }

    private void UpdateModeIndicator()
    {
        if (Status.Contains("ONBATT", StringComparison.OrdinalIgnoreCase))
        {
            ModeIndicator.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Vermelho
            ModeIcon.Text = "🔋";
            ModeText.Text = "MODO BATERIA";
        }
        else if (Status.Contains("ONLINE", StringComparison.OrdinalIgnoreCase))
        {
            ModeIndicator.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Verde
            ModeIcon.Text = "🔌";
            ModeText.Text = "MODO REDE";
        }
        else if (Status.Contains("COMMLOST", StringComparison.OrdinalIgnoreCase))
        {
            ModeIndicator.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Laranja
            ModeIcon.Text = "⚠️";
            ModeText.Text = "SEM COMUNICAÇÃO";
        }
    }

    private async System.Threading.Tasks.Task LoadLogsAsync()
    {
        try
        {
            var events = await _client.GetEventsAsync();
            foreach (var evt in events.TakeLast(100))
            {
                AddLogEntry(evt);
            }
        }
        catch (Exception ex)
        {
            AddLogEntry($"❌ Erro ao carregar logs: {ex.Message}");
        }
    }

    private void AddLogEntry(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogEntries.Add($"[{timestamp}] {message}");
        
        if (LogEntries.Count > 500)
            LogEntries.RemoveAt(0);
        
        LogList.ItemsSource = LogEntries;
        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[^1]);
    }

    private async void RefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        LogEntries.Clear();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer?.Stop();
        base.OnClosed(e);
    }
}
