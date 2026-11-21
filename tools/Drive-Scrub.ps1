param(
    [Parameter(Mandatory = $true)][double]$Value,
    [int]$SettleSeconds = 12
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process GifSalmon.Shell -ErrorAction Stop | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output "app: pid=$($p.Id)"

$sliders = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Slider)))

$scrub = $null
foreach ($s in $sliders) {
    $rv = $s.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern).Current
    Write-Output ("  slider min={0} max={1} val={2}" -f $rv.Minimum, $rv.Maximum, $rv.Value)
    if ($rv.Minimum -eq 0 -and $rv.Maximum -gt 1) { $scrub = $s }
}
if (-not $scrub) { throw 'scrubber not found' }

($scrub.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)).SetValue($Value)
Write-Output "scrubber set to $Value"

Start-Sleep -Seconds $SettleSeconds
& (Join-Path $PSScriptRoot 'Capture-Window.ps1') -Target GifSalmon.Shell -OutFile "$env:TEMP\opencode\e2e_scrub.png"
