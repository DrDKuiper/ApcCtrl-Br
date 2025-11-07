using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;

namespace apctray2;

public sealed class SerialPortInfo
{
    public string Port { get; init; } = string.Empty;
    public string FriendlyName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string PnpDeviceId { get; init; } = string.Empty;
    public override string ToString() => string.IsNullOrWhiteSpace(FriendlyName) ? Port : $"{FriendlyName} ({Port})";
}

public static class PortDetector
{
    public static List<SerialPortInfo> GetSerialPorts()
    {
        var ports = new List<SerialPortInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
            foreach (var mo in searcher.Get().Cast<ManagementObject>())
            {
                var name = (mo["Name"] as string) ?? string.Empty;
                var mfg = (mo["Manufacturer"] as string) ?? string.Empty;
                var pnp = (mo["PNPDeviceID"] as string) ?? string.Empty;
                var port = ExtractCom(name);
                if (string.IsNullOrEmpty(port)) continue;
                ports.Add(new SerialPortInfo
                {
                    Port = port,
                    FriendlyName = name,
                    Manufacturer = mfg,
                    PnpDeviceId = pnp
                });
            }
        }
        catch
        {
            // Fallback: list only names from SerialPort.GetPortNames
            foreach (var pn in SerialPort.GetPortNames())
            {
                ports.Add(new SerialPortInfo { Port = pn, FriendlyName = pn });
            }
        }

        // Ensure unique and sorted
        return ports
            .GroupBy(p => p.Port)
            .Select(g => g.First())
            .OrderBy(p => NaturalSortKey(p.Port))
            .ToList();
    }

    public static SerialPortInfo? IdentifyLikelyUPS(IEnumerable<SerialPortInfo> ports)
    {
        // Heurísticas: nomes contendo APC/UPS/American Power Conversion preferidos
        string[] keywords = ["APC", "UPS", "American Power Conversion", "Back-UPS", "Smart-UPS"];        
        var byName = ports.FirstOrDefault(p => keywords.Any(k => p.FriendlyName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        if (byName != null) return byName;

        // Caso clássico do Windows: "USB Serial Device (COMx)" é genérico.
        // Se só existir um COM, sugerimos esse.
        if (ports.Count() == 1) return ports.First();

        // Sem certeza
        return null;
    }

    private static string ExtractCom(string name)
    {
        // procura padrão (COM3)
        var idx = name.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var sub = name.Substring(idx + 1).TrimEnd(')');
            if (sub.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                return sub;
        }
        // fallback
        foreach (var pn in SerialPort.GetPortNames())
        {
            if (name.Contains(pn, StringComparison.OrdinalIgnoreCase)) return pn;
        }
        return string.Empty;
    }

    private static (int head, string tail) NaturalSortKey(string s)
    {
        // COM10 > COM2: queremos ordenar numericamente
        if (s.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(s[3..], out var n))
            return (n, string.Empty);
        return (int.MaxValue, s);
    }
}
