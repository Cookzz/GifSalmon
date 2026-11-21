using System.Diagnostics;

namespace GifSalmon.Shell.Services;

/// <summary>
/// File logger — WinExe apps have no console, so everything lands in
/// %LOCALAPPDATA%\GifSalmon\shell.log (also forwarded to Debug.WriteLine).
/// </summary>
public static class ShellLog
{
    private static readonly object Lock = new();

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GifSalmon", "shell.log");

    public static void Write(string message)
    {
        lock (Lock)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
            catch
            {
                // Never let logging take the app down.
            }
        }
        Debug.WriteLine(message);
    }

    public static void Error(string message, Exception ex)
        => Write($"ERROR {message}: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
}
