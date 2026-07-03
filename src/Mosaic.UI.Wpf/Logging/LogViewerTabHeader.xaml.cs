/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Header content that displays a log-viewer title with a numeric notification badge.
    /// </summary>
    public partial class LogViewerTabHeader : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(LogViewerTabHeader), new PropertyMetadata("Logs"));

        /// <summary>
        /// Gets or sets the header title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NotificationCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NotificationCountProperty = DependencyProperty.Register(
            nameof(NotificationCount), typeof(int), typeof(LogViewerTabHeader), new PropertyMetadata(default(int)));

        /// <summary>
        /// Gets or sets the notification count shown in the badge.
        /// </summary>
        public int NotificationCount
        {
            get => (int)GetValue(NotificationCountProperty);
            set => SetValue(NotificationCountProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogViewerTabHeader"/> class.
        /// </summary>
        public LogViewerTabHeader()
        {
            InitializeComponent();
        }
    }
}
