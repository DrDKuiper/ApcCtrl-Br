using System;

namespace apctray2;

/// <summary>
/// Eventos globais para notificar mudanças na aplicação
/// </summary>
public static class AppEvents
{
    public static event EventHandler<EventArgs>? ProfilesChanged;
    public static event EventHandler<EventArgs>? SettingsChanged;

    public static void NotifyProfilesChanged()
    {
        SimpleLogger.Info("AppEvents: ProfilesChanged fired");
        ProfilesChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifySettingsChanged()
    {
        SimpleLogger.Info("AppEvents: SettingsChanged fired");
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }
}
