using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using GifSalmon.Shell.ViewModels;

namespace GifSalmon.Shell.Services;

public sealed record ProbeResult(int Width, int Height, double Duration, string? FrameRate);
public sealed record ExportProgress(double Sec, long Size);

/// <summary>
/// Owns every ffmpeg/ffprobe process. One shared "current process" slot mirrors
/// the single `ffmpeg_ps` variable in main.js: starting a new job kills the old
/// one, and cancel kills it too.
/// </summary>
public sealed class FfmpegService
{
    private readonly object _lock = new();
    private Process? _current;
    private bool _exportCancelled;

    public string? FfmpegPath { get; set; }
    public string? FfprobePath { get; set; }

    public bool HasFfmpeg => File.Exists(FfmpegPath);
    public bool HasFfprobe => File.Exists(FfprobePath);

    // ------------------------------------------------------------------ probe

    /// <summary>probeInput → ProbeResult. Port of the `exec` call in main.js.</summary>
    /// <remarks>
    /// The original code parsed stderr (JSON.parse(se)); ffprobe with
    /// -print_format json writes to stdout, so we parse stdout instead.
    /// </remarks>
    public async Task<ProbeResult?> ProbeAsync(string input)
    {
        if (!HasFfprobe) return null;

        using var proc = Start(FfprobePath!, "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", input);
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        ShellLog.Write($"[ffprobe] exit={proc.ExitCode} stdout={stdout.Length} chars");

        if (proc.ExitCode != 0) return null;

        try
        {
            using var parsed = JsonDocument.Parse(stdout);
            var streams = parsed.RootElement.GetProperty("streams");
            foreach (var stream in streams.EnumerateArray())
            {
                // Same rule as renderer.js: first video stream wins (attached
                // pics come later in the stream list).
                if (stream.TryGetProperty("codec_type", out var ct) && ct.GetString() == "video")
                {
                    var width = (int)(TryGetNumber(stream, "width") ?? 320);
                    var height = (int)(TryGetNumber(stream, "height") ?? 240);
                    // NB: ffprobe JSON emits durations as strings, not numbers.
                    var duration = TryGetNumber(stream, "duration")
                        ?? TryGetNumber(parsed.RootElement, "duration")
                        ?? 0;
                    var frameRate =
                        stream.TryGetProperty("r_frame_rate", out var fr) ? fr.GetString() :
                        stream.TryGetProperty("avg_frame_rate", out var afr) ? afr.GetString() :
                        null;
                    return new ProbeResult(width, height, duration, frameRate);
                }
            }
            return null;
        }
        catch (JsonException ex)
        {
            ShellLog.Error("[ffprobe] JSON parse failed", ex);
            return null;
        }
    }

    /// <summary>Reads a numeric ffprobe field that may be a JSON number or string.</summary>
    private static double? TryGetNumber(JsonElement obj, string prop)
    {
        if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(prop, out var v))
        {
            return null;
        }
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String
            && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    // ---------------------------------------------------------------- palette

    /// <summary>getPalette → palette PNG bytes (piped stdout).</summary>
    public async Task<byte[]?> PaletteAsync(string input, GifSettings s)
    {
        if (!HasFfmpeg || !File.Exists(input)) return null;

        var w = s.Width > 0 ? s.Width : 320;
        var h = s.Height > 0 ? s.Height : 240;
        var fps = s.Fps > 0 ? s.Fps : 24;
        var transparency = s.Alpha ? 1 : 0;
        var colors = s.ColorCount > 0 ? s.ColorCount : 256;

        var scale = $"scale={w}:{h}";
        var vf = string.Format(CultureInfo.InvariantCulture,
            "fps={0},{1}:flags=lanczos,palettegen=stats_mode={2}:max_colors={3}:reserve_transparent={4}",
            fps, scale, s.StatsMode, colors, transparency);

        KillCurrent();
        _current = Start(FfmpegPath!, "-i", input, "-vf", vf, "-f", "image2", "-vcodec", "png", "pipe:1");
        var proc = _current!;

        // ffmpeg writes banner + progress to stderr; if we don't drain it the
        // stderr pipe buffer fills and ffmpeg blocks forever.
        var stderrDrain = proc.StandardError.ReadToEndAsync();

        using var ms = new MemoryStream();
        await proc.StandardOutput.BaseStream.CopyToAsync(ms);
        await proc.WaitForExitAsync();
        await stderrDrain;
        ShellLog.Write($"[ffmpeg:palette] exit={proc.ExitCode} stdout={ms.Length} bytes");

        return proc.ExitCode == 0 ? ms.ToArray() : null;
    }

