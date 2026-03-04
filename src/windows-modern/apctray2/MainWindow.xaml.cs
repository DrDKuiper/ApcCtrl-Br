using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace apctray2;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private NisClient _client;
    private bool _isUpdatingProfileSelection;

    public ObservableCollection<UpsProfile> AvailableProfiles { get; } = new();

    private UpsProfile? _selectedProfile;
    public UpsProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            _selectedProfile = value;
            OnPropertyChanged(nameof(SelectedProfile));

            if (_isUpdatingProfileSelection || value == null)
                return;

            Settings.Current.ProfileManager.SetActiveProfile(value.Id);
            Settings.Current.Host = value.Host;
            Settings.Current.Port = value.Port;
            Settings.Current.Save();
            TrayIcon.Current?.RefreshClientFromSettings();
            _ = RefreshAsync();
        }
    }

    private string _upsName = "<unknown>";
    public string UpsName { get => _upsName; set { _upsName = value; OnPropertyChanged(nameof(UpsName)); } }

    private string _status = "";
    public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

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

    // Saúde da bateria (tray principal)
    private string _batteryHealthText = "--";
    public string BatteryHealthText { get => _batteryHealthText; set { _batteryHealthText = value; OnPropertyChanged(nameof(BatteryHealthText)); } }

    private string _batteryHealthDetails = "";
    public string BatteryHealthDetails { get => _batteryHealthDetails; set { _batteryHealthDetails = value; OnPropertyChanged(nameof(BatteryHealthDetails)); } }

    private bool _isDarkMode = true;
    public bool IsDarkMode 
    { 
        get => _isDarkMode; 
        set 
        { 
            _isDarkMode = value; 
            OnPropertyChanged(nameof(IsDarkMode));
            ApplyTheme();
        } 
    }

    public MainWindow()
    {
        var active = Settings.Current.ProfileManager.GetActiveProfile();
        _client = active != null
            ? new NisClient(active.Host, active.Port)
            : new NisClient(Settings.Current.Host, Settings.Current.Port);

        InitializeComponent();
        DataContext = this;
        ReloadProfiles();
        ApplyTheme();
        _ = RefreshAsync();
    }

    public void ReloadProfiles()
    {
        _isUpdatingProfileSelection = true;
        AvailableProfiles.Clear();

        foreach (var profile in Settings.Current.ProfileManager.Profiles)
            AvailableProfiles.Add(profile);

        SelectedProfile = Settings.Current.ProfileManager.GetActiveProfile();
        _isUpdatingProfileSelection = false;
    }

    public void RefreshClientFromSettings()
    {
        var active = Settings.Current.ProfileManager.GetActiveProfile();
        if (active != null)
        {
            _client = new NisClient(active.Host, active.Port);
        }
        else
        {
            _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
        }
    }

    private void ApplyTheme()
    {
        // Theme is now fully managed by DarkTheme.xaml resource dictionary
        // The window Background is set to Transparent for the rounded border effect
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        try
        {
            SimpleLogger.Info("MainWindow.RefreshAsync: calling GetStatusAsync");
            var map = await _client.GetStatusAsync();
            SimpleLogger.Info($"MainWindow.RefreshAsync: received {map.Count} keys from NIS");
            
            // Debug: mostrar chaves recebidas
            System.Diagnostics.Debug.WriteLine($"=== NIS Status Keys ({map.Count}) ===");
            foreach (var kvp in map)
            {
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
            
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
            
            // Tentar diferentes variações de chave para bateria
            if (map.TryGetValue("BCHARGE", out var bc))
            {
                var parts = bc.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var bpd))
                    BatteryPercent = (int)bpd;
            }
            
            // Tentar diferentes variações de chave para carga
            if (map.TryGetValue("LOADPCT", out var lp))
            {
                var parts = lp.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var lpd))
                    LoadPercent = (int)lpd;
            }
            
            // Calcular watts consumidos se disponível
            if (map.TryGetValue("NOMPOWER", out var nomPower) && map.TryGetValue("LOADPCT", out var loadPct))
            {
                var nomParts = nomPower.Split(new[] { ' ', 'W', 'a', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                var loadParts = loadPct.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (nomParts.Length > 0 && double.TryParse(nomParts[0], out var nomW) &&
                    loadParts.Length > 0 && double.TryParse(loadParts[0], out var loadP))
                {
                    var watts = (int)(nomW * loadP / 100.0);
                    LoadWatts = $"{watts} W";
                }
            }
            
            // Tensão de entrada
            if (map.TryGetValue("LINEV", out var linev))
            {
                var parts = linev.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    InputVoltage = $"{v:F1} V";
                }
                else
                {
                    InputVoltage = linev;
                }
            }
            
            // Tensão de saída
            if (map.TryGetValue("OUTPUTV", out var outputv))
            {
                var parts = outputv.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    OutputVoltage = $"{v:F1} V";
                }
                else
                {
                    OutputVoltage = outputv;
                }
            }
            
            // Frequência
            if (map.TryGetValue("LINEFREQ", out var freq))
            {
                LineFreq = freq.Contains("Hz") ? freq : $"{freq} Hz";
            }
            
            // Temperatura
            if (map.TryGetValue("ITEMP", out var temp))
            {
                Temperature = temp.Contains("C") ? temp : $"{temp} °C";
            }
            // Atualizar saúde da bateria com base nas configurações persistidas
            UpdateBatteryHealth();
        }
        catch (SocketException ex)
        {
            Status = $"Erro de rede: {ex.Message}";
            SimpleLogger.Error(ex, "MainWindow.RefreshAsync network error");
        }
        catch (Exception ex)
        {
            Status = $"Erro: {ex.Message}";
            SimpleLogger.Error(ex, "MainWindow.RefreshAsync generic error");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void SelfTest_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Autoteste: a implementar (Fase 2)", "apctray2");
    }

    // Exposto para menu de tray
    public void SelfTest_Relay() => SelfTest_Click(this, new RoutedEventArgs());

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // Atualiza propriedades a partir de um mapa status NIS
    public void ApplyStatusMap(System.Collections.Generic.Dictionary<string, string> map)
    {
        if (map.TryGetValue("UPSNAME", out var name)) UpsName = name;
        if (map.TryGetValue("STATUS", out var st)) Status = st;
        if (map.TryGetValue("TIMELEFT", out var tl)) 
        {
            TimeLeft = tl.Contains(" ") ? tl.Split(' ')[0] : tl;
        }
        
        // Atualizar EnergyFlowControl apenas se já foi inicializado
        if (EnergyFlow != null)
        {
            try
            {
                EnergyFlow.UpdateStatus(map);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error(ex, "Error updating EnergyFlow control");
            }
        }
        
        if (map.TryGetValue("BCHARGE", out var bc))
        {
            var parts = bc.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], out var bpd))
                BatteryPercent = (int)bpd;
        }
        
        if (map.TryGetValue("LOADPCT", out var lp))
        {
            var parts = lp.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], out var lpd))
                LoadPercent = (int)lpd;
        }
        
        // Calcular watts consumidos
        if (map.TryGetValue("NOMPOWER", out var nomPower) && map.TryGetValue("LOADPCT", out var loadPct))
        {
            var nomParts = nomPower.Split(new[] { ' ', 'W', 'a', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
            var loadParts = loadPct.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (nomParts.Length > 0 && double.TryParse(nomParts[0], out var nomW) &&
                loadParts.Length > 0 && double.TryParse(loadParts[0], out var loadP))
            {
                var watts = (int)(nomW * loadP / 100.0);
                LoadWatts = $"{watts} W";
            }
        }
        
        // Tensão de entrada
        if (map.TryGetValue("LINEV", out var linev2))
        {
            var parts = linev2.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                InputVoltage = $"{v:F1} V";
            }
            else
            {
                InputVoltage = linev2;
            }
        }
        
        // Tensão de saída
        if (map.TryGetValue("OUTPUTV", out var outputv2))
        {
            var parts = outputv2.Split(new[] { ' ', 'V', 'o', 'l', 't', 's' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                OutputVoltage = $"{v:F1} V";
            }
            else
            {
                OutputVoltage = outputv2;
            }
        }
        
        // Frequência
        if (map.TryGetValue("LINEFREQ", out var freq2))
        {
            LineFreq = freq2.Contains("Hz") ? freq2 : $"{freq2} Hz";
        }
        
        // Temperatura
        if (map.TryGetValue("ITEMP", out var temp2))
        {
            Temperature = temp2.Contains("C") ? temp2 : $"{temp2} °C";
        }

        // Atualizar saúde da bateria baseada em capacidade estimada e nominal
        UpdateBatteryHealth();
    }

    private void UpdateBatteryHealth()
    {
        var estAh = Settings.Current.EstimatedCapacityAh;
        var nomAh = Settings.Current.BatteryNominalCapacityAh;
        var cycles = Settings.Current.CycleCount;
        var replacedEpoch = Settings.Current.BatteryReplacedEpoch;

        if (estAh <= 0 || nomAh <= 0)
        {
            BatteryHealthText = "Sem dados";
            BatteryHealthDetails = "Execute alguns ciclos completos para estimar a capacidade.";
            return;
        }

        var health = Math.Max(0, Math.Min(100, (int)Math.Round(estAh / nomAh * 100.0)));
        BatteryHealthText = $"{health}%";

        string details = $"Ciclos: {cycles}";
        if (replacedEpoch > 0)
        {
            var replaced = DateTimeOffset.FromUnixTimeSeconds((long)replacedEpoch).DateTime;
            var age = DateTime.Now - replaced;
            var years = (int)(age.TotalDays / 365);
            var months = (int)((age.TotalDays % 365) / 30);
            var ageText = years > 0
                ? $", idade: {years}a {months}m"
                : $", idade: {months}m";
            details += ageText;
        }

        BatteryHealthDetails = details;
    }
}
