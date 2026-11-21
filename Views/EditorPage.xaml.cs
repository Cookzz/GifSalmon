using System.ComponentModel;
using GifSalmon.Shell.Controls;
using GifSalmon.Shell.Services;
using GifSalmon.Shell.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace GifSalmon.Shell.Views;

public sealed partial class EditorPage : Page
{
    private readonly DialogService _dialogs;
    private readonly FfmpegService _ffmpeg;
    private readonly DispatcherQueue _dq;
    private DispatcherQueueTimer? _colorsDebounce;
    private int _requestSeq;

    public AppState State { get; }

    public EditorPage(AppState state, Window window, FfmpegService ffmpeg)
    {
        State = state;
        _dialogs = new DialogService(window);
        _ffmpeg = ffmpeg;
        _dq = DispatcherQueue.GetForCurrentThread();

        InitializeComponent();
        WireEvents();
        State.PropertyChanged += OnStatePropertyChanged;
        PaletteControl.SetColors(State.PaletteColors, State.PaletteVisibleCount);
        // css .progress/.progress-info: width 50vw — a FIXED width so the bar
        // doesn't breathe with the text below it
        PreviewPane.SizeChanged += (s, e) => ExportPanelHost.Width = e.NewSize.Width * 0.5;
    }

    private void WireEvents()
    {
        SelectFileButton.Click += OnSelectFile;
        ChangeFileButton.Click += OnSelectFile;

        WidthBox.LostFocus += OnDimensionsCommitted;
        HeightBox.LostFocus += OnDimensionsCommitted;
        FpsBox.LostFocus += OnDimensionsCommitted;

        DitherCheck.Checked += OnThumbnailSettingChanged;
        DitherCheck.Unchecked += OnThumbnailSettingChanged;
        DitherGridCombo.SelectionChanged += OnThumbnailSettingChanged;
        PaletteModeCombo.SelectionChanged += OnPaletteSettingChanged;
        StatsModeCombo.SelectionChanged += OnPaletteSettingChanged;

        ColorsSlider.ValueChanged += OnColorsValueChanged;
        ColorsBox.LostFocus += OnColorsCommitted;

        ScrubSlider.ValueChanged += OnScrubChanged;
        CancelPaletteButton.Click += OnCancelPalette;

        ExportButton.Click += OnExport;
        CancelExportButton.Click += OnCancelExport;
        ShowInFolderButton.Click += OnShowInFolder;
        PreviewExportButton.Click += OnPreviewExport;
        DoneButton.Click += (_, _) => ExportDone();
        PaletteControl.PaletteEdited += OnPaletteEdited;
    }

    // ------------------------------------------------------------ file input

    private async void OnSelectFile(object sender, RoutedEventArgs e)
    {
        var file = await _dialogs.OpenAsync();
        if (file is null) return;
        await SelectInputAsync(file.Path, file.Name);
    }

    private async Task SelectInputAsync(string path, string name)
    {
        State.Settings.InputFilePath = path;
        State.Settings.InputFileName = name;
        State.ResetForNewInput();
        State.HasInput = true;
        State.State = UiState.Scanning;

        var token = ++_requestSeq;
        var probe = await _ffmpeg.ProbeAsync(path);
        if (token != _requestSeq) return;

        if (probe is null)
        {
            ShellLog.Write($"[probe] failed for '{path}'");
            State.State = UiState.Idle;
            return;
        }

        // renderer: frames.max = (floor(duration*10)/10).toFixed(1) - 0.1
        var max = Math.Floor(probe.Duration * 10) / 10.0;
        State.FramesMax = Math.Round(max, 1) - 0.1;

        var fps = ParseFps(probe.FrameRate) ?? 24;
        State.Settings.ResetFromProbe(probe.Width, probe.Height, fps, probe.Duration, probe.FrameRate);

        State.State = UiState.GeneratingPalette;
        _ = RefreshPaletteAsync(token);
    }

