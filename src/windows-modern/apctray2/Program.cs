using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace apctray2;

public static class Program
{
    [STAThread]
    public static void Main(string[]? args = null)
    {
        args ??= Array.Empty<string>();

        // Basic CLI compatibility
        if (args.Any(a => string.Equals(a, "/install", StringComparison.OrdinalIgnoreCase)))
        {
            AutoStartManager.InstallForCurrentUser();
            return;
        }
        if (args.Any(a => string.Equals(a, "/remove", StringComparison.OrdinalIgnoreCase)))
        {
            AutoStartManager.RemoveForCurrentUser();
            return;
        }
        if (args.Any(a => string.Equals(a, "/kill", StringComparison.OrdinalIgnoreCase)))
        {
            ProcessHelpers.KillOtherInstances();
            return;
        }

        var app = new App();
        using var tray = new TrayIcon();
        TrayIcon.Current = tray;
        app.Run();
    }
}
