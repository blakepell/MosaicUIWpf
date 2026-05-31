/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="TextBlock"/> that will attempt to scale the font size so that all text fits.  <see cref="MinFontSize"/> and
    /// <see cref="MaxFontSize"/> are used as upper and lower boundaries.
    /// </summary>
    public class ScalingTextBlock : TextBlock
    {
        public static readonly DependencyProperty MinFontSizeProperty = DependencyProperty.Register(
            nameof(MinFontSize), typeof(double), typeof(ScalingTextBlock), new PropertyMetadata(12.0));

        /// <summary>
        /// The smallest font size that should be used.  The default is 12.
        /// </summary>
        public double MinFontSize
        {
            get => Convert.ToDouble(GetValue(MinFontSizeProperty));
            set => SetValue(MinFontSizeProperty, value);
        }

        public static readonly DependencyProperty MaxFontSizeProperty = DependencyProperty.Register(
            nameof(MaxFontSize), typeof(double), typeof(ScalingTextBlock), new PropertyMetadata(24.0));

        /// <summary>
        /// The largest font size that should be used.  The default is 24.
        /// </summary>
        public double MaxFontSize
        {
            get => Convert.ToDouble(GetValue(MaxFontSizeProperty));
            set => SetValue(MaxFontSizeProperty, value);
        }

        public static readonly DependencyProperty FontScaleIncrementProperty = DependencyProperty.Register(
            nameof(FontScaleIncrement), typeof(double), typeof(ScalingTextBlock), new PropertyMetadata(1.0));

        /// <summary>
        /// The value that should be used to increment scaling (e.g. 1 would attempt to lower by 1 font size when measuring text
        /// against the available width).  Lower numbers require more checks but choose a more precise size.  The default is 1.
        /// </summary>
        public double FontScaleIncrement
        {
            get => (double)GetValue(FontScaleIncrementProperty);
            set => SetValue(FontScaleIncrementProperty, value);
        }

        static ScalingTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScalingTextBlock), new FrameworkPropertyMetadata(typeof(ScalingTextBlock)));
        }

        /// <summary>
        /// OnInitialized event.  Code used to wire up events and dependency property watchers for
        /// the life cycle of this class.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Monitor for changes in the TextBlock's Text property.
            // Note: This will need to be removed when the control is unloaded to prevent a memory leak.
            DependencyPropertyDescriptor dp = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

            dp.AddValueChanged(this, (object a, EventArgs b) =>
            {
                this.ScaleFontSize();
            });

            dp.AddValueChanged(this, this.OnTextChanged);

            this.Loaded += Label_Loaded;
            this.Unloaded += Label_Unloaded;
        }

        /// <summary>
        /// Loaded event for TextBlock.  Used to properly scale the font for the initial load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_Loaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from loaded so we don't leak the event.
            this.Loaded -= this.Label_Loaded;

            // Scale the font for the initial display.
            this.ScaleFontSize();
        }

        /// <summary>
        /// Unloaded event for the TextBlock.  Used to cleanup any resources and handlers by the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from loaded so we don't leak the event.
            this.Unloaded -= this.Label_Unloaded;
            DependencyPropertyDescriptor dp = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            dp.RemoveValueChanged(this, this.OnTextChanged);
        }

        /// <summary>
        /// Event to scale the font when the Text property changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextChanged(object sender, EventArgs e)
        {
            this.ScaleFontSize();
        }

        /// <summary>
        /// Scales the font when the render size changes.
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.ScaleFontSize();
        }

        /// <summary>
        /// Scales the font size until the text in the TextBlock fits or hits the maximum/minimum font
        /// size value.
        /// </summary>
        private void ScaleFontSize()
        {
            this.Visibility = Visibility.Hidden;

            try
            {
                double increment = this.FontScaleIncrement;
                this.FontSize = this.MaxFontSize;

                // If the font size is already smaller than the minimum size then make it equal to
                // the minimum font size and then get out, no processing is required.
                if (this.FontSize < this.MinFontSize)
                {
                    this.Visibility = Visibility.Visible;
                    this.FontSize = this.MinFontSize;
                    return;
                }

                while (this.FontSize > this.MinFontSize & this.IsTextOverflow())
                {
                    this.FontSize -= increment;

                    // As soon as the font size dips below the MinFontSize set the FontSize
                    // to be equal to the MinFontSize then get out, no more processing is required.
                    if (this.FontSize < this.MinFontSize)
                    {
                        this.FontSize = this.MinFontSize;
                        this.Visibility = Visibility.Visible;
                        return;
                    }
                }
            }
            catch
            {
                // Stuff went bad, probably in IsTextOverflow.
                this.FontSize = this.MinFontSize;
            }

            this.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Whether the text of the <see cref="TextBlock"/> currently overflows the boundary.
        /// </summary>
        private bool IsTextOverflow()
        {
            try
            {
                this.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            }
            catch
            {
                return true;
            }

            return this.ActualWidth < this.DesiredSize.Width;
        }
    }
}