Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$sw = [System.Diagnostics.Stopwatch]::StartNew()
Start-Process "$PSScriptRoot\..\bin\Debug\net8.0-windows10.0.19041.0\win-x64\GifSalmon.Shell.exe"
while ($sw.ElapsedMilliseconds -lt 20000) {
    $p = Get-Process -Name GifSalmon.Shell -ErrorAction SilentlyContinue |
        Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    if ($p) {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        $btn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, 'Select File')))
        if ($btn) {
            Write-Output ("winui ready-to-interact (fresh): {0}ms" -f $sw.ElapsedMilliseconds)
            return
        }
    }
    Start-Sleep -Milliseconds 100
}
Write-Output "not ready within 20s"
