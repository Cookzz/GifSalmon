param([double]$Value = 5)

# waits for the shell log to reach the 'thumbnail] exit' line of the current
# pipeline (i.e. app state == Preview), then drives the scrubber.
$log = "$env:LOCALAPPDATA\GifSalmon\shell.log"
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 800
    $lines = Get-Content $log -ErrorAction SilentlyContinue | Select-String "\[ffmpeg:thumbnail\] exit=0"
    if ($lines) { $ready = $true; break }
}
if (-not $ready) { throw 'pipeline did not reach Preview in time' }
Start-Sleep -Seconds 1
& (Join-Path $PSScriptRoot 'Drive-Scrub.ps1') -Value $Value -SettleSeconds 12
