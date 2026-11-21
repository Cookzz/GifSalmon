using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Streams;
using Windows.UI;

namespace GifSalmon.Shell.Controls;

/// <summary>
/// The 16×16 pixel palette with the click-to-edit flow from the web app:
/// tapping a swatch opens a ColorPicker flyout; Ok mutates the palette and
/// raises PaletteEdited (the caller re-encodes the PNG and refreshes).
/// </summary>
public sealed partial class PixelPalette : UserControl
{
    private readonly Border[] _swatches = new Border[256];
    private Windows.UI.Color[]? _colors;
    private int _visibleCount;
    private int _selectedIndex = -1;

    private readonly Flyout _flyout;
    private readonly ColorPicker _picker;
    private readonly TextBox _hexBox;

    /// <summary>Raised with the full 256-color palette after a swatch edit.</summary>
    public event Action<Windows.UI.Color[]>? PaletteEdited;

    public PixelPalette()
    {
        InitializeComponent();

        for (var i = 0; i < _swatches.Length; i++)
        {
            var index = i;
            _swatches[i] = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
                Tag = index,
                IsTapEnabled = true,
            };
            _swatches[i].Tapped += OnSwatchTapped;
        }
        Swatches.ItemsSource = _swatches;

        _picker = new ColorPicker
        {
            IsAlphaEnabled = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = true,
            IsHexInputVisible = false,
            IsAlphaSliderVisible = false,
            IsColorPreviewVisible = true,
        };
        _hexBox = new TextBox { PlaceholderText = "RRGGBB", MaxLength = 6 };
        _hexBox.TextChanged += OnHexChanged;

        var okButton = new Button { Content = "Ok" };
        okButton.Click += OnOkClick;
        var cancelButton = new Button { Content = "Cancel" };
        cancelButton.Click += (_, _) => _flyout.Hide();

        var content = new StackPanel { Spacing = 8, MinWidth = 300 };
        content.Children.Add(_picker);
        content.Children.Add(_hexBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(okButton);
        content.Children.Add(buttons);

        _flyout = new Flyout { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
    }

    /// <summary>Shows the first <paramref name="visibleCount" /> swatches with real
    /// colors; the rest collapse (mirrors the web directive's maxColors).</summary>
    public void SetColors(Windows.UI.Color[]? colors, int visibleCount)
    {
        _colors = colors;
        _visibleCount = Math.Clamp(visibleCount, 0, 256);
        RefreshSwatches();
    }

    private void RefreshSwatches()
    {
        for (var i = 0; i < _swatches.Length; i++)
        {
            var swatch = _swatches[i];
            if (i >= _visibleCount || _colors is null || i >= _colors.Length)
            {
                swatch.Visibility = Visibility.Collapsed;
                continue;
            }
            swatch.Visibility = Visibility.Visible;
            swatch.Background = new SolidColorBrush(_colors[i]);
        }
    }

    private void OnSwatchTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_colors is null) return;
        if (sender is Border { Tag: int index } && index < _colors.Length)
        {
            _selectedIndex = index;
            _picker.Color = _colors[index];
            _hexBox.Text = ToHex(_colors[index]);
            _flyout.ShowAt(_swatches[index]);
        }
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_hexBox.Text.Length != 6) return;
        if (!int.TryParse(_hexBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var value)) return;
        _picker.Color = Color.FromArgb(
            255,
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF));
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex >= 0 && _colors is not null && _selectedIndex < _colors.Length)
        {
            _colors[_selectedIndex] = _picker.Color;
            RefreshSwatches();
            PaletteEdited?.Invoke(_colors);
        }
        _flyout.Hide();
    }

    private static string ToHex(Windows.UI.Color c)
        => $"{c.R:X2}{c.G:X2}{c.B:X2}";
}
