param(
    [int]$MaxWaitSeconds = 600
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

# fresh launch with the install page forced
$env:GIFSALMON_FORCE_INSTALL = '1'
Remove-Item "$env:LOCALAPPDATA\GifSalmon\shell.log" -ErrorAction SilentlyContinue
Start-Process "$PSScriptRoot\..\bin\Debug\net8.0-windows10.0.19041.0\win-x64\GifSalmon.Shell.exe"
Start-Sleep -Seconds 8

$p = Get-Process GifSalmon.Shell -ErrorAction Stop | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output "app: pid=$($p.Id) title='$($p.MainWindowTitle)'"

& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Target GifSalmon.Shell -OutFile "$env:TEMP\opencode\install_page.png"

$btn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, 'Download & Install')))
if (-not $btn) { throw 'Download & Install button not found' }
($btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
Write-Output 'invoked Download & Install — downloading ffmpeg (BtbN zip is ~90MB)...'

$log = "$env:LOCALAPPDATA\GifSalmon\shell.log"
$deadline = (Get-Date).AddSeconds($MaxWaitSeconds)
$done = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    if (Get-Content $log -ErrorAction SilentlyContinue | Select-String "\[install\] source=") { $done = $true; break }
}
Write-Output ($done ? 'install finished' : 'install did NOT finish in time')

Get-Content $log | Select-String "installer|install\]" | ForEach-Object { $_.Line }
Start-Sleep -Seconds 2
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Target GifSalmon.Shell -OutFile "$env:TEMP\opencode\install_after.png"
