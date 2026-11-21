param(
    [Parameter(Mandatory = $true)][string]$FilePath,
    [string]$OutFile = "$env:TEMP\opencode\e2e.png",
    [int]$SettleSeconds = 25
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-ByName($root, $name, $scope = [System.Windows.Automation.TreeScope]::Descendants) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $root.FindFirst($scope, $cond)
}

# 1) app window
$p = Get-Process GifSalmon.Shell -ErrorAction Stop | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output "app: pid=$($p.Id)"

# 2) invoke Select File (or the Change button if idle button is hidden)
$btn = Find-ByName $root 'Select File'
if (-not $btn) { $btn = Find-ByName $root $FilePath }
if (-not $btn) { throw 'Select File button not found' }
($btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
Write-Output 'invoked Select File'

# 3) wait for the modal open dialog (#32770)
$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, '#32770')
$dialog = $null
for ($i = 0; $i -lt 20 -and -not $dialog; $i++) {
    Start-Sleep -Milliseconds 500
    $candidates = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
    foreach ($c in $candidates) {
        if ($c.Current.ProcessId -eq $p.Id -and $c.Current.Name -match 'Open') { $dialog = $c }
    }
}
if (-not $dialog) {
    Write-Output 'open dialog not visible to UIA - relying on picker retry/user action'
    return
}
Write-Output "dialog: '$($dialog.Current.Name)'"

# 4) set the file name and confirm
$edit = $dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)))
if (-not $edit) { throw 'file name edit not found' }
($edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($FilePath)
Write-Output "filename set: $FilePath"

$openBtn = $null
$buttons = $dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)))
foreach ($b in $buttons) {
    if ($b.Current.Name -match '^Open$') { $openBtn = $b }
}
if (-not $openBtn) { throw 'Open button not found' }
($openBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
Write-Output 'invoked Open'
