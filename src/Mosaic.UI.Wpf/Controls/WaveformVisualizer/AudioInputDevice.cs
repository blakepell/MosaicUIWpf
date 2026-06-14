/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.WaveformVisualizer
{
    /// <summary>
    /// Describes an audio input endpoint that can be selected by an <see cref="InputWaveformVisualizer"/>.
    /// </summary>
    public sealed class AudioInputDevice
    {
        internal AudioInputDevice(string? id, string name, bool isDefault)
        {
            Id = id;
            Name = name;
            IsDefault = isDefault;
        }

        /// <summary>
        /// Gets the Windows audio endpoint identifier.
        /// </summary>
        /// <value>
        /// The endpoint identifier, or <see langword="null" /> when the device represents the current default input.
        /// </value>
        public string? Id { get; }

        /// <summary>
        /// Gets the display name of the audio input endpoint.
        /// </summary>
        /// <value>
        /// The display name of the endpoint.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets a value that indicates whether this entry follows the current default audio input endpoint.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this entry follows the default input; otherwise, <see langword="false" />.
        /// </value>
        public bool IsDefault { get; }

        /// <inheritdoc/>
        public override string ToString() => Name;
    }
}
