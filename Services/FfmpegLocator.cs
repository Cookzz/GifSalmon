namespace GifSalmon.Shell.Services;

/// <summary>
/// Replaces the `which` npm package used in app/main.js to locate ffmpeg/ffprobe.
/// </summary>
public static class FfmpegLocator
{
    public static string? FindOnPath(string exeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(dir, exeName + ".exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
