param(
    [Parameter(Mandatory = $true)][string]$ProcessName,
    [int]$MaxDepth = 7
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process -Name $ProcessName -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { throw "no window for $ProcessName" }

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output ("ROOT: '{0}' [{1}] enabled={2}" -f $root.Current.Name, $root.Current.ControlType.ProgrammaticName, $root.Current.IsEnabled)

function Dump($el, $depth) {
    if ($depth -gt $MaxDepth) { return }
    $kids = $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($k in $kids) {
        $c = $k.Current
        $name = if ($c.Name) { $c.Name } else { "" }
        $id = if ($c.AutomationID) { " id='$($c.AutomationID)'" } else { "" }
        $val = ""
        if ($c.ControlType.ProgrammaticName -match "Edit|Slider|CheckBox") {
            try { $vp = $k.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern); $val = " value='$($vp.Current.Value)'" } catch { }
            try { $rp = $k.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern); $val = " value='$($rp.Current.Value)'" } catch { }
        }
        Write-Output ("{0}{1} '{2}'{3}{4} enabled={5}" -f ("  " * $depth), $c.ControlType.ProgrammaticName, $name, $id, $val, $c.IsEnabled)
        Dump $k ($depth + 1)
    }
}

Dump $root 1
