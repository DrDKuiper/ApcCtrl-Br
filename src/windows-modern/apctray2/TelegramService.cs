using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace apctray2;

public static class TelegramService
{
    private static readonly HttpClient _httpClient = new();
    
    public static async Task<bool> SendMessageAsync(string botToken, string chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            return false;
        
        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "HTML"
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"[Telegram] Response: {result}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] Error: {ex.Message}");
            return false;
        }
    }
    
    public static string BuildDailyLog(string upsName, int cycles, double capacityAh, int capacitySamples, System.Collections.Generic.List<string> todayEvents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>[Log diário do nobreak]</b>");
        sb.AppendLine($"<b>Nome:</b> {upsName}");
        sb.AppendLine($"<b>Data:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"<b>Ciclos:</b> {cycles}");
        
        var capLine = capacityAh > 0 
            ? $"{capacityAh:F1} Ah ({capacitySamples} amostras)" 
            : "--";
        sb.AppendLine($"<b>Capacidade estimada:</b> {capLine}");
        
        sb.AppendLine();
        sb.AppendLine("<b>Eventos do dia:</b>");
        if (todayEvents.Count == 0)
        {
            sb.AppendLine("(nenhum evento registrado hoje)");
        }
        else
        {
            foreach (var evt in todayEvents)
            {
                sb.AppendLine($"- {evt}");
            }
        }
        
        return sb.ToString();
    }
}
