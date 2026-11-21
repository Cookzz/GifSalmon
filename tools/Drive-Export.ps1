param([int]$MidCaptureSeconds = 10, [int]$MaxWaitSeconds = 240)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process GifSalmon.Shell -ErrorAction Stop | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output "app: pid=$($p.Id)"

$btn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, 'Export')))
if (-not $btn) { throw 'Export button not found' }
($btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
Write-Output 'invoked Export — waiting for save dialog (click Save in the dialog!)'

# capture the UI mid-export once ffmpeg has actually started
$log = "$env:LOCALAPPDATA\GifSalmon\shell.log"
$deadline = (Get-Date).AddSeconds(120)
$started = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if (Get-Content $log -ErrorAction SilentlyContinue | Select-String "\[ffmpeg:export cmd\]") { $started = $true; break }
}
if ($started) {
    Write-Output 'export started - capturing mid-progress'
    Start-Sleep -Seconds $MidCaptureSeconds
    & (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Target GifSalmon.Shell -OutFile "$env:TEMP\opencode\e2e_mid.png"
}

# wait for completion
$deadline = (Get-Date).AddSeconds($MaxWaitSeconds)
$done = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if (Get-Content $log -ErrorAction SilentlyContinue | Select-String "\[ffmpeg:export\] exit=") { $done = $true; break }
}
Write-Output ($done ? 'export finished' : 'export did not finish in time')
Get-Content $log | Select-String "\[ffmpeg:export\] exit=|\[ffmpeg:export stderr\]" | Select-Object -Last 2 | ForEach-Object { $_.Line }
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Target GifSalmon.Shell -OutFile "$env:TEMP\opencode\e2e_export.png"
