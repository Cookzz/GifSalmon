using System.ComponentModel;
using System.Runtime.CompilerServices;
using GifSalmon.Shell.Services;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace GifSalmon.Shell.ViewModels;

/// <summary>Ports the web UI's status.state machine (renderer.js).</summary>
public enum UiState
{
    Idle = 0,               // nothing selected
    Scanning = 1,           // probing input with ffprobe
    GeneratingPalette = 2,  // palettegen running
    GeneratingPreview = 3,  // thumbnail running
    Exporting = 4,          // gif export in progress
    Preview = 5,            // idle with preview visible
}

/// <summary>Typed settings — replaces the JS settings object passed over the bridge.</summary>
public sealed class GifSettings : INotifyPropertyChanged
{
    private string? _inputFilePath;
    private string? _inputFileName;
    private int _originalWidth = 320;
    private int _originalHeight = 240;
    private int _width = 320;
    private int _height = 240;
    private bool _lock = true;
    private int _fps = 24;
    private bool _dither = true;
    private int _ditherScale = 3;
    private string _statsMode = "diff";
    private string _diffMode = "rectangle";
    private int _colorCount = 256;
    private bool _alpha;
    private double _duration;
    private string? _frameRate;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? InputFilePath { get => _inputFilePath; set => Set(ref _inputFilePath, value); }
    public string? InputFileName { get => _inputFileName; set => Set(ref _inputFileName, value); }

    public int OriginalWidth { get => _originalWidth; set => Set(ref _originalWidth, value); }
    public int OriginalHeight { get => _originalHeight; set => Set(ref _originalHeight, value); }

    public int Width
    {
        get => _width;
        set { if (Set(ref _width, value)) OnDimensionsChanged(); }
    }

    public int Height
    {
        get => _height;
        set { if (Set(ref _height, value)) OnDimensionsChanged(); }
    }

    public bool Lock
    {
        get => _lock;
        set { if (Set(ref _lock, value)) OnDimensionsChanged(); }
    }

    /// <summary>Ratio label like "3:4" — faithful port of the web `ratio` filter
    /// (continued-fraction reduction of originalHeight/originalWidth).</summary>
    public string RatioLabel
    {
        get
        {
            const double tolerance = 1.0E-6;
            var x = (double)_originalHeight / _originalWidth;
            double h1 = 1, h2 = 0, k1 = 0, k2 = 1, b = x;
            do
            {
                var a = Math.Floor(b);
                (h1, h2) = (a * h1 + h2, h1);
                (k1, k2) = (a * k1 + k2, k1);
                if (double.IsInfinity(b - a)) break;
                b = 1 / (b - a);
            } while (Math.Abs(x - h1 / k1) > x * tolerance);
            return $"{h1:0}:{k1:0}";
        }
    }

    public int Fps { get => _fps; set => Set(ref _fps, value); }
    public bool Dither
    {
        get => _dither;
        set { if (Set(ref _dither, value)) OnPropertyChanged(nameof(DitherText)); }
    }
    public string DitherText => _dither ? "Yes" : "No";
    public int DitherScale { get => _ditherScale; set => Set(ref _ditherScale, value); }

    /// <summary>"full" | "diff" (palettegen stats_mode).</summary>
    public string StatsMode { get => _statsMode; set => Set(ref _statsMode, value); }

    /// <summary>"rectangle" | "none" (paletteuse diff_mode).</summary>
    public string DiffMode { get => _diffMode; set => Set(ref _diffMode, value); }

    public int StatsModeIndex
    {
        get => _statsMode == "full" ? 0 : 1;
        set => StatsMode = value == 0 ? "full" : "diff";
    }

    public int DiffModeIndex
    {
        get => _diffMode == "rectangle" ? 0 : 1;
        set => DiffMode = value == 0 ? "rectangle" : "none";
    }

    public int ColorCount { get => _colorCount; set => Set(ref _colorCount, value); }
    public bool Alpha { get => _alpha; set => Set(ref _alpha, value); }

    /// <summary>From ffprobe (used for export progress %).</summary>
    public double Duration { get => _duration; set => Set(ref _duration, value); }
    public string? FrameRate { get => _frameRate; set => Set(ref _frameRate, value); }

