using System;
using System.IO;

namespace apctray2;

public static class SimpleLogger
{
    private static readonly object _lock = new();
    private static string LogFilePath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "apctray2");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "apctray2.log");
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception ex, string context) => Write("ERROR", $"{context}: {ex}");

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Nunca deixar logging derrubar a aplicação.
        }
    }
}
