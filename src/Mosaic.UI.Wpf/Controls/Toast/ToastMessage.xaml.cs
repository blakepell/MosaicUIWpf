/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single toast notification card.  Toasts either auto-dismiss after <see cref="Duration"/>
    /// elapses or, when no duration is provided, stay open until closed via the close button
    /// (or <see cref="Dismiss"/>).  Display is normally handled by <see cref="ToastManager"/>.
    /// </summary>
    public partial class ToastMessage : UserControl
    {
        /// <summary>
        /// Fluent (dark theme InfoBar) palette and Segoe MDL2 glyph for each severity.
        /// </summary>
        private static readonly Dictionary<ToastSeverity, (string Glyph, Color Accent, Color Background)> Palette = new()
        {
            [ToastSeverity.Success] = ("", Color.FromRgb(0x6C, 0xCB, 0x5F), Color.FromRgb(0x39, 0x3D, 0x1B)),
            [ToastSeverity.Info] = ("", Color.FromRgb(0x60, 0xCD, 0xFF), Color.FromRgb(0x2E, 0x2E, 0x2E)),
            [ToastSeverity.Warning] = ("", Color.FromRgb(0xFC, 0xE1, 0x00), Color.FromRgb(0x43, 0x35, 0x19)),
            [ToastSeverity.Error] = ("", Color.FromRgb(0xFF, 0x99, 0xA4), Color.FromRgb(0x44, 0x27, 0x26))
        };

        private DispatcherTimer? _dismissTimer;
        private bool _isDismissed;

        /// <summary>
        /// Designer only constructor.
        /// </summary>
        public ToastMessage() : this("Title", "Message", ToastSeverity.Info, null)
        {
        }

        /// <summary>
        /// A single toast notification card.
        /// </summary>
        /// <param name="title">The bolded title line.</param>
        /// <param name="message">The message body.</param>
        /// <param name="severity">The severity which determines the color scheme and icon.</param>
        /// <param name="duration">
        /// How long the toast stays open.  When null the toast stays open until the user
        /// closes it and a close button is displayed.
        /// </param>
        public ToastMessage(string title, string message, ToastSeverity severity, TimeSpan? duration)
        {
            InitializeComponent();

            this.Severity = severity;
            this.Duration = duration;

            titleTextBlock.Text = title;
            titleTextBlock.Visibility = string.IsNullOrWhiteSpace(title) ? Visibility.Collapsed : Visibility.Visible;
            messageTextBlock.Text = message;
            messageTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;

            // Timed toasts close themselves; only manual toasts get a close button.
            closeButton.Visibility = duration.HasValue ? Visibility.Collapsed : Visibility.Visible;

            var (glyph, accent, background) = Palette[severity];
            var accentBrush = new SolidColorBrush(accent);
            iconTextBlock.Text = glyph;
            iconTextBlock.Foreground = accentBrush;
            accentBar.Background = accentBrush;
            cardBorder.Background = new SolidColorBrush(background);
        }

        /// <summary>
        /// The severity of the toast.
        /// </summary>
        public ToastSeverity Severity { get; }

        /// <summary>
        /// How long the toast stays open, or null if it stays open until dismissed.
        /// </summary>
        public TimeSpan? Duration { get; }

        /// <summary>
        /// Raised once when the toast has been dismissed for any reason.
        /// </summary>
        public event EventHandler<ToastDismissedEventArgs>? Dismissed;

        /// <summary>
        /// Called by the manager when the toast has been added to the visual tree.  Runs the
        /// entrance animation and starts the auto-dismiss timer if a duration was provided.
        /// </summary>
        internal void BeginDisplay()
        {
            var slide = new TranslateTransform(0, 12);
            this.RenderTransform = slide;
            this.Opacity = 0;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });

            if (this.Duration.HasValue)
            {
                _dismissTimer = new DispatcherTimer { Interval = this.Duration.Value };
                _dismissTimer.Tick += (_, _) => this.Dismiss(ToastDismissReason.Timeout);
                _dismissTimer.Start();
            }
        }

        /// <summary>
        /// Dismisses the toast with a short exit animation and raises <see cref="Dismissed"/>.
        /// </summary>
        /// <param name="reason">Why the toast is being dismissed.</param>
        public void Dismiss(ToastDismissReason reason = ToastDismissReason.Programmatic)
        {
            if (_isDismissed)
            {
                return;
            }

            _isDismissed = true;
            _dismissTimer?.Stop();
            this.IsHitTestVisible = false;

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (_, _) => this.Dismissed?.Invoke(this, new ToastDismissedEventArgs(reason));
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Occurs when the close button is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> that contains the event data.</param>
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Dismiss(ToastDismissReason.ClosedByUser);
        }
    }
}
