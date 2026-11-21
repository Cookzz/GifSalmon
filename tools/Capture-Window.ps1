param(
    [Parameter(Mandatory = $true)][string]$Target,   # process name (e.g. GifSalmon.Shell, electron)
    [Parameter(Mandatory = $true)][string]$OutFile,
    [string]$Title                                    # optional: match by window title instead
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Shot {
    public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
"@

$p = if ($Title) {
    Get-Process | Where-Object { $_.MainWindowTitle -eq $Title -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
} else {
    Get-Process -Name $Target -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
}
if (-not $p) { throw "no window found (target=$Target title=$Title)" }

$h = $p.MainWindowHandle
[Win32Shot]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 400

$r = New-Object Win32Shot+RECT
[Win32Shot]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$hgt = $r.Bottom - $r.Top
if ($w -le 0 -or $hgt -le 0) { throw "bad window rect" }

$bmp = New-Object System.Drawing.Bitmap $w, $hgt
$g = [System.Drawing.Graphics]::FromImage($bmp)
$dc = $g.GetHdc()
# PW_RENDERFULLCONTENT (2): renders DWM-composed content (WinUI3/DirectComposition)
[Win32Shot]::PrintWindow($h, $dc, 2) | Out-Null
$g.ReleaseHdc($dc)
$g.Dispose()
$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved $OutFile (${w}x${hgt}) pid=$($p.Id) title='$($p.MainWindowTitle)'"
