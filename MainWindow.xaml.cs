using GifSalmon.Shell.Services;
using GifSalmon.Shell.ViewModels;
using GifSalmon.Shell.Views;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.UI;

namespace GifSalmon.Shell;

public sealed partial class MainWindow : Window
{
    private readonly SettingsStore _store = new();
    private readonly AppState _state = new();
    private AppSettings _settings;
    private bool _boundsApplied;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _store.Load();

        Title = "GifSalmon";
        ThemeTitleBar();
        SetWindowIcon();

        System.AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ShellLog.Error("unhandled exception", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            ShellLog.Error("unobserved task exception", e.Exception);

        ShellLog.Write($"=== launch, base dir: {AppContext.BaseDirectory}");

        ShowStartPage();
        ApplyWindowState();

        Activated += (_, args) =>
        {
            if (!_boundsApplied && args.WindowActivationState != WindowActivationState.Deactivated)
            {
                _boundsApplied = true;
                ApplyWindowState();
            }
        };
        Closed += OnClosed;
    }

    /// <summary>Same bootstrap flow as Electron's main.js: no ffmpeg → install page.
    /// GIFSALMON_FORCE_INSTALL=1 always shows the install page (E2E testing).</summary>
    private void ShowStartPage()
    {
        var forceInstall = Environment.GetEnvironmentVariable("GIFSALMON_FORCE_INSTALL") == "1";
        var ffmpegPath = forceInstall ? null : ResolveFfmpegPath();
        var ffprobePath = forceInstall ? null : ResolveFfprobePath();

        var ffmpeg = new FfmpegService { FfmpegPath = ffmpegPath, FfprobePath = ffprobePath };
        _state.Ffmpeg = ffmpeg;

        ShellLog.Write($"ffmpeg: '{ffmpegPath}' ffprobe: '{ffprobePath}'");

        if (forceInstall || !ffmpeg.HasFfmpeg || !ffmpeg.HasFfprobe)
        {
            var installPage = new InstallPage(progress => InstallFfmpegAsync(ffmpeg, progress));
            installPage.InstallSucceeded += () => Content = new EditorPage(_state, this, ffmpeg);
            Content = installPage;
            return;
        }

        Content = new EditorPage(_state, this, ffmpeg);
    }

    private async Task<bool> InstallFfmpegAsync(FfmpegService ffmpeg, IProgress<DownloadProgress> progress)
    {
        var result = await FfmpegInstaller.InstallAsync(progress);
        ffmpeg.FfmpegPath = result.Ffmpeg;
        ffmpeg.FfprobePath = result.Ffprobe;

        _settings.Ffmpeg = new FfmpegPaths { Ffmpeg = result.Ffmpeg, Ffprobe = result.Ffprobe };
        _store.Save(_settings);
        ShellLog.Write($"[install] source={result.Source} ffmpeg='{result.Ffmpeg}' ffprobe='{result.Ffprobe}'");
        return ffmpeg.HasFfmpeg && ffmpeg.HasFfprobe;
    }

    private string? ResolveFfmpegPath()
    {
        // Same resolution order as main.js: stored path first, then PATH.
        var stored = _settings.Ffmpeg?.Ffmpeg;
        return stored is { } f && File.Exists(f) ? f : FfmpegLocator.FindOnPath("ffmpeg");
    }

    private string? ResolveFfprobePath()
    {
        var stored = _settings.Ffmpeg?.Ffprobe;
        return stored is { } p && File.Exists(p) ? p : FfmpegLocator.FindOnPath("ffprobe");
    }

    private void SetWindowIcon()
    {
        try
        {
            var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(icon))
            {
                AppWindow.SetIcon(icon);
            }
        }
        catch
        {
            // Icon is a nice-to-have.
        }
    }

    private void ThemeTitleBar()
    {
        try
        {
            var tb = AppWindow.TitleBar;
            tb.BackgroundColor = Color.FromArgb(255, 0x1a, 0x1a, 0x1a);
            tb.ForegroundColor = Color.FromArgb(255, 0xee, 0xee, 0xee);
            tb.ButtonBackgroundColor = Color.FromArgb(255, 0x1a, 0x1a, 0x1a);
            tb.ButtonForegroundColor = Color.FromArgb(255, 0xee, 0xee, 0xee);
            tb.ButtonHoverBackgroundColor = Color.FromArgb(255, 0x33, 0x33, 0x33);
            tb.ButtonHoverForegroundColor = Color.FromArgb(255, 0xff, 0xff, 0xff);
        }
        catch
        {
            // Title bar theming is a nice-to-have.
        }
    }

    // --------------------------------------------------------- window state

    private void ApplyWindowState()
    {
        var w = _settings.Window;
        if (w.Width <= 0 || w.Height <= 0)
        {
            return;
        }

        try
        {
            var scale = ContentScale();
            AppWindow.MoveAndResize(new RectInt32(
                (int)(w.X * scale), (int)(w.Y * scale),
                (int)(w.Width * scale), (int)(w.Height * scale)));
        }
        catch
        {
            // Window bounds are a nice-to-have.
        }
    }

    private void OnClosed(object sender, object args)
    {
        SaveWindowState();
        _store.Save(_settings);
    }

    private void SaveWindowState()
    {
        try
        {
            var scale = ContentScale();
            var pos = AppWindow.Position;
            var size = AppWindow.Size;
            _settings.Window = new WindowState
            {
                X = (int)(pos.X / scale),
                Y = (int)(pos.Y / scale),
                Width = (int)(size.Width / scale),
                Height = (int)(size.Height / scale),
            };
        }
        catch
        {
            // Window bounds are a nice-to-have.
        }
    }

    private double ContentScale()
    {
        try
        {
            var scale = ((FrameworkElement?)Content)?.XamlRoot?.RasterizationScale ?? 1;
            return scale > 0 ? scale : 1;
        }
        catch
        {
            return 1;
        }
    }
}
