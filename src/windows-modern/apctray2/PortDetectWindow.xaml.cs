using System;
using System.Linq;
using System.Windows;

namespace apctray2;

public partial class PortDetectWindow : Window
{
    private SerialPortInfo? _suggestion;

    public PortDetectWindow()
    {
        InitializeComponent();
        LoadPorts();
    }

    private void LoadPorts()
    {
        var ports = PortDetector.GetSerialPorts();
        PortsList.ItemsSource = ports;
        _suggestion = PortDetector.IdentifyLikelyUPS(ports);
        if (_suggestion != null)
        {
            PortsList.SelectedItem = ports.FirstOrDefault(p => p.Port == _suggestion.Port);
            Title = $"Detectar Nobreak (Sugerido: {_suggestion.Port})";
        }
        else
        {
            Title = "Detectar Nobreak (sem sugestão)";
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadPorts();
    }

    private void CopySuggestion_Click(object sender, RoutedEventArgs e)
    {
        var sel = (SerialPortInfo?)PortsList.SelectedItem ?? _suggestion;
        if (sel == null)
        {
            MessageBox.Show("Nenhuma porta selecionada ou sugerida.");
            return;
        }
        Clipboard.SetText(sel.Port);
        MessageBox.Show($"Copiado: {sel.Port}. Use este valor como DEVICE no apcctrl.conf.");
    }
}
