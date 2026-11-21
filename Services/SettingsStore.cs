using System.Text.Json;

namespace GifSalmon.Shell.Services;

public sealed class AppSettings
{
    public FfmpegPaths Ffmpeg { get; set; } = new();
    public WindowState Window { get; set; } = new();
}

public sealed class FfmpegPaths
{
    public string? Ffmpeg { get; set; }
    public string? Ffprobe { get; set; }
}

public sealed class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Replaces Electron's lowdb store (app_db.json). Same shape, written to
/// %LOCALAPPDATA%\GifSalmon\app_db.json.
/// </summary>
public sealed class SettingsStore
{
    private static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GifSalmon");

    private static string FilePath => Path.Combine(Dir, "app_db.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt settings shouldn't take the app down.
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // Best effort.
        }
    }
}
