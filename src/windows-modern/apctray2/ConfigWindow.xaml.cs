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
        HostBox.Text = Settings.Current.Host;
        PortBox.Text = Settings.Current.Port.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port <= 0 || port > 65535)
        {
            MessageBox.Show("Porta inválida.");
            return;
        }
        Settings.Current.Host = HostBox.Text.Trim();
        Settings.Current.Port = port;
        Settings.Current.Save();
        _tray.RefreshClientFromSettings();
        Close();
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
}
