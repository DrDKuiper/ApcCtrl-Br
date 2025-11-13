using System;
using System.Windows;

namespace apctray2;

public partial class ConfigWindow : Window
{
    private readonly TrayIcon _tray;
    public ConfigWindow(TrayIcon tray)
    {
        InitializeComponent();
        _tray = tray;
        
        // Conexão
        HostBox.Text = Settings.Current.Host;
        PortBox.Text = Settings.Current.Port.ToString();
        RefreshBox.Text = Settings.Current.RefreshSeconds.ToString();
        
        // Limites
        MinVoltBox.Text = Settings.Current.MinVoltage.ToString();
        MaxVoltBox.Text = Settings.Current.MaxVoltage.ToString();
        MinFreqBox.Text = Settings.Current.MinFrequency.ToString();
        MaxFreqBox.Text = Settings.Current.MaxFrequency.ToString();
        
        // Bateria
        BattVoltBox.Text = Settings.Current.BatteryNominalVoltage.ToString();
        BattCapBox.Text = Settings.Current.BatteryNominalCapacityAh.ToString();
        PowerFactorBox.Text = Settings.Current.AssumedPowerFactor.ToString();
        
        // Telegram
        TelegramEnableBox.IsChecked = Settings.Current.TelegramEnabled;
        BotTokenBox.Text = Settings.Current.TelegramBotToken;
        ChatIdBox.Text = Settings.Current.TelegramChatId;
        DailyLogHourBox.Text = Settings.Current.DailyLogHour.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Conexão
            if (!int.TryParse(PortBox.Text, out var port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Porta inválida.");
                return;
            }
            if (!int.TryParse(RefreshBox.Text, out var refresh) || refresh < 1)
            {
                MessageBox.Show("Intervalo de atualização inválido.");
                return;
            }
            
            Settings.Current.Host = HostBox.Text.Trim();
            Settings.Current.Port = port;
            Settings.Current.RefreshSeconds = refresh;
            
            // Limites
            if (double.TryParse(MinVoltBox.Text, out var minV)) Settings.Current.MinVoltage = minV;
            if (double.TryParse(MaxVoltBox.Text, out var maxV)) Settings.Current.MaxVoltage = maxV;
            if (double.TryParse(MinFreqBox.Text, out var minF)) Settings.Current.MinFrequency = minF;
            if (double.TryParse(MaxFreqBox.Text, out var maxF)) Settings.Current.MaxFrequency = maxF;
            
            // Bateria
            if (double.TryParse(BattVoltBox.Text, out var battV)) Settings.Current.BatteryNominalVoltage = battV;
            if (double.TryParse(BattCapBox.Text, out var battC)) Settings.Current.BatteryNominalCapacityAh = battC;
            if (double.TryParse(PowerFactorBox.Text, out var pf)) Settings.Current.AssumedPowerFactor = pf;
            
            // Telegram
            Settings.Current.TelegramEnabled = TelegramEnableBox.IsChecked ?? false;
            Settings.Current.TelegramBotToken = BotTokenBox.Text.Trim();
            Settings.Current.TelegramChatId = ChatIdBox.Text.Trim();
            if (int.TryParse(DailyLogHourBox.Text, out var hour) && hour >= 0 && hour < 24)
                Settings.Current.DailyLogHour = hour;
            
            Settings.Current.Save();
            _tray.RefreshClientFromSettings();
            MessageBox.Show("Configurações salvas com sucesso!");
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}");
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var host = HostBox.Text.Trim();
        if (!int.TryParse(PortBox.Text, out var port) || port <= 0 || port > 65535)
        {
            MessageBox.Show("Porta inválida.");
            return;
        }
        var client = new NisClient(host, port);
        try
        {
            var ok = await client.TestAsync(2000);
            MessageBox.Show(ok ? "Conectado ao NIS!" : "Sem resposta do NIS.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha: {ex.Message}");
        }
    }
    
    private async void TestTelegram_Click(object sender, RoutedEventArgs e)
    {
        var token = BotTokenBox.Text.Trim();
        var chatId = ChatIdBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            MessageBox.Show("Preencha o Bot Token e Chat ID.");
            return;
        }
        
        var result = await TelegramService.SendMessageAsync(token, chatId, "🧪 Teste de notificação do apctray2!");
        MessageBox.Show(result ? "✅ Mensagem enviada com sucesso!" : "❌ Falha ao enviar mensagem. Verifique o token e chat ID.");
    }
}
