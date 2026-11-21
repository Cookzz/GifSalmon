using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GifSalmon.Shell.Services;

public sealed record FfmpegInstallResult(string Ffmpeg, string Ffprobe, string Source);

/// <summary>Live download stats (Total is 0 when the server sends no Content-Length).</summary>
public sealed record DownloadProgress(long Received, long Total);

/// <summary>
/// Downloads prebuilt ffmpeg/ffprobe. Primary source: BtbN/FFmpeg-Builds
/// (actively maintained; single zip containing both exes). Fallback:
/// ffbinaries.com (the Electron-era source, now stale but battle-tested).
/// Never redistributes — binaries land in the user's local app data.
/// </summary>
public static class FfmpegInstaller
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GifSalmon-Shell/1.0");
        return client;
    }

    private static string DestDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GifSalmon", "ffmpeg");

    public static async Task<FfmpegInstallResult> InstallAsync(IProgress<DownloadProgress>? progress = null)
    {
        try
        {
            var result = await InstallBtbNAsync(progress);
            ShellLog.Write($"[installer] installed from BtbN ({result.Ffmpeg})");
            return result;
        }
        catch (Exception ex)
        {
            ShellLog.Write($"[installer] BtbN failed ({ex.Message}) — falling back to ffbinaries");
        }

        var fallback = await InstallFfbinariesAsync(progress);
        ShellLog.Write($"[installer] installed from ffbinaries ({fallback.Ffmpeg})");
        return fallback;
    }

    // -------------------------------------------------------------- BtbN

    private static async Task<FfmpegInstallResult> InstallBtbNAsync(IProgress<DownloadProgress>? progress)
    {
        var json = await Http.GetStringAsync("https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest");
        using var doc = JsonDocument.Parse(json);

        string? assetUrl = null;
        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            if (name is not null && url is not null &&
                Regex.IsMatch(name, @"^ffmpeg-\w+-latest-win64-gpl\.zip$", RegexOptions.IgnoreCase))
            {
                assetUrl = url;
                break;
            }
        }

        if (assetUrl is null)
        {
            throw new InvalidOperationException("No matching BtbN win64-gpl asset in the latest release.");
        }

        Directory.CreateDirectory(DestDir);
        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        try
        {
            await DownloadAsync(assetUrl, zipPath, progress);

            using var zip = ZipFile.OpenRead(zipPath);
            // BtbN nests the exes under bin/ — match by file name anywhere in the archive.
            var ffmpeg = ExtractSingle(zip, "ffmpeg.exe");
            var ffprobe = ExtractSingle(zip, "ffprobe.exe");
            return new FfmpegInstallResult(ffmpeg, ffprobe, "BtbN");
        }
        finally
        {
            try { File.Delete(zipPath); } catch { /* ignore */ }
        }
    }

    /// <summary>Extracts an entry by file name from anywhere in the archive, overwriting.</summary>
    private static string ExtractSingle(ZipArchive zip, string exeName)
    {
        var entry = zip.Entries.FirstOrDefault(e =>
            e.Name.Equals(exeName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"{exeName} not found in the downloaded archive.");

        var target = Path.Combine(DestDir, exeName);
        entry.ExtractToFile(target, overwrite: true);
        return target;
    }

    // ------------------------------------------------------- ffbinaries

    private static async Task<FfmpegInstallResult> InstallFfbinariesAsync(IProgress<DownloadProgress>? progress)
    {
        var platformKey = Environment.Is64BitOperatingSystem ? "windows-64" : "windows-32";
        var json = await Http.GetStringAsync("https://ffbinaries.com/api/v1/version/latest");

        using var doc = JsonDocument.Parse(json);
        var bin = doc.RootElement.GetProperty("bin").GetProperty(platformKey);
        var ffmpegUrl = bin.GetProperty("ffmpeg").GetString()!;
        var ffprobeUrl = bin.GetProperty("ffprobe").GetString()!;

        Directory.CreateDirectory(DestDir);
        var ffmpeg = await DownloadExtractAsync(ffmpegUrl, "ffmpeg.exe", progress);
        var ffprobe = await DownloadExtractAsync(ffprobeUrl, "ffprobe.exe", progress);
        return new FfmpegInstallResult(ffmpeg, ffprobe, "ffbinaries");
    }

    private static async Task<string> DownloadExtractAsync(string url, string exeName, IProgress<DownloadProgress>? progress)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        try
        {
            await DownloadAsync(url, zipPath, progress);

            using var zip = ZipFile.OpenRead(zipPath);
            return ExtractSingle(zip, exeName);
        }
        finally
        {
            try { File.Delete(zipPath); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Chunked download with live progress. ResponseHeadersRead means the
    /// HttpClient timeout only guards the headers — slow CDN downloads are
    /// allowed to take as long as they need.
    /// </summary>
    private static async Task DownloadAsync(string url, string targetPath, IProgress<DownloadProgress>? progress)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? 0;

        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var fs = File.Create(targetPath);

        var buffer = new byte[64 * 1024];
        long received = 0;
        while (true)
        {
            var read = await httpStream.ReadAsync(buffer);
            if (read <= 0) break;
            await fs.WriteAsync(buffer.AsMemory(0, read));
            received += read;
            progress?.Report(new DownloadProgress(received, total));
        }

        if (total > 0 && received != total)
        {
            throw new InvalidOperationException($"Download incomplete: {received}/{total} bytes");
        }
    }
}
