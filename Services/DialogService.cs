using System.Runtime.InteropServices;
using Windows.Storage.Pickers;

namespace GifSalmon.Shell.Services;

public sealed record FileResult(string Name, string Path);

/// <summary>
/// Replaces Electron's dialog.showSaveDialog / input[type=file] path discovery.
/// File pickers must be initialized with the window HWND in unpackaged WinUI 3 apps.
/// </summary>
public sealed class DialogService
{
    private static readonly string[] VideoExtensions =
    [
        ".mp4", ".m4v", ".mkv", ".webm", ".mov", ".avi",
        ".wmv", ".flv", ".mpg", ".mpeg", ".ts", ".m2ts", ".gif",
    ];

    private readonly Microsoft.UI.Xaml.Window _window;

    public DialogService(Microsoft.UI.Xaml.Window window) => _window = window;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Replaces webUtils.getPathForFile(File) — returns the real path of the picked file.</summary>
    /// <remarks>
    /// The unpackaged WinAppSDK picker host intermittently fails to attach its
    /// dialog (task stays WaitingForActivation forever). Retry with the window
    /// activated — matching what a user's click would do.
    /// </remarks>
    public async Task<FileResult?> OpenAsync()
    {
        for (var attempt = 1; ; attempt++)
        {
            ActivateWindow();
            var picker = new FileOpenPicker();
            InitializeWithWindow(picker);
            foreach (var ext in VideoExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }

            ShellLog.Write($"[picker] attempt {attempt}: PickSingleFileAsync starting");
            var task = picker.PickSingleFileAsync().AsTask();
            var finished = await Task.WhenAny(task, Task.Delay(8_000));
            if (finished == task)
            {
                var file = await task;
                ShellLog.Write($"[picker] returned {(file is null ? "(cancelled)" : file.Path)}");
                return file is null ? null : new FileResult(file.Name, file.Path);
            }

            ShellLog.Write($"[picker] attempt {attempt}: TIMEOUT (task {task.Status})");
            if (attempt >= 3)
            {
                return null;
            }
        }
    }

    private void ActivateWindow()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            SetForegroundWindow(hwnd);
        }
        catch
        {
            // Best effort.
        }
    }

    /// <summary>Replaces dialog.showSaveDialog({ defaultPath: output, buttonLabel: "Export" }).</summary>
    public async Task<string?> SaveAsync(string suggestedOutput)
    {
        for (var attempt = 1; ; attempt++)
        {
            ActivateWindow();
            var picker = new FileSavePicker();
            InitializeWithWindow(picker);

            var output = suggestedOutput ?? "";
            var name = Path.GetFileNameWithoutExtension(output);
            picker.SuggestedFileName = string.IsNullOrWhiteSpace(name) ? "export" : name;
            picker.DefaultFileExtension = ".gif";
            picker.FileTypeChoices.Add("GIF image", new List<string> { ".gif" });

            ShellLog.Write($"[picker] attempt {attempt}: PickSaveFileAsync starting");
            var task = picker.PickSaveFileAsync().AsTask();
            var finished = await Task.WhenAny(task, Task.Delay(8_000));
            if (finished == task)
            {
                var file = await task;
                ShellLog.Write($"[picker] save returned {(file is null ? "(cancelled)" : file.Path)}");
                return file?.Path;
            }

            ShellLog.Write($"[picker] attempt {attempt}: save TIMEOUT (task {task.Status})");
            if (attempt >= 3)
            {
                return null;
            }
        }
    }

    private void InitializeWithWindow(object picker)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }
}
