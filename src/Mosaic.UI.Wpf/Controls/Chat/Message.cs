/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Cache;
using Mosaic.UI.Wpf.Json;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a message exchanged in a communication system, including its content, sender, direction, and
    /// timestamp.
    /// </summary>
    public partial class Message : ObservableObject
    {
        /// <summary>
        /// Gets or sets the direction of the message.
        /// </summary>
        [ObservableProperty]
        private MessageDirection _direction;

        /// <summary>
        /// Gets or sets the direction of the previous message in a conversation.
        /// </summary>
        [ObservableProperty]
        private MessageDirection _previousMessageDirection;

        /// <summary>
        /// Gets or sets the sender's friendly name (e.g. username or display name).
        /// </summary>
        [ObservableProperty]
        private string? _from;

        /// <summary>
        /// Gets or sets the text value of the message.
        /// </summary>
        [ObservableProperty]
        private string? _text;

        /// <summary>
        /// Gets or sets the timestamp associated with this instance.
        /// </summary>
        [ObservableProperty]
        private DateTime _timestamp;

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        /// <remarks>
        /// System.Text.Json converter provided so the message brushes can be serialized/deserialized.
        /// </remarks>
        [JsonConverter(typeof(SolidColorBrushJsonConverter))]
        [ObservableProperty]
        private Brush _backgroundBrush = ColorPaletteCache.GetBrush("#0A86F1");

        /// <summary>
        /// Gets or sets the foreground color used for rendering.
        /// </summary>
        /// <remarks>
        /// System.Text.Json converter provided so the message brushes can be serialized/deserialized.
        /// </remarks>
        [JsonConverter(typeof(SolidColorBrushJsonConverter))]
        [ObservableProperty]
        private Brush _foregroundBrush = Brushes.White;
    }
}
