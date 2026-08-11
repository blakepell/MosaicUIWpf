/*
 * Originally based off (MIT): https://github.com/JMHeartley/WPF-Chart-Controls
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single slice rendered by a <see cref="PieChart"/>. Changing any property raises
    /// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>, which causes the owning chart
    /// to repaint. A chart bound to an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> of
    /// these therefore stays in sync with its data without an explicit refresh call.
    /// </summary>
    public partial class PieCategory : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PieCategory"/> class.
        /// </summary>
        public PieCategory()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PieCategory"/> class.
        /// </summary>
        /// <param name="name">The label shown for the slice in the legend.</param>
        /// <param name="value">The value the slice represents, relative to the other slices in the chart.</param>
        /// <param name="colorBrush">The brush used to fill the slice, or null to let the chart pick one.</param>
        public PieCategory(string name, double value, Brush? colorBrush = null)
        {
            this.Name = name;
            this.Value = value;
            this.ColorBrush = colorBrush;
        }

        /// <summary>
        /// Gets or sets the label shown for the slice in the legend.
        /// </summary>
        [Category("Common")]
        [Description("The label shown for the slice in the legend.")]
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value the slice represents. The slice's share of the pie is this value divided by
        /// the total of every value in the chart, so the values do not have to add up to any particular number.
        /// </summary>
        [Category("Common")]
        [Description("The value the slice represents, relative to the other slices in the chart.")]
        [ObservableProperty]
        public partial double Value { get; set; }

        /// <summary>
        /// Gets or sets the brush used to fill this slice. When null the owning chart assigns one from its
        /// palette based on the slice's position in the collection.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to fill this slice. Null lets the chart pick one from its palette.")]
        [ObservableProperty]
        public partial Brush? ColorBrush { get; set; }

        /// <summary>
        /// Gets or sets a custom object for this particular slice.
        /// </summary>
        [Category("Common")]
        [Description("A custom object associated with this slice.")]
        [ObservableProperty]
        public partial object? Tag { get; set; }

        /// <summary>
        /// Backing field for <see cref="Percentage"/>.
        /// </summary>
        private double _percentage;

        /// <summary>
        /// Gets the slice's share of the pie, from 0 to 100. This is calculated by the owning
        /// <see cref="PieChart"/> from <see cref="Value"/> and the total of every value in the chart, so it is
        /// only meaningful once the category has been added to a chart.
        /// </summary>
        [Browsable(false)]
        public double Percentage
        {
            get => _percentage;
            internal set => this.SetProperty(ref _percentage, value);
        }

        /// <summary>
        /// Gets the brush the owning <see cref="PieChart"/> actually painted this slice with, which is
        /// <see cref="ColorBrush"/> when one was supplied and a palette color otherwise. The legend binds to
        /// this so its swatch matches the slice.
        /// </summary>
        [Browsable(false)]
        public Brush? EffectiveBrush => this.ColorBrush ?? _paletteBrush;

        /// <summary>
        /// Backing field for the palette color assigned by the owning chart.
        /// </summary>
        private Brush? _paletteBrush;

        /// <summary>
        /// Assigns the fallback color the owning chart picked for this slice.
        /// </summary>
        /// <param name="brush">The palette brush to fall back to when <see cref="ColorBrush"/> is null.</param>
        internal void SetPaletteBrush(Brush? brush)
        {
            if (ReferenceEquals(_paletteBrush, brush))
            {
                return;
            }

            _paletteBrush = brush;
            this.OnPropertyChanged(nameof(this.EffectiveBrush));
        }

        // Keeps the legend's swatch in step when a slice is given (or loses) an explicit color.
        partial void OnColorBrushChanged(Brush? value)
        {
            this.OnPropertyChanged(nameof(this.EffectiveBrush));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Name}: {this.Value}";
        }
    }

    /// <summary>
    /// Carries the <see cref="PieCategory"/> whose slice was clicked to handlers of
    /// <see cref="PieChart.SliceClicked"/>.
    /// </summary>
    public class PieCategoryEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PieCategoryEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The chart raising the event.</param>
        /// <param name="category">The category backing the slice that was clicked.</param>
        public PieCategoryEventArgs(RoutedEvent routedEvent, object source, PieCategory category)
            : base(routedEvent, source)
        {
            this.Category = category;
        }

        /// <summary>
        /// The category backing the slice that was clicked.
        /// </summary>
        public PieCategory Category { get; }
    }
}
