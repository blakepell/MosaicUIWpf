/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace MosaicWpfDemo.Common
{
    /// <summary>
    /// A <see cref="Freezable"/> that relays a <see cref="DataContext"/> across a binding gap. A
    /// <see cref="ContextMenu"/> (or other pop-up) lives in its own name-scope and is not part of the
    /// visual tree, so it cannot reach an ancestor's <c>DataContext</c> with a normal
    /// <c>RelativeSource</c> binding. Placing a <see cref="BindingProxy"/> in an element's resources lets
    /// the proxy inherit that element's <c>DataContext</c> (Freezables carry the inheritance context), so a
    /// menu item can bind through it: <c>{Binding Data.SomeCommand, Source={StaticResource Proxy}}</c>.
    /// </summary>
    public sealed class BindingProxy : Freezable
    {
        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the value relayed by the proxy — typically bound to the host element's
        /// <c>DataContext</c> and read back as <c>Data.&lt;member&gt;</c>.
        /// </summary>
        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }
}
