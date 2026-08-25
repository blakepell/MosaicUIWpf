/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Models
{
    /// <summary>
    /// Stores the decrypted username and password for a BBS profile while they are in use.
    /// </summary>
    /// <param name="UserName">The login username.</param>
    /// <param name="Password">The login password.</param>
    public sealed record BbsCredentials(string UserName, string Password)
    {
        /// <summary>
        /// Gets the private key file used to authenticate an SSH session.
        /// </summary>
        /// <value>
        /// The full path of an OpenSSH or PEM private key, or <see langword="null"/> when the
        /// session authenticates with <see cref="Password"/> alone.
        /// </value>
        public string? KeyFile { get; init; }

        /// <summary>
        /// Gets the passphrase that decrypts <see cref="KeyFile"/>.
        /// </summary>
        /// <value>
        /// The private key passphrase, or <see langword="null"/> when the key is not encrypted.
        /// </value>
        public string? KeyPassphrase { get; init; }
    }
}
