// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides calculated layout values that the <see cref="ProgressRing"/> control template uses to size and
    /// position its animated ellipses.
    /// </summary>
    /// <remarks>
    /// These values are computed by the owning <see cref="ProgressRing"/> as its size changes and are exposed
    /// as read-only properties for the control template to bind against.
    /// </remarks>
    public sealed class ProgressRingTemplateSettings : DependencyObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressRingTemplateSettings"/> class.
        /// </summary>
        internal ProgressRingTemplateSettings()
        {
        }

        private static readonly DependencyPropertyKey EllipseDiameterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseDiameter),
                typeof(double),
                typeof(ProgressRingTemplateSettings),
                null);

        /// <summary>
        /// Identifies the <see cref="EllipseDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EllipseDiameterProperty = EllipseDiameterPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the diameter, in device-independent units, of each animated ellipse in the ring.
        /// </summary>
        /// <value>
        /// The diameter of a single ellipse.
        /// </value>
        public double EllipseDiameter
        {
            get => (double)this.GetValue(EllipseDiameterProperty);
            internal set => this.SetValue(EllipseDiameterPropertyKey, value);
        }

        private static readonly DependencyPropertyKey EllipseOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseOffset),
                typeof(Thickness),
                typeof(ProgressRingTemplateSettings),
                null);

        /// <summary>
        /// Identifies the <see cref="EllipseOffset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EllipseOffsetProperty = EllipseOffsetPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the margin used to position each ellipse relative to the center of the ring.
        /// </summary>
        /// <value>
        /// The offset applied to an ellipse so that it is placed along the circumference of the ring.
        /// </value>
        public Thickness EllipseOffset
        {
            get => (Thickness)this.GetValue(EllipseOffsetProperty);
            internal set => this.SetValue(EllipseOffsetPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MaxSideLengthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(MaxSideLength),
                typeof(double),
                typeof(ProgressRingTemplateSettings),
                null);

        /// <summary>
        /// Identifies the <see cref="MaxSideLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxSideLengthProperty = MaxSideLengthPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the maximum side length, in device-independent units, of the square that bounds the ring.
        /// </summary>
        /// <value>
        /// The smaller of the control's actual width and height, used to keep the ring circular.
        /// </value>
        public double MaxSideLength
        {
            get => (double)this.GetValue(MaxSideLengthProperty);
            internal set => this.SetValue(MaxSideLengthPropertyKey, value);
        }

    }
}
