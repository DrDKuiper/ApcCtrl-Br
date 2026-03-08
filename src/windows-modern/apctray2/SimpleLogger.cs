using System;
using System.IO;

namespace apctray2;

public static class SimpleLogger
{
    private static readonly object _lock = new();
    private static string? _logFilePath;

    private static string GetLogFilePath()
    {
        if (_logFilePath != null)
            return _logFilePath;

        try
        {
            // Tentar primeiro em AppData\Roaming
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "apctray2");
            Directory.CreateDirectory(appDataDir);
            _logFilePath = Path.Combine(appDataDir, "apctray2.log");
            
            // Testar escrita
            File.AppendAllText(_logFilePath, $"\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] Log initialized in AppData\\Roaming\n");
            return _logFilePath;
        }
        catch
        {
            try
            {
                // Fallback para Temp
                var tempDir = Path.Combine(Path.GetTempPath(), "apctray2");
                Directory.CreateDirectory(tempDir);
                _logFilePath = Path.Combine(tempDir, "apctray2.log");
                
                File.AppendAllText(_logFilePath, $"\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] Log initialized in Temp directory (fallback)\n");
                return _logFilePath;
            }
            catch
            {
                // Ultimate fallback - apenas não logar
                _logFilePath = string.Empty;
                return _logFilePath;
            }
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception ex, string context) => Write("ERROR", $"{context}: {ex}");

    private static void Write(string level, string message)
    {
        try
        {
            var logPath = GetLogFilePath();
            if (string.IsNullOrEmpty(logPath)) return; // Não logando em fallback
            
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Nunca deixar logging derrubar a aplicação.
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Logging failed: {message}");
        }
    }

    /// <summary>
    /// Retorna o caminho do arquivo de log
    /// </summary>
    public static string GetLogPath() => GetLogFilePath();
}