    // -------------------------------------------------------------- thumbnail

    /// <summary>getThumbnail → GIF frame bytes. Pipes the palette into stdin.</summary>
    public async Task<byte[]?> ThumbnailAsync(string input, byte[] palette, double time, GifSettings s)
    {
        if (!HasFfmpeg || !File.Exists(input)) return null;

        var w = s.Width > 0 ? s.Width : 320;
        var h = s.Height > 0 ? s.Height : 240;
        var fps = s.Fps > 0 ? s.Fps : 24;
        var dither = s.Dither ? $"bayer:bayer_scale={s.DitherScale}" : "none";
        var diffMode = string.IsNullOrEmpty(s.DiffMode) ? "rectangle" : s.DiffMode;

        var scale = $"scale={w}:{h}";
        var vf = string.Format(CultureInfo.InvariantCulture,
            "fps={0},{1}:flags=lanczos [x];[x][1:v]paletteuse=dither={2}:diff_mode={3}",
            fps, scale, dither, diffMode);

        KillCurrent();
        _current = Start(FfmpegPath!,
            "-ss", time.ToString("0.0######", CultureInfo.InvariantCulture),
            "-i", input,
            "-i", "pipe:0",
            "-t", "1",
            "-lavfi", vf,
            "-frames:v", "1",
            "-an", "-f", "image2",
            "-vcodec", "gif", "pipe:1");

        var proc = _current!;
        var stderrDrain = proc.StandardError.ReadToEndAsync();
        await WriteToStdinAsync(proc, palette);

        using var ms = new MemoryStream();
        await proc.StandardOutput.BaseStream.CopyToAsync(ms);
        await proc.WaitForExitAsync();
        await stderrDrain;
        ShellLog.Write($"[ffmpeg:thumbnail] exit={proc.ExitCode} stdout={ms.Length} bytes");

        return proc.ExitCode == 0 ? ms.ToArray() : null;
    }

    // ----------------------------------------------------------------- export

