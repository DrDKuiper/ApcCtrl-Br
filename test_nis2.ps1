$client = New-Object System.Net.Sockets.TcpClient
$client.Connect('127.0.0.1', 3551)
$stream = $client.GetStream()

# Enviar comando sem newline (6 bytes exatos)
$cmd = [System.Text.Encoding]::ASCII.GetBytes('status')
$stream.Write($cmd, 0, $cmd.Length)
$stream.Flush()

# Ler resposta
$buffer = New-Object byte[] 16384
$sb = New-Object System.Text.StringBuilder
try {
    while ($true) {
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -eq 0) { break }
        $text = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        [void]$sb.Append($text)
    }
} catch {
    Write-Host "Exceção durante leitura (esperado se servidor fechar): $($_.Exception.Message)"
}

Write-Host "=== Resposta NIS ($(${sb}.Length) bytes) ==="
Write-Host $sb.ToString()
Write-Host "=== Fim ==="
$client.Close()
