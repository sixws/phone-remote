# Find residual processes of the uninstalled app
$patterns = @('ryen', 'nsd', 'netspeed', 'dynamic')

Write-Output "=== Processes matching command line ==="
Get-CimInstance Win32_Process | Where-Object {
    $cmd = $_.CommandLine
    if (-not $cmd) { return $false }
    foreach ($p in $patterns) {
        if ($cmd -like "*$p*") { return $true }
    }
    return $false
} | ForEach-Object {
    Write-Output ("PID {0}  {1}" -f $_.ProcessId, $_.Name)
    Write-Output ("      {0}" -f $_.CommandLine)
}

Write-Output ""
Write-Output "=== Windows with matching titles ==="
Get-Process | Where-Object { $_.MainWindowTitle -like '*nsd*' -or $_.MainWindowTitle -like '*NetSpeed*' } | ForEach-Object {
    Write-Output ("PID {0}  {1}" -f $_.Id, $_.MainWindowTitle)
}
Write-Output "=== Check done ==="
