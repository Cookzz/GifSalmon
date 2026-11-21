Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, '#32770')
$wins = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
Write-Output "top-level #32770 windows: $($wins.Count)"
foreach ($w in $wins) {
    Write-Output ("  '{0}' | pid={1} | class={2} | enabled={3}" -f `
        $w.Current.Name, $w.Current.ProcessId, $w.Current.ClassName, $w.Current.IsEnabled)
}
