using System;
using System.IO;
using System.Text.Json;

namespace apctray2;

public sealed class Settings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3551;
    public int RefreshSeconds { get; set; } = 5;

    private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "apctray2");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static Settings? _current;
    public static Settings Current => _current ??= Load();

    public static Settings Load()
    {
        try
        {
            // Env overrides
            var envHost = Environment.GetEnvironmentVariable("APCCTRL_HOST");
            var envPort = Environment.GetEnvironmentVariable("APCCTRL_PORT");

            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                if (!string.IsNullOrWhiteSpace(envHost)) s.Host = envHost!;
                if (int.TryParse(envPort, out var p)) s.Port = p;
                return s;
            }

            var def = new Settings();
            if (!string.IsNullOrWhiteSpace(envHost)) def.Host = envHost!;
            if (int.TryParse(envPort, out var ep)) def.Port = ep;
            return def;
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
        _current = this;
    }
}
