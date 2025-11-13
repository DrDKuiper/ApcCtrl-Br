using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace apctray2;

public partial class EnergyFlowControl : UserControl
{
    private readonly Storyboard _arrowAnimation;
    
    public EnergyFlowControl()
    {
        InitializeComponent();
        
        // Criar animação de setas
        _arrowAnimation = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        
        var anim1 = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(2),
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(anim1, ArrowRotation1);
        Storyboard.SetTargetProperty(anim1, new PropertyPath(RotateTransform.AngleProperty));
        _arrowAnimation.Children.Add(anim1);
        
        var anim2 = new DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = TimeSpan.FromSeconds(0.8),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(anim2, ArrowScale2);
        Storyboard.SetTargetProperty(anim2, new PropertyPath(ScaleTransform.ScaleXProperty));
        _arrowAnimation.Children.Add(anim2);
        
        Loaded += (s, e) => _arrowAnimation.Begin();
    }
    
    public void UpdateStatus(Dictionary<string, string> statusData)
    {
        var status = statusData.GetValueOrDefault("STATUS", "");
        var isOnBatt = status.Contains("ONBATT", StringComparison.OrdinalIgnoreCase);
        var isCharging = status.Contains("CHARGING", StringComparison.OrdinalIgnoreCase);
        
        // Detectar alertas
        var gridAlert = statusData.GetValueOrDefault("GRID_ALERT") == "1";
        var battAlert = statusData.GetValueOrDefault("BATT_ALERT") == "1";
        var upsAlert = statusData.GetValueOrDefault("UPS_ALERT") == "1";
        
        // Cores dinâmicas
        var standby = new SolidColorBrush(Color.FromRgb(239, 68, 68)) { Opacity = 0.6 };
        var gridColor = gridAlert ? Brushes.Red : (isOnBatt ? standby : Brushes.LimeGreen);
        var battColor = battAlert ? Brushes.Red : (isOnBatt ? Brushes.Orange : standby);
        var upsColor = upsAlert ? Brushes.Red : (isCharging ? Brushes.Yellow : (isOnBatt ? Brushes.Orange : Brushes.LimeGreen));
        var outColor = isOnBatt ? Brushes.Orange : Brushes.LimeGreen;
        
        // Atualizar ícones e cores
        GridIcon.Foreground = gridColor;
        GridValue.Foreground = gridColor;
        GridValue.Text = statusData.GetValueOrDefault("LINEV", "--");
        
        BatteryIcon.Foreground = battColor;
        BatteryValue.Foreground = battColor;
        BatteryValue.Text = statusData.GetValueOrDefault("BCHARGE", "--");
        
        UpsIcon.Foreground = upsColor;
        UpsLabel.Text = isCharging ? "UPS (Carregando)" : "UPS";
        
        DevicesIcon.Foreground = outColor;
        DevicesValue.Foreground = outColor;
        DevicesValue.Text = statusData.GetValueOrDefault("LOADPCT", "--");
        
        // Cor das setas
        GridBatteryArrow.Foreground = isOnBatt ? battColor : gridColor;
        UpsDevicesArrow.Foreground = outColor;
    }
}
