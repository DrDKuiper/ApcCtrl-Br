using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Timers;

namespace apctray2;

public class MetricSample
{
    public DateTime Time { get; set; }
    public double? Charge { get; set; }
    public double? Load { get; set; }
    public double? LineV { get; set; }
    public double? Freq { get; set; }
    public double? TimeLeft { get; set; }
    public double? TimeLeftEst { get; set; }
}

public sealed class MetricsStore
{
    private const int MaxSamples = 2880; // 48h a 1 amostra/minuto
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "apctray2"
    );
    private static readonly string SavePath = Path.Combine(AppDataDir, "metrics.json");
    
    public ObservableCollection<MetricSample> Samples { get; } = new();
    
    private readonly System.Timers.Timer _saveTimer;
    private readonly object _lock = new();

    public MetricsStore()
    {
        Load();
        
        // Auto-save a cada 5 minutos
        _saveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _saveTimer.Elapsed += (s, e) => Save();
        _saveTimer.AutoReset = true;
        _saveTimer.Start();
    }

    public void Append(MetricSample sample)
    {
        lock (_lock)
        {
            Samples.Add(sample);
            if (Samples.Count > MaxSamples)
            {
                Samples.RemoveAt(0);
            }
        }
    }

    public void Save()
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(AppDataDir);
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(Samples, options);
                File.WriteAllText(SavePath, json);
                System.Diagnostics.Debug.WriteLine($"[MetricsStore] Saved {Samples.Count} samples to {SavePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MetricsStore] Save error: {ex.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            
            var json = File.ReadAllText(SavePath);
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var loaded = JsonSerializer.Deserialize<ObservableCollection<MetricSample>>(json, options);
            
            if (loaded != null)
            {
                lock (_lock)
                {
                    Samples.Clear();
                    foreach (var sample in loaded)
                    {
                        Samples.Add(sample);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[MetricsStore] Loaded {Samples.Count} samples from {SavePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MetricsStore] Load error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _saveTimer?.Stop();
        _saveTimer?.Dispose();
        Save();
    }
}
