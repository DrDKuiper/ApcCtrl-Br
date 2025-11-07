using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace apctray2;

public static class AutoStartManager
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "Apctray2";

    public static void InstallForCurrentUser()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName ??
                  Environment.ProcessPath ??
                  AppContext.BaseDirectory;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ??
                        Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key?.SetValue(ValueName, $"\"{exe}\"");
    }

    public static void RemoveForCurrentUser()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void Enable() => InstallForCurrentUser();
    
    public static void Disable() => RemoveForCurrentUser();
}
