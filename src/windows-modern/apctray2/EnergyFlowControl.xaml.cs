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
        
        // Opacidades baseadas no fluxo real
        // Quando em bateria, "apagar" a rede; quando em rede, destacar Grid ↔ UPS
        double gridOpacity    = isOnBatt ? 0.25 : 1.0;
        double gridArrowOpacity = isOnBatt ? 0.25 : 1.0;
        double battOpacity    = 1.0; // Bateria sempre relevante
        double upsOpacity     = 1.0; // UPS sempre relevante
        double outOpacity     = 1.0; // Dispositivos sempre relevantes
        
        // Se comunicação perdida, apagar quase tudo
        bool commLost = status.Contains("COMMLOST", StringComparison.OrdinalIgnoreCase);
        if (commLost)
        {
            gridOpacity = 0.15;
            gridArrowOpacity = 0.15;
            battOpacity = 0.15;
            upsOpacity = 0.15;
            outOpacity = 0.15;
        }
        
        // Atualizar ícones e cores (com opacidade)
        GridIcon.Foreground = gridColor;
        GridValue.Foreground = gridColor;
        GridValue.Text = statusData.GetValueOrDefault("LINEV", "--");
        GridIcon.Opacity = gridOpacity;
        GridValue.Opacity = gridOpacity;
        
        BatteryIcon.Foreground = battColor;
        BatteryValue.Foreground = battColor;
        BatteryValue.Text = statusData.GetValueOrDefault("BCHARGE", "--");
        BatteryIcon.Opacity = battOpacity;
        BatteryValue.Opacity = battOpacity;
        
        UpsIcon.Foreground = upsColor;
        UpsLabel.Text = isCharging ? "UPS (Carregando)" : "UPS";
        UpsIcon.Opacity = upsOpacity;
        UpsLabel.Opacity = upsOpacity;
        
        DevicesIcon.Foreground = outColor;
        DevicesValue.Foreground = outColor;
        DevicesValue.Text = statusData.GetValueOrDefault("LOADPCT", "--");
        DevicesIcon.Opacity = outOpacity;
        DevicesValue.Opacity = outOpacity;
        
        // Cor e opacidade das setas
        GridBatteryArrow.Foreground = isOnBatt ? battColor : gridColor;
        GridBatteryArrow.Opacity = gridArrowOpacity;
        UpsDevicesArrow.Foreground = outColor;
        UpsDevicesArrow.Opacity = commLost ? 0.15 : 1.0;
    }
}
