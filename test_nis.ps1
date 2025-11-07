$client = New-Object System.Net.Sockets.TcpClient
$client.Connect('127.0.0.1', 3551)
$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter($stream)
$writer.WriteLine('status')
$writer.Flush()
Start-Sleep -Milliseconds 800
$reader = New-Object System.IO.StreamReader($stream)
Write-Host "=== NIS Response ==="
while ($reader.Peek() -ge 0) {
    $line = $reader.ReadLine()
    Write-Host $line
}
$client.Close()
Write-Host "=== End ==="
