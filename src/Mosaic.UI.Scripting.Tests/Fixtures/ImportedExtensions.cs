/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Scripting.Test.ImportedExtensions;

/// <summary>
/// Extension methods that are only callable after <c>include Mosaic.UI.Wpf.Scripting.Test.ImportedExtensions</c>.
/// </summary>
public static class ShoutExtensions
{
    public static string Shout(this string value) => value.ToUpperInvariant() + "!";
}
