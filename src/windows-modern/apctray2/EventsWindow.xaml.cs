using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace apctray2;

public partial class EventsWindow : Window
{
    private readonly TrayIcon _tray;
    private readonly DispatcherTimer _timer;

    public EventsWindow(TrayIcon tray)
    {
        InitializeComponent();
        _tray = tray;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (s, e) => RefreshList();
        _timer.Start();
        RefreshList();
    }

    private void RefreshList()
    {
        var items = _tray.GetEventsCache();
        List.ItemsSource = items.ToList();
        if (items.Count > 0)
            List.ScrollIntoView(items[^1]);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
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
