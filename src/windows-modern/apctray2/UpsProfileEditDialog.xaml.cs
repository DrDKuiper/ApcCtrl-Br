using System;
using System.Windows;

namespace apctray2;

public partial class UpsProfileEditDialog : Window
{
    public UpsProfile? Profile { get; private set; }

    public UpsProfileEditDialog(UpsProfile? existingProfile = null)
    {
        InitializeComponent();

        if (existingProfile != null)
        {
            Title = "Editar Perfil";
            NameBox.Text = existingProfile.Name;
            HostBox.Text = existingProfile.Host;
            PortBox.Text = existingProfile.Port.ToString();
            DescriptionBox.Text = existingProfile.Description;
            Profile = existingProfile;
        }
        else
        {
            Title = "Novo Perfil";
            NameBox.Text = "Nobreak";
            HostBox.Text = "127.0.0.1";
            PortBox.Text = "3551";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var host = HostBox.Text.Trim();
        var portText = PortBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Por favor, informe um nome para o nobreak.", "Validação", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            MessageBox.Show("Por favor, informe o servidor (IP ou hostname).", "Validação", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            HostBox.Focus();
            return;
        }

        if (!int.TryParse(portText, out var port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Por favor, informe uma porta válida (1-65535).", "Validação", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PortBox.Focus();
            return;
        }

        if (Profile == null)
        {
            Profile = new UpsProfile();
        }

        Profile.Name = name;
        Profile.Host = host;
        Profile.Port = port;
        Profile.Description = DescriptionBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
