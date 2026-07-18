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
    /// A <see cref="DisplayNameAttribute"/> that can be applied to enum fields.
    /// </summary>
    /// <remarks>
    /// The stock <see cref="DisplayNameAttribute"/> declares an <see cref="AttributeUsageAttribute"/>
    /// that excludes fields, so C# does not allow it directly on enum members.  This subclass
    /// re-opens the attribute for fields while remaining a <see cref="DisplayNameAttribute"/>, so
    /// <see cref="EnumComboBox"/> (and any other <see cref="DisplayNameAttribute"/>-aware code)
    /// resolves it through the normal base-type lookup.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumDisplayNameAttribute : DisplayNameAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumDisplayNameAttribute"/>.
        /// </summary>
        /// <param name="displayName">The friendly display name for the enum member.</param>
        public EnumDisplayNameAttribute(string displayName) : base(displayName)
        {
        }
    }
}
