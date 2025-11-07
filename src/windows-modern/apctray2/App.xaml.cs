using System;
using System.Linq;
using System.Windows;

namespace apctray2;

public partial class App : Application
{
	private TrayIcon? _tray;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Keep app alive without windows
		ShutdownMode = ShutdownMode.OnExplicitShutdown;

		var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
		if (args.Any(a => string.Equals(a, "/install", StringComparison.OrdinalIgnoreCase)))
		{
			AutoStartManager.InstallForCurrentUser();
			Shutdown();
			return;
		}
		if (args.Any(a => string.Equals(a, "/remove", StringComparison.OrdinalIgnoreCase)))
		{
			AutoStartManager.RemoveForCurrentUser();
			Shutdown();
			return;
		}
		if (args.Any(a => string.Equals(a, "/kill", StringComparison.OrdinalIgnoreCase)))
		{
			ProcessHelpers.KillOtherInstances();
			Shutdown();
			return;
		}

		_tray = new TrayIcon();
		TrayIcon.Current = _tray;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_tray?.Dispose();
		base.OnExit(e);
	}
}
