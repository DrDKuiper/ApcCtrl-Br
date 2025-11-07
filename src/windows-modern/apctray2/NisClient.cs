using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace apctray2;

public sealed class NisClient
{
    private readonly string _host;
    private readonly int _port;

    public NisClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    private async Task SendNisMessage(NetworkStream stream, string command)
    {
        var data = Encoding.ASCII.GetBytes(command);
        var len = (short)data.Length;
        var header = new byte[2];
        header[0] = (byte)(len >> 8);   // big-endian (network byte order)
        header[1] = (byte)(len & 0xFF);
        
        await stream.WriteAsync(header, 0, 2);
        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
    }

    private async Task<string> ReceiveNisMessage(NetworkStream stream)
    {
        var sb = new StringBuilder();
        
        while (true)
        {
            // Ler cabeçalho de tamanho (2 bytes, big-endian)
            var header = new byte[2];
            var read = await stream.ReadAsync(header, 0, 2);
            if (read < 2) break;  // EOF
            
            int len = (header[0] << 8) | header[1];
            if (len == 0) break;  // soft EOF (servidor envia len=0 para terminar)
            
            // Ler dados
            var buffer = new byte[len];
            int totalRead = 0;
            while (totalRead < len)
            {
                read = await stream.ReadAsync(buffer, totalRead, len - totalRead);
                if (read <= 0) break;
                totalRead += read;
            }
            
            if (totalRead != len)
                throw new Exception($"NIS: recebido {totalRead} bytes, esperado {len}");
            
            sb.Append(Encoding.ASCII.GetString(buffer));
        }
        
        return sb.ToString();
    }

    public async Task<Dictionary<string, string>> GetStatusAsync()
    {
        using var client = new TcpClient();
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        await client.ConnectAsync(_host, _port);
        using var stream = client.GetStream();

        await SendNisMessage(stream, "status");
        var response = await ReceiveNisMessage(stream);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in response.Replace("\r", "").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();
            map[key] = value;
        }

        return map;
    }
    public async Task<List<string>> GetEventsAsync()
    {
        using var client = new TcpClient();
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        await client.ConnectAsync(_host, _port);
        using var stream = client.GetStream();

        await SendNisMessage(stream, "events");
        var response = await ReceiveNisMessage(stream);

        var list = new List<string>();
        foreach (var line in response.Replace("\r", "").Split('\n'))
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            list.Add(trimmed);
        }
        
        return list;
    }
    public async Task<bool> TestAsync(int timeoutMs = 2000)
    {
        try
        {
            using var client = new TcpClient();
            client.ReceiveTimeout = timeoutMs;
            client.SendTimeout = timeoutMs;
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            await SendNisMessage(stream, "status");
            
            // Ler apenas o cabeçalho da primeira mensagem para confirmar resposta
            var header = new byte[2];
            var read = await stream.ReadAsync(header, 0, 2);
            return read == 2;
        }
        catch
        {
            return false;
        }
    }
}