using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MosaicWpfDemo.Controls
{
    public partial class ColorThemeGallery : UserControl
    {
        public ObservableCollection<PaletteRow> Palettes { get; } = new();

        private static readonly string[] PaletteNames =
        {
            "Neutral", "Blue", "Indigo", "Purple", "Pink", "Red",
            "Orange", "Yellow", "Lime", "Green", "Teal", "Cyan"
        };

        private static readonly int[] Shades = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900 };

        public ColorThemeGallery()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Build rows only once (safe re-entry)
            if (Palettes.Count > 0) return;

            foreach (var palette in PaletteNames)
            {
                var row = new PaletteRow(palette);
                foreach (var shade in Shades)
                {
                    var colorKey = $"{palette}.{shade}.Color";
                    var brushKey = $"{palette}.{shade}.Brush";

                    // Try to get the color and brush from the resource system
                    var colorObj = TryFindResource(colorKey) ?? Application.Current.TryFindResource(colorKey);
                    var brushObj = TryFindResource(brushKey) ?? Application.Current.TryFindResource(brushKey);

                    // Derive missing pieces if possible
                    Color? color = null;
                    SolidColorBrush? brush = null;

                    if (colorObj is Color c) color = c;
                    if (brushObj is SolidColorBrush b) brush = b;

                    if (color == null && brush != null) color = brush.Color;
                    if (brush == null && color != null) brush = new SolidColorBrush(color.Value);

                    if (color == null || brush == null)
                        continue; // skip if we couldn't resolve

                    var hex = ColorToHex(color.Value);
                    var overlayFg = GetContrastingForeground(color.Value);

                    row.Shades.Add(new SwatchItem
                    {
                        Palette = palette,
                        Shade = shade,
                        Color = color.Value,
                        Brush = brush,
                        Hex = hex,
                        DisplayKey = $"{palette}.{shade}.Brush",
                        CopyTooltip = $"Copy {{DynamicResource {palette}.{shade}.Brush}}",
                        OverlayForeground = overlayFg,
                        CopyCommand = new RelayCommand(_ =>
                        {
                            var snippet = "{DynamicResource " + $"{palette}.{shade}.Brush" + "}";
                            Clipboard.SetText(snippet);
                            // Optional: small notice
                            ShowCopiedBalloon($"{palette}.{shade}.Brush");
                        })
                    });
                }

                if (row.Shades.Count > 0)
                    Palettes.Add(row);
            }
        }

        private static string ColorToHex(Color c) =>
            $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        // Perceived luminance: ITU-R BT.709
        private static Brush GetContrastingForeground(Color c)
        {
            double luminance = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
            return luminance < 0.55 ? Brushes.White : Brushes.Black;
        }

        private void ShowCopiedBalloon(string key)
        {
            // Lightweight, non-invasive “copied” notice via a ToolTip on the control itself.
            var tip = new ToolTip
            {
                Content = $"Copied {{DynamicResource {key}}}",
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                IsOpen = true,
                StaysOpen = false
            };

            // Auto-close shortly after opening
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1200)
            };
            timer.Tick += (_, __) =>
            {
                timer.Stop();
                tip.IsOpen = false;
            };
            timer.Start();
        }
    }

    public class PaletteRow
    {
        public string Name { get; }
        public ObservableCollection<SwatchItem> Shades { get; } = new();
        public PaletteRow(string name) => Name = name;
    }

    public class SwatchItem
    {
        public string Palette { get; set; } = "";
        public int Shade { get; set; }
        public string DisplayKey { get; set; } = "";
        public string Hex { get; set; } = "";
        public Color Color { get; set; }
        public SolidColorBrush Brush { get; set; } = Brushes.Transparent;
        public Brush OverlayForeground { get; set; } = Brushes.White;
        public string CopyTooltip { get; set; } = "";
        public ICommand CopyCommand { get; set; } = default!;
    }

    /// <summary>Simple ICommand implementation.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