    public void ResetFromProbe(int width, int height, int fps, double duration, string? frameRate)
    {
        OriginalWidth = width;
        OriginalHeight = height;
        Width = width;
        Height = height;
        Fps = fps;
        Duration = duration;
        FrameRate = frameRate;
        OnPropertyChanged(nameof(RatioLabel));
    }

    /// <summary>widthChange: keep ratio while locked.</summary>
    public void ApplyLockedWidth(int width)
    {
        if (width <= 0 || !_lock) { Width = width; return; }
        var ratio = (double)_originalHeight / _originalWidth;
        Width = width;
        Height = (int)Math.Round(width * ratio);
    }

    /// <summary>heightChange: keep ratio while locked.</summary>
    public void ApplyLockedHeight(int height)
    {
        if (height <= 0 || !_lock) { Height = height; return; }
        var ratio = (double)_originalWidth / _originalHeight;
        Height = height;
        Width = (int)Math.Round(height * ratio);
    }

    private void OnDimensionsChanged() => OnPropertyChanged(nameof(RatioLabel));

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>The renderer's $scope, as a bindable view model.</summary>
public sealed class AppState : INotifyPropertyChanged
{
    private UiState _state = UiState.Idle;
    private double _framesCurrent;
    private double _framesMax = 100;
    private double _colorValue = 256;
    private double _exportProgress;
    private long _exportSize;
    private string? _exportPath;
    private bool _exportCanceling;
    private bool _hasInput;

    public GifSettings Settings { get; } = new();

    /// <summary>The ffmpeg binary set resolved at startup (set by MainWindow).</summary>
    public Services.FfmpegService? Ffmpeg { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged(nameof(State));
            RaiseStateFlags();
        }
    }

    public bool IsIdle => _state == UiState.Idle;
    public bool IsScanning => _state == UiState.Scanning;
    public bool IsGeneratingPalette => _state == UiState.GeneratingPalette;
    public bool IsGeneratingPreview => _state == UiState.GeneratingPreview;
    public bool IsExporting => _state == UiState.Exporting;
    public bool IsPreview => _state == UiState.Preview;
    public bool ShowLoader => _state is UiState.Scanning or UiState.GeneratingPalette or UiState.GeneratingPreview;
    public bool ShowExport => _state == UiState.Exporting;
    public bool ShowPreviewImage => _state == UiState.Preview;
    public bool ShowPaletteArea => _state != UiState.Idle;
    public bool ControlsEnabled => _state == UiState.Preview;
    public bool CanSelectInput => _state is UiState.Idle or UiState.Preview;

    public bool HasInput { get => _hasInput; set => Set(ref _hasInput, value); }

    public double FramesCurrent
    {
        get => _framesCurrent;
        set { if (Set(ref _framesCurrent, value)) OnPropertyChanged(nameof(FramesCurrentLabel)); }
    }

    public double FramesMax { get => _framesMax; set { if (Set(ref _framesMax, value)) OnPropertyChanged(nameof(FramesMaxLabel)); } }

    public string FramesCurrentLabel => $"{_framesCurrent:0.#}s";
    public string FramesMaxLabel => $"{_framesMax + 0.1:0.#}s";

    /// <summary>Syncs with Settings.ColorCount (the $watch pair).</summary>
    public double ColorValue
    {
        get => _colorValue;
        set
        {
            var v = (int)Math.Round(value);
            if (Set(ref _colorValue, v) && Settings.ColorCount != v)
            {
                Settings.ColorCount = v;
            }
        }
    }

    public double ExportProgress
    {
        get => _exportProgress;
        set
        {
            if (Set(ref _exportProgress, value))
            {
                OnPropertyChanged(nameof(ExportPercentLabel));
                OnPropertyChanged(nameof(ExportSizeLabel));
                OnPropertyChanged(nameof(IsExportComplete));
                OnPropertyChanged(nameof(IsExportRunning));
                OnPropertyChanged(nameof(ExportBarBrush));
            }
        }
    }
    public long ExportSize { get => _exportSize; set { if (Set(ref _exportSize, value)) OnPropertyChanged(nameof(ExportSizeLabel)); } }
    public string? ExportPath { get => _exportPath; set => Set(ref _exportPath, value); }
    public bool ExportCanceling
    {
        get => _exportCanceling;
        set
        {
            if (Set(ref _exportCanceling, value)) OnPropertyChanged(nameof(ExportPercentLabel));
        }
    }

