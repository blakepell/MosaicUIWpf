/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Tests.ScriptImportedExtensions;

/// <summary>
/// Extension methods that are only in scope after a script runs <c>include Mosaic.UI.Wpf.Tests.ScriptImportedExtensions</c>.
/// </summary>
public static class ShoutExtensions
{
    public static string Shout(this string value, int count) => value.ToUpperInvariant() + new string('!', count);

    public static T Second<T>(this IEnumerable<T> items) => items.Skip(1).First();
}
