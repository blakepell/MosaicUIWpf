// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A progress ring component to indicate that a long running process is occurring.
    /// </summary>
    public class ProgressRing : Control
    {
        const string ACTIVE_STATE_NAME = "Active";
        const string INACTIVE_STATE_NAME = "Inactive";
        const string SMALL_STATE_NAME = "Small";
        const string LARGE_STATE_NAME = "Large";

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressRing), new FrameworkPropertyMetadata(typeof(ProgressRing)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressRing"/> class.
        /// </summary>
        public ProgressRing()
        {
            this.SetValue(TemplateSettingsPropertyKey, new ProgressRingTemplateSettings());

            this.SizeChanged += this.OnSizeChanged;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="ProgressRing"/> is animating and visible.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the ring is active and animating; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool IsActive
        {
            get => (bool)this.GetValue(IsActiveProperty);
            set => this.SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(OnIsActivePropertyChanged));

        private static void OnIsActivePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((ProgressRing)sender).OnIsActivePropertyChanged(args);
        }

        private static readonly DependencyPropertyKey TemplateSettingsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TemplateSettings),
                typeof(ProgressRingTemplateSettings),
                typeof(ProgressRing),
                null);

        /// <summary>
        /// Identifies the <see cref="TemplateSettings"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TemplateSettingsProperty =
            TemplateSettingsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the calculated layout values used by the control template to size and position the animated ellipses.
        /// </summary>
        /// <value>
        /// The settings that the template binds to for ellipse diameter, offset, and overall side length.
        /// </value>
        public ProgressRingTemplateSettings TemplateSettings => (ProgressRingTemplateSettings)this.GetValue(TemplateSettingsProperty);

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ChangeVisualState();
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ApplyTemplateSettings();
            this.ChangeVisualState();
        }

        void OnIsActivePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            this.ChangeVisualState();
        }

        void ChangeVisualState()
        {
            VisualStateManager.GoToState(this, this.IsActive ? ACTIVE_STATE_NAME : INACTIVE_STATE_NAME, true);
            VisualStateManager.GoToState(this, this.TemplateSettings.MaxSideLength < 60 ? SMALL_STATE_NAME : LARGE_STATE_NAME, true);
        }

        void ApplyTemplateSettings()
        {
            var templateSettings = this.TemplateSettings;

            var (width, diameterValue, anchorPoint) = calcSettings();
            (double, double, double) calcSettings()
            {
                if (this.ActualWidth != 0)
                {
                    double width = Math.Min(this.ActualWidth, this.ActualHeight);
                    double diameterAdditive;
                    {
                        double init()
                        {
                            if (width <= 40.0)
                            {
                                return 1.0;
                            }

                            return 0.0;
                        }

                        diameterAdditive = init();
                    }

                    double diameterValue = (width * 0.1) + diameterAdditive;
                    double anchorPoint = (width * 0.5) - diameterValue;
                    return (width, diameterValue, anchorPoint);
                }

                return (0.0, 0.0, 0.0);
            }
            ;

            templateSettings.EllipseDiameter = diameterValue;

            var thicknessEllipseOffset = new Thickness(0, anchorPoint, 0, 0);

            templateSettings.EllipseOffset = thicknessEllipseOffset;
            templateSettings.MaxSideLength = width;
        }
    }
}
