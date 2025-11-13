using System;
using System.IO;
using System.Text.Json;

namespace apctray2;

public sealed class Settings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3551;
    public int RefreshSeconds { get; set; } = 5;
    
    // Voltage and frequency thresholds
    public double MinVoltage { get; set; } = 105.0;
    public double MaxVoltage { get; set; } = 140.0;
    public double MinFrequency { get; set; } = 58.0;
    public double MaxFrequency { get; set; } = 62.0;
    
    // Battery configuration (2x12V/7Ah in series)
    public double BatteryNominalVoltage { get; set; } = 24.0;
    public double BatteryNominalCapacityAh { get; set; } = 7.0;
    
    // Power factor for VA → W conversion
    public double AssumedPowerFactor { get; set; } = 0.6;
    
    // Cycle tracking
    public int CycleCount { get; set; } = 0;
    public double OnBattStartEpoch { get; set; } = 0;
    public double LastOnBattSeconds { get; set; } = 0;
    
    // Capacity estimation
    public double EstimatedCapacityAh { get; set; } = 0;
    public int EstimatedCapacitySamples { get; set; } = 0;
    public double BatteryReplacedEpoch { get; set; } = 0;
    
    // Telegram configuration
    public string TelegramBotToken { get; set; } = "";
    public string TelegramChatId { get; set; } = "";
    public int DailyLogHour { get; set; } = 8;
    public bool TelegramEnabled { get; set; } = false;

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
