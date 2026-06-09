using System.Windows;

namespace AvalonDock.Themes.VisualStudio.Menu
{
    /// <summary>
    /// Resource key management class for menu-specific colors, styles and other elements
    /// that are typically changed between themes.
    /// </summary>
    public static class MenuKeys
    {
        /// <summary>
        /// Gets the resource key for the menu separator border brush.
        /// </summary>
        public static readonly ComponentResourceKey MenuSeparatorBorderBrushKey = new(typeof(ResourceKeys), "MenuSeparatorBorderBrushKey");

        /// <summary>
        /// Gets the resource key for the submenu item background.
        /// </summary>
        public static readonly ComponentResourceKey SubmenuItemBackgroundKey = new(typeof(ResourceKeys), "SubmenuItemBackgroundKey");

        /// <summary>
        /// Gets the resource key for the menu item highlighted background.
        /// </summary>
        public static readonly ComponentResourceKey MenuItemHighlightedBackgroundKey = new(typeof(ResourceKeys), "MenuItemHighlightedBackgroundKey");

        /// <summary>
        /// Gets the resource key for the submenu item background highlighted.
        /// </summary>
        public static readonly ComponentResourceKey SubmenuItemBackgroundHighlightedKey = new(typeof(ResourceKeys), "SubmenuItemBackgroundHighlightedKey");

        /// <summary>
        /// Gets the resource key for the check mark background brush.
        /// </summary>
        public static readonly ComponentResourceKey CheckMarkBackgroundBrushKey = new(typeof(ResourceKeys), "CheckMarkBackgroundBrushKey");

        /// <summary>
        /// Gets the resource key for the check mark border brush.
        /// </summary>
        public static readonly ComponentResourceKey CheckMarkBorderBrushKey = new(typeof(ResourceKeys), "CheckMarkBorderBrushKey");

        /// <summary>
        /// Gets the resource key for the check mark foreground brush.
        /// </summary>
        public static readonly ComponentResourceKey CheckMarkForegroundBrushKey = new(typeof(ResourceKeys), "CheckMarkForegroundBrushKey");

        /// <summary>
        /// Gets the resource key for the disabled sub menu item background brush.
        /// </summary>
        public static readonly ComponentResourceKey DisabledSubMenuItemBackgroundBrushKey = new(typeof(ResourceKeys), "DisabledSubMenuItemBackgroundBrushKey");

        /// <summary>
        /// Gets the resource key for the disabled sub menu item border brush.
        /// </summary>
        public static readonly ComponentResourceKey DisabledSubMenuItemBorderBrushKey = new(typeof(ResourceKeys), "DisabledSubMenuItemBorderBrushKey");

        /// <summary>
        /// Gets the resource key for the text brush.
        /// </summary>
        public static readonly ComponentResourceKey TextBrushKey = new(typeof(ResourceKeys), "TextBrushKey");

        /// <summary>
        /// Gets the resource key for the item background selected.
        /// </summary>
        public static readonly ComponentResourceKey ItemBackgroundSelectedKey = new(typeof(ResourceKeys), "ItemBackgroundSelectedKey");

        /// <summary>
        /// Gets the resource key for the item text disabled.
        /// </summary>
        public static readonly ComponentResourceKey ItemTextDisabledKey = new(typeof(ResourceKeys), "ItemTextDisabledKey");

        /// <summary>
        /// Gets the resource key for the item background hover.
        /// </summary>
        public static readonly ComponentResourceKey ItemBackgroundHoverKey = new(typeof(ResourceKeys), "ItemBackgroundHoverKey");

        /// <summary>
        /// Gets the resource key for the drop shadow effect.
        /// </summary>
        public static readonly ComponentResourceKey DropShadowEffectKey = new(typeof(ResourceKeys), "DropShadowEffectKey");
    }
}
