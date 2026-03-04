using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace apctray2;

public partial class UpsProfilesWindow : Window
{
    private readonly TrayIcon _tray;

    public UpsProfilesWindow(TrayIcon tray)
    {
        InitializeComponent();
        _tray = tray;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        ProfilesList.ItemsSource = Settings.Current.ProfileManager.Profiles;
        ProfilesList.SelectedItem = Settings.Current.ProfileManager.GetActiveProfile();
        UpdateStatus();
    }

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = ProfilesList.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection && Settings.Current.ProfileManager.Profiles.Count > 1;
        ActivateButton.IsEnabled = hasSelection;
        TestButton.IsEnabled = hasSelection;
        
        var selected = ProfilesList.SelectedItem as UpsProfile;
        var active = Settings.Current.ProfileManager.GetActiveProfile();
        if (selected != null && active != null && selected.Id == active.Id)
        {
            StatusText.Text = "✓ Perfil ativo atualmente";
        }
        else
        {
            StatusText.Text = hasSelection ? "Selecione uma ação" : "";
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new UpsProfileEditDialog();
        if (dialog.ShowDialog() == true && dialog.Profile != null)
        {
            Settings.Current.ProfileManager.AddProfile(dialog.Profile);
            Settings.Current.Save();
            LoadProfiles();
            _tray.RefreshClientFromSettings();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not UpsProfile profile) return;

        var dialog = new UpsProfileEditDialog(profile);
        if (dialog.ShowDialog() == true && dialog.Profile != null)
        {
            profile.Name = dialog.Profile.Name;
            profile.Host = dialog.Profile.Host;
            profile.Port = dialog.Profile.Port;
            profile.Description = dialog.Profile.Description;
            Settings.Current.Save();
            LoadProfiles();

            var active = Settings.Current.ProfileManager.GetActiveProfile();
            if (active != null && active.Id == profile.Id)
            {
                _tray.RefreshClientFromSettings();
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not UpsProfile profile) return;

        if (Settings.Current.ProfileManager.Profiles.Count <= 1)
        {
            MessageBox.Show("Não é possível remover o último perfil.", "Aviso", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Deseja realmente remover o perfil '{profile.Name}'?",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Settings.Current.ProfileManager.RemoveProfile(profile.Id);
            Settings.Current.Save();
            LoadProfiles();
            _tray.RefreshClientFromSettings();
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not UpsProfile profile) return;

        Settings.Current.ProfileManager.SetActiveProfile(profile.Id);
        Settings.Current.Save();
        _tray.RefreshClientFromSettings();
        
        MessageBox.Show($"Perfil '{profile.Name}' ativado!\n\nDados atualizados imediatamente.", 
            "Perfil Ativado", MessageBoxButton.OK, MessageBoxImage.Information);
        
        LoadProfiles();
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not UpsProfile profile) return;

        TestButton.IsEnabled = false;
        StatusText.Text = "🔄 Testando conexão...";

        try
        {
            var client = new NisClient(profile.Host, profile.Port);
            var ok = await client.TestAsync(3000);
            
            if (ok)
            {
                var status = await client.GetStatusAsync();
                var upsName = status.GetValueOrDefault("UPSNAME", "?");
                var statusText = status.GetValueOrDefault("STATUS", "?");
                
                StatusText.Text = $"✅ Conectado!\nUPS: {upsName}\nStatus: {statusText}";
            }
            else
            {
                StatusText.Text = "❌ Sem resposta do servidor NIS";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void UpdateStatus()
    {
        var active = Settings.Current.ProfileManager.GetActiveProfile();
        if (active != null)
        {
            Title = $"Gerenciar Nobreaks - Ativo: {active.Name}";
        }
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

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