    /// <summary>exportGif → progress / finished / cancelled callbacks.</summary>
    public async Task ExportAsync(
        string input,
        string output,
        byte[] palette,
        GifSettings s,
        Action<ExportProgress> onProgress,
        Action<long> onFinished,
        Action onCancelled)
    {
        if (!HasFfmpeg || !File.Exists(input)) { onFinished(0); return; }

        var w = s.Width > 0 ? s.Width : 320;
        var h = s.Height > 0 ? s.Height : 240;
        var fps = s.Fps > 0 ? s.Fps : 10; // note: main.js exportGif defaults to 10, not 24
        var dither = s.Dither ? $"bayer:bayer_scale={s.DitherScale}" : "none";

        var scale = $"scale={w}:{h}";
        var vf = string.Format(CultureInfo.InvariantCulture,
            "fps={0},{1}:flags=lanczos [x];[x][1:v]paletteuse=dither={2}",
            fps, scale, dither);

        KillCurrent();
        _exportCancelled = false;
        // -y: the FileSavePicker pre-creates the output file, and ffmpeg
        // refuses to overwrite an existing file without it.
        _current = Start(FfmpegPath!,
            "-y",
            "-i", input,
            "-i", "pipe:0",
            "-lavfi", vf,
            "-an",
            "-f", "gif",
            output);

        var proc = _current!;
        ShellLog.Write($"[ffmpeg:export cmd] {proc.StartInfo.FileName} {string.Join(" ", proc.StartInfo.ArgumentList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}");

        var stderrLog = new StringBuilder();
        var stderrTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardError.ReadLineAsync()) != null)
            {
                stderrLog.AppendLine(line);

                // Keep the tail only — gyan/full builds dump huge banners that
                // would push the real error message out of the buffer.
                if (stderrLog.Length > 8000)
                {
                    stderrLog.Remove(0, stderrLog.Length - 8000);
                }

                var progress = ParseProgressLine(line);
                if (progress != null && progress.TryGetValue("time", out var time))
                {
                    // Electron parity: parseInt(size.replace('kB',''))*1000 —
                    // parseInt reads the leading digits, so "78592KiB" → 78592.
                    long size = 0;
                    if (progress.TryGetValue("size", out var sizeRaw))
                    {
                        var digits = LeadingDigits(sizeRaw);
                        if (digits.Length > 0 && long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedKb))
                        {
                            size = parsedKb * 1000;
                        }
                    }
                    onProgress(new ExportProgress(TimemarkToSeconds(time), size));
                }
            }
        });

        await WriteToStdinAsync(proc, palette);
        await proc.WaitForExitAsync();
        await stderrTask;

        var outputInfo = new FileInfo(output);
        ShellLog.Write($"[ffmpeg:export] exit={proc.ExitCode} outputExists={outputInfo.Exists} size={(outputInfo.Exists ? outputInfo.Length : 0)}");
        if (proc.ExitCode != 0)
        {
            ShellLog.Write($"[ffmpeg:export stderr]\n{stderrLog}");
        }

        if (_exportCancelled)
        {
            _exportCancelled = false;
            onCancelled();
        }
        else
        {
            onFinished(outputInfo.Exists ? outputInfo.Length : 0);
        }
    }

    // ----------------------------------------------------------------- cancel

    /// <summary>cancelProcess</summary>
    public void Cancel()
    {
        lock (_lock)
        {
            try { _current?.Kill(entireProcessTree: true); } catch { /* already gone */ }
        }
    }

    /// <summary>cancel_export — kills ffmpeg and optionally removes the partial output file.</summary>
    public async Task CancelExportAsync(string? removePartial)
    {
        _exportCancelled = true;
        Cancel();
        if (!string.IsNullOrEmpty(removePartial))
        {
            try { File.Delete(removePartial); } catch { /* ignore */ }
        }
        await Task.CompletedTask;
    }

    // ---------------------------------------------------------------- helpers

    private static ProcessStartInfo Build(string exe, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        return psi;
    }

    private static Process Start(string exe, params string[] args)
    {
        var proc = Process.Start(Build(exe, args))!;
        proc.EnableRaisingEvents = true;
        return proc;
    }

    private void KillCurrent()
    {
        lock (_lock)
        {
            try { _current?.Kill(entireProcessTree: true); } catch { /* ignore */ }
            _current = null;
        }
    }

    private static async Task WriteToStdinAsync(Process proc, byte[] bytes)
    {
        await proc.StandardInput.BaseStream.WriteAsync(bytes);
        await proc.StandardInput.BaseStream.FlushAsync();
        proc.StandardInput.Close();
    }

    // ---------------------------------------- progress parsing (ported helpers)

    private static double TimemarkToSeconds(string timemark)
    {
        if (double.TryParse(timemark, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        {
            return n;
        }
        if (!timemark.Contains(':') && timemark.Contains('.'))
        {
            return double.Parse(timemark, CultureInfo.InvariantCulture);
        }

        var parts = timemark.Split(':');
        var secs = double.Parse(parts[^1], CultureInfo.InvariantCulture);
        if (parts.Length >= 2) secs += double.Parse(parts[^2], CultureInfo.InvariantCulture) * 60;
        if (parts.Length >= 3) secs += double.Parse(parts[^3], CultureInfo.InvariantCulture) * 3600;
        return secs;
    }

    /// <summary>parseInt semantics: leading whitespace + digits, rest ignored.</summary>
    private static string LeadingDigits(string raw)
    {
        var trimmed = raw.TrimStart();
        var sb = new StringBuilder();
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 || ch != '+') break;
        }
        return sb.ToString();
    }

    private static Dictionary<string, string>? ParseProgressLine(string? line)
    {
        if (line is null) return null;
        // JS: line.replace(/=\s+/g, '=').trim() — collapses ALL whitespace after '='
        line = System.Text.RegularExpressions.Regex.Replace(line, @"=\s+", "=").Trim();
        var progress = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in line.Split(' '))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            progress[part[..idx]] = part[(idx + 1)..];
        }
        return progress.Count > 0 ? progress : null;
    }
}
