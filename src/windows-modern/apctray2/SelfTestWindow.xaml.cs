using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace apctray2;

public partial class SelfTestWindow : Window
{
    private NisClient? _client;

    public SelfTestWindow(NisClient? client = null)
    {
        try
        {
            SimpleLogger.Info("SelfTestWindow: constructor starting");
            InitializeComponent();
            _client = client;
            DataContext = this;
            SimpleLogger.Info("SelfTestWindow: constructor completed");
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "SelfTestWindow.Constructor");
            throw;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SimpleLogger.Info("SelfTestWindow.Window_Loaded: starting");
            
            if (_client == null)
            {
                try
                {
                    SimpleLogger.Info("SelfTestWindow.Window_Loaded: creating NisClient from settings");
                    var activeProfile = Settings.Current.ProfileManager.GetActiveProfile();
                    if (activeProfile != null)
                    {
                        _client = new NisClient(activeProfile.Host, activeProfile.Port);
                        SimpleLogger.Info($"SelfTestWindow.Window_Loaded: using active profile {activeProfile.Name} @ {activeProfile.Host}:{activeProfile.Port}");
                    }
                    else
                    {
                        _client = new NisClient(Settings.Current.Host, Settings.Current.Port);
                        SimpleLogger.Info($"SelfTestWindow.Window_Loaded: using default settings @ {Settings.Current.Host}:{Settings.Current.Port}");
                    }
                }
                catch (Exception profileEx)
                {
                    SimpleLogger.Error(profileEx, "SelfTestWindow.Window_Loaded: error creating NisClient");
                    ConnectivityResult.Text = $"❌ Erro ao criar cliente NIS: {profileEx.Message}";
                    return;
                }
            }

            SimpleLogger.Info("SelfTestWindow.Window_Loaded: calling RunTests");
            try
            {
                await RunTests();
            }
            catch (Exception testEx)
            {
                SimpleLogger.Error(testEx, "SelfTestWindow.Window_Loaded: RunTests failed");
                StatusText.Text = $"Erro during tests: {testEx.Message}";
            }
            SimpleLogger.Info("SelfTestWindow.Window_Loaded: RunTests completed");
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "SelfTestWindow.Window_Loaded");
            StatusText.Text = $"Erro ao carregar: {ex.Message}";
            ConnectivityResult.Text = "❌ Falha na inicialização";
        }
    }

    private async System.Threading.Tasks.Task RunTests()
    {
        try
        {
            StatusText.Text = "Executando testes de conectividade e sistema...";
            SimpleLogger.Info("SelfTestWindow.RunTests: starting");
            
            // Teste 1: Conectividade
            SimpleLogger.Info("SelfTestWindow.RunTests: TestConnectivity");
            await TestConnectivity();

            // Teste 2: Status do UPS
            SimpleLogger.Info("SelfTestWindow.RunTests: TestUpsStatus");
            await TestUpsStatus();

            // Teste 3: Bateria
            SimpleLogger.Info("SelfTestWindow.RunTests: TestBattery");
            await TestBattery();

            // Teste 4: Energia
            SimpleLogger.Info("SelfTestWindow.RunTests: TestPower");
            await TestPower();

            // Resumo final
            SimpleLogger.Info("SelfTestWindow.RunTests: UpdateSummary");
            UpdateSummary();
            
            SimpleLogger.Info("SelfTestWindow.RunTests: completed");
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "SelfTestWindow.RunTests");
            throw;
        }
    }

    private async System.Threading.Tasks.Task TestConnectivity()
    {
        try
        {
            if (_client == null)
            {
                ConnectivityResult.Text = "❌ Cliente NIS não inicializado";
                SimpleLogger.Info("SelfTestWindow.TestConnectivity: _client is null");
                return;
            }

            SimpleLogger.Info($"SelfTestWindow.TestConnectivity: testing connection to {Settings.Current.Host}:{Settings.Current.Port}");
            var result = await _client.TestAsync();
            
            SimpleLogger.Info($"SelfTestWindow.TestConnectivity: result = {result}");
            
            if (result)
            {
                ConnectivityResult.Text = "✅ Conexão estabelecida com sucesso\n" +
                    $"Host: {Settings.Current.Host}:{Settings.Current.Port}\n" +
                    $"Status: Respondendo normalmente";
                ConnectivityResult.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
                SimpleLogger.Info("SelfTestWindow.TestConnectivity: connection successful");
            }
            else
            {
                ConnectivityResult.Text = "❌ Falha na conexão com o NIS\n" +
                    $"Verifique se o servidor está disponível em {Settings.Current.Host}:{Settings.Current.Port}";
                ConnectivityResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                SimpleLogger.Info("SelfTestWindow.TestConnectivity: connection failed");
            }
        }
        catch (Exception ex)
        {
            ConnectivityResult.Text = $"❌ Erro ao testar conectividade:\n{ex.Message}";
            ConnectivityResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            SimpleLogger.Error(ex, "SelfTestWindow.TestConnectivity");
        }
    }

    private async System.Threading.Tasks.Task TestUpsStatus()
    {
        try
        {
            if (_client == null)
            {
                UpsStatusResult.Text = "❌ Cliente NIS não inicializado";
                return;
            }

            SimpleLogger.Info("SelfTestWindow.TestUpsStatus: getting status");
            var status = await _client.GetStatusAsync();

            SimpleLogger.Info($"SelfTestWindow.TestUpsStatus: received {status.Count} keys");

            var statusText = status.TryGetValue("STATUS", out var s) ? s : "Desconhecido";
            var upsName = status.TryGetValue("UPSNAME", out var n) ? n : "N/A";
            var model = status.TryGetValue("MODEL", out var m) ? m : "N/A";

            SimpleLogger.Info($"SelfTestWindow.TestUpsStatus: STATUS={statusText}, NAME={upsName}, MODEL={model}");

            var isOnline = statusText.Contains("ONLINE", StringComparison.OrdinalIgnoreCase);
            var isOnBattery = statusText.Contains("ONBATT", StringComparison.OrdinalIgnoreCase);
            var isCommLost = statusText.Contains("COMMLOST", StringComparison.OrdinalIgnoreCase);

            if (isCommLost)
            {
                UpsStatusResult.Text = $"⚠️ {upsName}\n" +
                    $"Status: SEM COMUNICAÇÃO\n" +
                    $"Modelo: {model}\n" +
                    $"⚠️ Atenção: O UPS está sem comunicação!";
                UpsStatusResult.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange
            }
            else if (isOnBattery)
            {
                UpsStatusResult.Text = $"🔋 {upsName}\n" +
                    $"Status: OPERANDO EM BATERIA\n" +
                    $"Modelo: {model}\n" +
                    $"⚠️ O UPS está em modo bateria!";
                UpsStatusResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            }
            else if (isOnline)
            {
                UpsStatusResult.Text = $"✅ {upsName}\n" +
                    $"Status: ONLINE\n" +
                    $"Modelo: {model}\n" +
                    $"Operando normalmente com energia da rede.";
                UpsStatusResult.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
            }
            else
            {
                UpsStatusResult.Text = $"ℹ️ {upsName}\n" +
                    $"Status: {statusText}\n" +
                    $"Modelo: {model}";
                UpsStatusResult.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
            }
        }
        catch (Exception ex)
        {
            UpsStatusResult.Text = $"❌ Erro ao obter status:\n{ex.Message}";
            UpsStatusResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            SimpleLogger.Error(ex, "SelfTestWindow.TestUpsStatus");
        }
    }

    private async System.Threading.Tasks.Task TestBattery()
    {
        try
        {
            if (_client == null)
            {
                BatteryResult.Text = "❌ Cliente NIS não inicializado";
                return;
            }

            var status = await _client.GetStatusAsync();

            var batteryPercent = "N/A";
            var batteryStatus = "Desconhecido";
            var timeLeft = "N/A";

            if (status.TryGetValue("BCHARGE", out var bc))
            {
                var parts = bc.Split(new[] { ' ', '%', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && double.TryParse(parts[0], out var bp))
                {
                    batteryPercent = $"{(int)bp}%";
                }
            }

            if (status.TryGetValue("TIMELEFT", out var tl))
            {
                var parts = tl.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    timeLeft = $"{parts[0]} min";
            }

            if (status.TryGetValue("STATUS", out var s) && s.Contains("LOWBATT", StringComparison.OrdinalIgnoreCase))
                batteryStatus = "BATERIA BAIXA";
            else if (status.TryGetValue("STATUS", out var s2) && s2.Contains("REPLACEBATT", StringComparison.OrdinalIgnoreCase))
                batteryStatus = "BATERIA REQUER SUBSTITUIÇÃO";
            else
                batteryStatus = "Normal";

            var batteryChargeValue = int.Parse(batteryPercent.Replace("%", ""));
            var batteryOk = batteryChargeValue > 20;

            BatteryResult.Text = $"{(batteryOk ? "✅" : "⚠️")} Carga: {batteryPercent}\n" +
                $"Status: {batteryStatus}\n" +
                $"Autonomia: {timeLeft}";
            BatteryResult.Foreground = batteryOk ? 
                new SolidColorBrush(Color.FromRgb(34, 197, 94)) : 
                new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Green or Orange
        }
        catch (Exception ex)
        {
            BatteryResult.Text = $"❌ Erro ao testar bateria:\n{ex.Message}";
            BatteryResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            SimpleLogger.Error(ex, "SelfTestWindow.TestBattery");
        }
    }

    private async System.Threading.Tasks.Task TestPower()
    {
        try
        {
            if (_client == null)
            {
                PowerResult.Text = "❌ Cliente NIS não inicializado";
                return;
            }

            var status = await _client.GetStatusAsync();

            var inputVoltage = "N/A";
            var outputVoltage = "N/A";
            var loadPercent = "N/A";
            var lineFreq = "N/A";
            var temperature = "N/A";

            if (status.TryGetValue("LINEV", out var lv))
                inputVoltage = lv.Contains("V") ? lv : $"{lv} V";

            if (status.TryGetValue("OUTPUTV", out var ov))
                outputVoltage = ov.Contains("V") ? ov : $"{ov} V";

            if (status.TryGetValue("LOADPCT", out var lp))
                loadPercent = lp.Contains("%") ? lp : $"{lp}%";

            if (status.TryGetValue("LINEFREQ", out var lf))
                lineFreq = lf.Contains("Hz") ? lf : $"{lf} Hz";

            if (status.TryGetValue("ITEMP", out var temp))
                temperature = temp.Contains("C") ? temp : $"{temp} °C";

            var voltageOk = !inputVoltage.Contains("N/A");

            PowerResult.Text = $"{(voltageOk ? "✅" : "⚠️")} Tensão entrada: {inputVoltage}\n" +
                $"Tensão saída: {outputVoltage}\n" +
                $"Carga: {loadPercent}\n" +
                $"Frequência: {lineFreq}\n" +
                $"Temperatura: {temperature}";
            PowerResult.Foreground = voltageOk ? 
                new SolidColorBrush(Color.FromRgb(34, 197, 94)) : 
                new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Green or Red
        }
        catch (Exception ex)
        {
            PowerResult.Text = $"❌ Erro ao testar energia:\n{ex.Message}";
            PowerResult.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            SimpleLogger.Error(ex, "SelfTestWindow.TestPower");
        }
    }

    private void UpdateSummary()
    {
        try
        {
            var connectOk = ConnectivityResult.Text.StartsWith("✅");
            var statusOk = UpsStatusResult.Text.StartsWith("✅");
            var batteryOk = BatteryResult.Text.StartsWith("✅");
            var powerOk = PowerResult.Text.StartsWith("✅");

            var allOk = connectOk && statusOk && batteryOk && powerOk;

            var summary = allOk
                ? "✅ TODOS OS TESTES PASSARAM\n\nO UPS está operando normalmente sem problemas detectados."
                : "⚠️ ALGUNS TESTES COM ADVERTÊNCIA\n\nVerifique os detalhes acima para mais informações.";

            SummaryResult.Text = summary;
            SummaryResult.Foreground = allOk ? 
                new SolidColorBrush(Color.FromRgb(34, 197, 94)) : 
                new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Green or Orange

            StatusText.Text = $"Testes concluídos em {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            SimpleLogger.Error(ex, "SelfTestWindow.UpdateSummary");
        }
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        ConnectivityResult.Text = "Testando...";
        UpsStatusResult.Text = "Testando...";
        BatteryResult.Text = "Testando...";
        PowerResult.Text = "Testando...";
        SummaryResult.Text = "Aguardando resultados...";

        await RunTests();
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
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
}
