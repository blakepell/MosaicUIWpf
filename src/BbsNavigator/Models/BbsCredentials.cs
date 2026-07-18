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
    internal sealed record BbsCredentials(string UserName, string Password);
}