    /// <summary>parseInt of the framerate filter output, e.g. "30000/1001" → 29.</summary>
    private static int? ParseFps(string? frameRate)
    {
        if (string.IsNullOrEmpty(frameRate)) return null;
        var parts = frameRate.Split('/');
        if (!int.TryParse(parts[0], out var num) || num == 0) return null;
        var den = parts.Length > 1 && int.TryParse(parts[1], out var den2) && den2 != 0 ? den2 : 1;
        return (int)Math.Truncate((double)num / den);
    }

    // ------------------------------------------------- palette / thumbnail loop

    private async Task RefreshPaletteAsync(int token)
    {
        if (!State.HasInput || State.Settings.InputFilePath is null) return;

        State.State = UiState.GeneratingPalette;
        _ffmpeg.Cancel();

        var png = await _ffmpeg.PaletteAsync(State.Settings.InputFilePath, State.Settings);
        if (token != _requestSeq) return;

        if (png is null)
        {
            ShellLog.Write("[palette] generation failed");
            State.State = UiState.Idle;
            return;
        }

        await State.SetPaletteAsync(png, State.Settings.ColorCount);
        if (token != _requestSeq) return;

        State.State = UiState.GeneratingPreview;
        _ = RefreshThumbnailAsync(token);
    }

    private async Task RefreshThumbnailAsync(int token)
    {
        if (!State.HasInput || State.Settings.InputFilePath is null || !State.HasPalette) return;

        State.State = UiState.GeneratingPreview;
        _ffmpeg.Cancel();

        var gif = await _ffmpeg.ThumbnailAsync(
            State.Settings.InputFilePath, State.PalettePng!, State.FramesCurrent, State.Settings);
        if (token != _requestSeq) return;

        if (gif is null)
        {
            ShellLog.Write("[thumbnail] generation failed");
            State.State = State.ThumbnailBytes is null ? UiState.Idle : UiState.Preview;
            return;
        }

        State.ThumbnailBytes = gif;
        State.SetStoredSnapshot();
        State.State = UiState.Preview;
    }

    // ---------------------------------------------------------------- events

    private void OnDimensionsCommitted(object sender, RoutedEventArgs e)
    {
        var stored = State.Stored;
        if (stored is null) return;

        // widthChange/heightChange: while locked, derive the other dimension
        // from whichever one the user changed.
        if (State.Settings.Lock)
        {
            var width = (int)Math.Round(WidthBox.Value);
            var height = (int)Math.Round(HeightBox.Value);
            if (width != stored.Width)
            {
                State.Settings.ApplyLockedWidth(width);
            }
            else if (height != stored.Height)
            {
                State.Settings.ApplyLockedHeight(height);
            }
        }

        RefreshThumbnailIfStoredDiffers();
    }

    private void RefreshThumbnailIfStoredDiffers()
    {
        if (State.State != UiState.Preview || !State.HasPalette || State.Stored is null) return;
        var s = State.Settings;
        if (s.Width == State.Stored.Width && s.Height == State.Stored.Height && s.Fps == State.Stored.Fps)
        {
            return;
        }
        var token = ++_requestSeq;
        _ = RefreshThumbnailAsync(token);
    }

    private void OnThumbnailSettingChanged(object sender, RoutedEventArgs e) => RefreshThumbnailInPreview();
    private void OnThumbnailSettingChanged(object sender, SelectionChangedEventArgs e) => RefreshThumbnailInPreview();
    private void OnPaletteSettingChanged(object sender, SelectionChangedEventArgs e) => RefreshPaletteInPreview();

    private void RefreshThumbnailInPreview()
    {
        if (State.State != UiState.Preview || !State.HasPalette) return;
        var token = ++_requestSeq;
        _ = RefreshThumbnailAsync(token);
    }

    private void RefreshPaletteInPreview()
    {
        if (!State.HasPalette) return;
        var token = ++_requestSeq;
        _ = RefreshPaletteAsync(token);
    }

    private void OnScrubChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        // web: ng-model="frames.current" on the slider input
        State.FramesCurrent = e.NewValue;

