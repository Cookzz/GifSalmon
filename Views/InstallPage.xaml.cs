using System.ComponentModel;
using System.Runtime.CompilerServices;
using GifSalmon.Shell.Services;
using GifSalmon.Shell.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GifSalmon.Shell.Views;

public sealed partial class InstallPage : Page, INotifyPropertyChanged
{
    private readonly Func<IProgress<DownloadProgress>, Task<bool>> _install;
    private bool _installing;

    // windowed speed samples: (timestamp, received bytes)
    private readonly List<(DateTime Time, long Bytes)> _speedSamples = new();
    private DateTime _lastUiUpdate = DateTime.MinValue;
    private long _lastReceived;

    /// <summary>Raised after a successful install so the shell can show the editor.</summary>
    public event Action? InstallSucceeded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Installing { get => _installing; set { if (Set(ref _installing, value)) OnPropertyChanged(nameof(NotInstalling)); } }
    public bool NotInstalling => !_installing;

    public InstallPage(Func<IProgress<DownloadProgress>, Task<bool>> install)
    {
        _install = install;
        InitializeComponent();
    }

    private void OnQuit(object sender, RoutedEventArgs e) => Application.Current.Exit();

    private async void OnInstall(object sender, RoutedEventArgs e)
    {
        Installing = true;
        ResetProgressUi();

        var ok = false;
        try
        {
            // Progress<T> captures this UI thread — reports arrive here directly
            ok = await _install(new Progress<DownloadProgress>(OnDownloadProgress));
        }
        catch (Exception ex)
        {
            ShellLog.Error("[install_ffmpeg]", ex);
        }
        Installing = false;
        if (ok)
        {
            InstallSucceeded?.Invoke();
        }
    }

    private void ResetProgressUi()
    {
        _speedSamples.Clear();
        _lastUiUpdate = DateTime.MinValue;
        _lastReceived = 0;
        InstallBar.Value = 0;
        InstallBar.IsIndeterminate = true;
        InstallProgressText.Text = "";
    }

    private void OnDownloadProgress(DownloadProgress p)
    {
        // new file (ffbinaries downloads two) → reset the window
        if (p.Received + 1024 * 1024 < _lastReceived)
        {
            _speedSamples.Clear();
            _lastUiUpdate = DateTime.MinValue;
        }
        _lastReceived = p.Received;

        var now = DateTime.UtcNow;
        _speedSamples.Add((now, p.Received));
        while (_speedSamples.Count > 2 && (now - _speedSamples[0].Time).TotalSeconds > 4)
        {
            _speedSamples.RemoveAt(0);
        }

        var isFinal = p.Total > 0 && p.Received >= p.Total;
        if (!isFinal && (now - _lastUiUpdate).TotalMilliseconds < 150)
        {
            return; // throttle repaints on fast CDNs
        }
        _lastUiUpdate = now;

        // windowed speed over the last ~4s
        var first = _speedSamples[0];
        var elapsed = (now - first.Time).TotalSeconds;
        var speed = elapsed > 0.25 ? (p.Received - first.Bytes) / elapsed : 0;

        InstallBar.IsIndeterminate = p.Total <= 0;
        if (p.Total > 0)
        {
            InstallBar.Value = (double)p.Received / p.Total * 100;
        }

        var speedText = speed > 0 ? $" ({AppState.FormatBytes((long)speed)}/s)" : "";
        InstallProgressText.Text = p.Total > 0
            ? $"{AppState.FormatBytes(p.Received)} / {AppState.FormatBytes(p.Total)}{speedText}"
            : $"{AppState.FormatBytes(p.Received)}{speedText}";
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
