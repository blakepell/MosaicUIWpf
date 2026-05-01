/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A non-interactive category label displayed inside a <see cref="SideMenu"/>.
    /// Place one anywhere inside <see cref="SideMenu.MenuItems"/> to visually group the
    /// items that follow it.
    /// </summary>
    /// <remarks>
    /// <see cref="SideMenuHeader"/> extends <see cref="SideMenuItem"/> so it can live in the
    /// same <see cref="SideMenu.MenuItems"/> collection without any type changes.  It is
    /// rendered with a distinct template and is never selectable.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// <mosaic:SideMenu>
    ///     <mosaic:SideMenu.MenuItems>
    ///         <mosaic:SideMenuHeader Text="CONTROLS" />
    ///         <mosaic:SideMenuItem Text="Badge" ... />
    ///         <mosaic:SideMenuItem Text="Avatar" ... />
    ///     </mosaic:SideMenu.MenuItems>
    /// </mosaic:SideMenu>
    /// ]]>
    /// </code>
    /// </example>
    public class SideMenuHeader : SideMenuItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SideMenuHeader"/>.
        /// </summary>
        public SideMenuHeader()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SideMenuHeader"/> with the specified label text.
        /// </summary>
        public SideMenuHeader(string text)
        {
            Text = text;
        }
    }
}
