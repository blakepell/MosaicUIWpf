/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Describes a video capture device that can be displayed by a <see cref="Webcam"/> control.
    /// </summary>
    /// <remarks>
    /// Instances are immutable.  Two instances are equal when they refer to the same
    /// <see cref="SymbolicLink"/>, so a device returned by one call to
    /// <see cref="WebcamDeviceManager.GetDevices"/> matches the same device returned by a later call.
    /// The symbolic link is stable for a given device and port, which makes it suitable for persisting
    /// the user's camera choice in settings.
    /// </remarks>
    public sealed class WebcamDevice : IEquatable<WebcamDevice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamDevice"/> class.
        /// </summary>
        /// <param name="name">The friendly name of the device.</param>
        /// <param name="symbolicLink">The Media Foundation symbolic link that identifies the device.</param>
        /// <exception cref="ArgumentException"><paramref name="symbolicLink"/> is null, empty or white space.</exception>
        public WebcamDevice(string name, string symbolicLink)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(symbolicLink);

            Name = string.IsNullOrWhiteSpace(name) ? symbolicLink : name;
            SymbolicLink = symbolicLink;
        }

        /// <summary>
        /// Gets the friendly display name of the device, for example "Integrated Camera".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the symbolic link that uniquely identifies the device on this system.
        /// </summary>
        public string SymbolicLink { get; }

        /// <inheritdoc />
        public bool Equals(WebcamDevice? other)
        {
            return other is not null && string.Equals(SymbolicLink, other.SymbolicLink, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as WebcamDevice);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(SymbolicLink);

        /// <summary>
        /// Returns the friendly name of the device.
        /// </summary>
        public override string ToString() => Name;
    }
}
