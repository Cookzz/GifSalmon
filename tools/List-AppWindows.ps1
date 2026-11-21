Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public class WinEnum {
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    public static List<string> ForPid(uint targetPid) {
        var results = new List<string>();
        EnumWindows((h, l) => {
            GetWindowThreadProcessId(h, out var pid);
            if (pid == targetPid) {
                var title = new StringBuilder(256); GetWindowText(h, title, 256);
                var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                results.Add(string.Format("hwnd=0x{0:X} visible={1} class='{2}' title='{3}'", h.ToInt64(), IsWindowVisible(h), cls, title));
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }
}
"@

$p = Get-Process GifSalmon.Shell | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
Write-Output "app pid=$($p.Id)"
[WinEnum]::ForPid([uint32]$p.Id) | ForEach-Object { Write-Output "  $_" }