        // renderer: $watch('frames.current') → refreshThumbnail only in state 5
        RefreshThumbnailInPreview();
    }

    private void OnColorsValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        => OnColorsChanged(e.NewValue);

    private void OnColorsCommitted(object sender, RoutedEventArgs e)
        => OnColorsChanged(ColorsBox.Value);

    private void OnColorsChanged(double value)
    {
        State.ColorValue = value; // syncs Settings.ColorCount (the $watch pair)

        if (!State.HasPalette) return;

        // Web commits on mouseup/blur; a short debounce approximates that
        // without regenerating a palette per pixel of drag.
        if (_colorsDebounce is null)
        {
            _colorsDebounce = _dq.CreateTimer();
            _colorsDebounce.IsRepeating = false;
            _colorsDebounce.Tick += (_, _) =>
            {
                var token = ++_requestSeq;
                _ = RefreshPaletteAsync(token);
            };
        }
        _colorsDebounce.Stop();
        _colorsDebounce.Interval = TimeSpan.FromMilliseconds(400);
        _colorsDebounce.Start();
    }

    private void OnCancelPalette(object sender, RoutedEventArgs e)
    {
        // renderer cancel(true): kill ffmpeg, reset to a clean idle state
        _ffmpeg.Cancel();
        _requestSeq++; // invalidate any in-flight continuations
        State.ResetForNewInput();
        State.HasInput = false;
        State.State = UiState.Idle;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppState.PaletteColors))
        {
            PaletteControl.SetColors(State.PaletteColors, State.PaletteVisibleCount);
        }
    }

    // ---------------------------------------------------------- pixel editor

    private async void OnPaletteEdited(Windows.UI.Color[] colors)
    {
        // web: put_palette → colorPaletteChange → refreshThumbnail
        var token = ++_requestSeq;
        await State.UpdatePaletteFromEditedColorsAsync(colors);
        if (token != _requestSeq) return;
        _ = RefreshThumbnailAsync(token);
    }

    // --------------------------------------------------------------- export

    private async void OnExport(object sender, RoutedEventArgs e)
    {
        if (!State.HasPalette || State.Settings.InputFilePath is null) return;

        var input = State.Settings.InputFilePath;

        // renderer export(): defaultPath = <input dir>/<name>.gif
        var dir = Path.GetDirectoryName(input) ?? "";
        var name = Path.GetFileNameWithoutExtension(input);
        var suggested = Path.Combine(dir, name + ".gif");

        var output = await _dialogs.SaveAsync(suggested);
        if (output is null) return;

        State.ExportPath = output;
        State.ExportProgress = 0;
        State.ExportSize = 0;
        State.ExportCanceling = false;
        State.State = UiState.Exporting;

        var token = ++_requestSeq;
        var palette = State.PalettePng!;
        var duration = State.Settings.Duration;

        _ = Task.Run(async () =>
        {
            await _ffmpeg.ExportAsync(
                input,
                output,
                palette,
                State.Settings,
                p => _dq.TryEnqueue(() =>
                {
                    if (token != _requestSeq) return;
                    // renderer: progress.sec / probe.duration * 100
                    State.ExportProgress = duration > 0 ? p.Sec / duration * 100.0 : 0;
                    State.ExportSize = p.Size;
                }),
                size => _dq.TryEnqueue(() =>
                {
                    if (token != _requestSeq) return;
                    State.ExportProgress = 100;
                    State.ExportSize = size;
                }),
                () => _dq.TryEnqueue(() =>
                {
                    if (token != _requestSeq) return;
                    State.ExportCanceling = false;
                    ExportDone();
                }));
        });
    }

    private async void OnCancelExport(object sender, RoutedEventArgs e)
    {
        State.ExportCanceling = true;
        await _ffmpeg.CancelExportAsync(State.ExportPath);
        // onCancelled callback finishes the reset (ExportDone)
    }

    private void ExportDone()
    {
        // renderer exportDone(): back to preview with a cleared export status
        State.ExportProgress = 0;
        State.ExportSize = 0;
        State.ExportPath = null;
        State.State = UiState.Preview;
    }

    private void OnShowInFolder(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(State.ExportPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = false,
                Arguments = $"/select,\"{State.ExportPath}\"",
            });
        }
        catch (Exception ex)
        {
            ShellLog.Error("[export] show in folder", ex);
        }
    }

    private void OnPreviewExport(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(State.ExportPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(State.ExportPath)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            ShellLog.Error("[export] preview", ex);
        }
    }
}