    public bool IsExportComplete => _state == UiState.Exporting && _exportProgress >= 100;
    public bool IsExportRunning => _state == UiState.Exporting && _exportProgress < 100;

    public string LoaderTitle => _state switch
    {
        UiState.Scanning => "Scanning Input",
        UiState.GeneratingPalette => "Generating Color Palette",
        UiState.GeneratingPreview => "Generating Preview",
        _ => "",
    };

    public string ExportPercentLabel => _exportCanceling
        ? "Canceling"
        : _exportProgress >= 100 ? "Complete" : $"{_exportProgress:0.0}%";

    /// <summary>css .progress-fill: #46CCF3, turns #57A773 when finished.</summary>
    public Microsoft.UI.Xaml.Media.SolidColorBrush ExportBarBrush
        => _exportProgress >= 100
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x57, 0xA7, 0x73))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x46, 0xCC, 0xF3));

    public string ExportSizeLabel
    {
        get
        {
            var size = _exportSize > 0 ? FormatBytes(_exportSize) : "-";
            // web: "<size|filesize> <span ng-show=progress<100>written</span>"
            var duringExport = _state == UiState.Exporting && _exportProgress < 100;
            return duringExport ? $"{size} written" : size;
        }
    }

    // ------------------------------------------------------------- palette

    private byte[]? _palettePng;
    private Windows.UI.Color[]? _paletteFull;   // all 256 entries (padded black)
    private Windows.UI.Color[]? _paletteVisible; // first ColorCount entries (what the grid shows)

    /// <summary>True once a palette PNG has been generated for the current input.</summary>
    public bool HasPalette => _palettePng is not null;

    /// <summary>Raw palette PNG bytes (piped to ffmpeg's stdin for thumbnails/export).</summary>
    public byte[]? PalettePng => _palettePng;

    /// <summary>Decoded palette pixels (first ColorCount entries of the 16×16 PNG).</summary>
    public Windows.UI.Color[]? PaletteColors { get; private set; }

    public async Task SetPaletteAsync(byte[] png, int visibleCount)
    {
        _palettePng = png;
        OnPropertyChanged(nameof(HasPalette));
        OnPropertyChanged(nameof(PalettePng));

        try
        {
            using var ms = new MemoryStream(png);
            var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
            var frame = await decoder.GetFrameAsync(0);
            var data = await frame.GetPixelDataAsync();
            var px = data.DetachPixelData(); // BGRA

            var total = Math.Min(256, px.Length / 4);
            var full = new Windows.UI.Color[256];
            for (var i = 0; i < total; i++)
            {
                full[i] = Windows.UI.Color.FromArgb(255, px[i * 4 + 2], px[i * 4 + 1], px[i * 4]);
            }
            for (var i = total; i < 256; i++)
            {
                full[i] = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            }
            _paletteFull = full;
            PaletteVisibleCount = Math.Clamp(visibleCount, 0, 256);
            PaletteColors = full.Take(PaletteVisibleCount).ToArray();
        }
        catch (Exception ex)
        {
            ShellLog.Error("[palette] decode failed", ex);
            _paletteFull = null;
            PaletteColors = null;
        }
        OnPropertyChanged(nameof(PaletteColors));
    }

    /// <summary>Ports the web editor's put_palette: rebuild the 16×16 palette PNG
    /// from the edited pixel colors and swap it in (used for thumbnails/export).</summary>
    public async Task UpdatePaletteFromEditedColorsAsync(Windows.UI.Color[] edited)
    {
        var png = await EncodePaletteAsync(edited);
        _paletteFull = edited;
        _palettePng = png;
        OnPropertyChanged(nameof(HasPalette));
        OnPropertyChanged(nameof(PalettePng));

        PaletteVisibleCount = Math.Clamp(PaletteVisibleCount, 0, 256);
        PaletteColors = edited.Take(PaletteVisibleCount).ToArray();
        OnPropertyChanged(nameof(PaletteColors));
    }

    private static async Task<byte[]> EncodePaletteAsync(Windows.UI.Color[] colors)
    {
        var bgra = new byte[256 * 4];
        for (var i = 0; i < 256; i++)
        {
            var c = i < colors.Length ? colors[i] : Windows.UI.Color.FromArgb(255, 0, 0, 0);
            bgra[i * 4 + 0] = c.B;
            bgra[i * 4 + 1] = c.G;
            bgra[i * 4 + 2] = c.R;
            bgra[i * 4 + 3] = 255;
        }

        var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, 16, 16, 96.0, 96.0, bgra);
        await encoder.FlushAsync();

        var size = (uint)stream.Size;
        var bytes = new byte[size];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(size);
        reader.ReadBytes(bytes);
        return bytes;
    }

    public void ClearPalette()
    {
        _palettePng = null;
        _paletteFull = null;
        PaletteColors = null;
        OnPropertyChanged(nameof(HasPalette));
        OnPropertyChanged(nameof(PalettePng));
        OnPropertyChanged(nameof(PaletteColors));
    }

    public int PaletteVisibleCount { get; private set; }

    // ----------------------------------------------------------- thumbnail

    private byte[]? _thumbnailBytes;
    private ImageSource? _thumbnailImage;

    public byte[]? ThumbnailBytes
    {
        get => _thumbnailBytes;
        set
        {
            if (Set(ref _thumbnailBytes, value))
            {
                _ = SetThumbnailImageAsync(value);
            }
        }
    }

    public ImageSource? ThumbnailImage { get => _thumbnailImage; private set => Set(ref _thumbnailImage, value); }

    private async Task SetThumbnailImageAsync(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            ThumbnailImage = null;
            return;
        }
        try
        {
            var image = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            using var ms = new MemoryStream(bytes);
            await image.SetSourceAsync(ms.AsRandomAccessStream());
            ThumbnailImage = image;
        }
        catch (Exception ex)
        {
            ShellLog.Error("[thumbnail] decode failed", ex);
            ThumbnailImage = null;
        }
    }

    // ------------------------------------------------- stored settings snapshot

    /// <summary>Mirrors renderer's $scope.stored (updated after each successful thumbnail)
    /// — blur handlers compare against it to decide whether to refresh.</summary>
    public sealed record StoredSnapshot(int Width, int Height, int Fps);

    public StoredSnapshot? Stored { get; private set; }

    public void SetStoredSnapshot()
        => Stored = new StoredSnapshot(Settings.Width, Settings.Height, Settings.Fps);

    // -------------------------------------------------------------- resets

    public void ResetForNewInput()
    {
        FramesCurrent = 0;
        Settings.Duration = 0;
        ExportProgress = 0;
        ExportSize = 0;
        ExportPath = null;
        ExportCanceling = false;
        ThumbnailBytes = null;
        ClearPalette();
    }

    /// <summary>Web filesize filter port — also used by the installer's download readout.</summary>
    internal static string FormatBytes(long size)
    {
        // web filesize filter: (size / 1000^i).toFixed(2) * 1 → trims trailing zeros
        if (size <= 0) return "-";
        var i = (int)Math.Floor(Math.Log(size) / Math.Log(1000));
        var units = new[] { "B", "kB", "MB", "GB", "TB" };
        i = Math.Clamp(i, 0, units.Length - 1);
        var value = Math.Round(size / Math.Pow(1000, i), 2);
        return $"{value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} {units[i]}";
    }

    private void RaiseStateFlags()
    {
        foreach (var flag in new[]
        {
            nameof(IsIdle), nameof(IsScanning), nameof(IsGeneratingPalette),
            nameof(IsGeneratingPreview), nameof(IsExporting), nameof(IsPreview),
            nameof(ShowLoader), nameof(ShowExport), nameof(ShowPreviewImage),
            nameof(ShowPaletteArea), nameof(ControlsEnabled), nameof(CanSelectInput),
        })
        {
            OnPropertyChanged(flag);
        }
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
