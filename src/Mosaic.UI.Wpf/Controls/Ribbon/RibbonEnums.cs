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
    /// Specifies how a <see cref="Ribbon"/> arranges the commands of the selected tab.
    /// </summary>
    public enum RibbonLayoutMode
    {
        /// <summary>Groups contain up to three rows of commands with large buttons and group labels.</summary>
        Classic,

        /// <summary>A single compact row of commands with fewer commands visible inline.</summary>
        Simplified
    }

    /// <summary>
    /// Specifies the spacing density of Ribbon commands. Density is independent of <see cref="RibbonLayoutMode"/>.
    /// </summary>
    public enum UserInterfaceDensity
    {
        /// <summary>Tighter rows suited to mouse-heavy use.</summary>
        Compact,

        /// <summary>The default spacing.</summary>
        Standard,

        /// <summary>Roomier rows suited to touch interaction.</summary>
        Spacious
    }

    /// <summary>
    /// The presentation size of a Ribbon group or command.
    /// </summary>
    /// <remarks>Values are ordered from most to least expanded; the layout engine relies on that ordering.</remarks>
    public enum VariantSize
    {
        /// <summary>Large icon with the label underneath, occupying the full group height.</summary>
        Large = 0,

        /// <summary>Small icon with the label beside it, occupying one row.</summary>
        Medium = 1,

        /// <summary>Small icon only, occupying one row; the label is exposed through the screen tip and automation name.</summary>
        Small = 2,

        /// <summary>A group is reduced to a single button that opens its content in a popup.</summary>
        Collapsed = 3
    }

    /// <summary>
    /// Specifies whether a command may leave the inline Simplified row for an overflow menu.
    /// </summary>
    public enum ItemCollapseBehavior
    {
        /// <summary>The command overflows only when space requires it.</summary>
        Default,

        /// <summary>The command always stays inline.</summary>
        Never,

        /// <summary>The command always lives in overflow while in Simplified mode.</summary>
        Always
    }

    /// <summary>
    /// Constrains the variants the layout engine may assign to a command.
    /// </summary>
    public enum ItemVariantBehavior
    {
        /// <summary>Any variant supported by the command may be used.</summary>
        All,

        /// <summary>The command is never reduced to <see cref="VariantSize.Small"/>.</summary>
        NeverSmall,

        /// <summary>The command skips <see cref="VariantSize.Medium"/>, going straight from large to small.</summary>
        NeverMedium,

        /// <summary>The command is never shown at <see cref="VariantSize.Large"/>.</summary>
        NeverLarge,

        /// <summary>The command is always shown at <see cref="VariantSize.Large"/> where supported.</summary>
        AlwaysLarge,

        /// <summary>The command is always shown at <see cref="VariantSize.Medium"/>.</summary>
        AlwaysMedium,

        /// <summary>The command is always shown at <see cref="VariantSize.Small"/>.</summary>
        AlwaysSmall
    }

    /// <summary>
    /// Specifies where a group's commands go when they overflow in Simplified mode.
    /// </summary>
    public enum RibbonOverflowTarget
    {
        /// <summary>Overflowed commands are listed in the tab-level overflow menu at the end of the row.</summary>
        Tab,

        /// <summary>Overflowed commands are listed in a drop-down at the end of their own group.</summary>
        Group
    }

    /// <summary>
    /// The location of the Quick Access Toolbar relative to the Ribbon.
    /// </summary>
    public enum QuickAccessToolbarLocation
    {
        /// <summary>Above the tab strip.</summary>
        Above,

        /// <summary>Below the command card.</summary>
        Below
    }

    /// <summary>
    /// Specifies whether the Quick Access Toolbar is shown and whether the user may change that.
    /// </summary>
    public enum QuickAccessToolbarMode
    {
        /// <summary>The toolbar is not available and the user cannot reveal it.</summary>
        None,

        /// <summary>The toolbar is hidden but the user may reveal it.</summary>
        Hidden,

        /// <summary>The toolbar is visible and the user may hide it.</summary>
        Visible
    }

    /// <summary>
    /// The animation the selected-tab indicator (the accent underline) plays when the selected tab changes.
    /// </summary>
    public enum RibbonTabIndicatorAnimation
    {
        /// <summary>The indicator moves to the new tab without animating.</summary>
        None,

        /// <summary>
        /// The indicator travels from the previous tab: its leading edge stretches toward the new tab, then the
        /// trailing edge catches up and snaps it to the new tab's width.
        /// </summary>
        Slide,

        /// <summary>
        /// The indicator appears on the new tab at once, expands outward in both directions, then snaps back to the
        /// tab's width. This is the default.
        /// </summary>
        Expand
    }

    /// <summary>
    /// Identifies the surface a generated command presentation is created for.
    /// </summary>
    public enum RibbonPresentationKind
    {
        /// <summary>A row in a Simplified overflow menu.</summary>
        Overflow,

        /// <summary>A compact button in the Quick Access Toolbar.</summary>
        QuickAccessToolbar
    }

    /// <summary>
    /// Selects which preferences <see cref="RibbonSerializer"/> saves or restores.
    /// </summary>
    [Flags]
    public enum RibbonSerializerOptions
    {
        /// <summary>No preferences.</summary>
        None = 0,

        /// <summary><see cref="Ribbon.IsMinimized"/>.</summary>
        MinimizedState = 1,

        /// <summary><see cref="Ribbon.LayoutMode"/>.</summary>
        LayoutMode = 2,

        /// <summary><see cref="Ribbon.Density"/>.</summary>
        UserInterfaceDensity = 4,

        /// <summary>The ordered Quick Access Toolbar command keys.</summary>
        QuickAccessToolbarItems = 8,

        /// <summary><see cref="Ribbon.QuickAccessToolbarLocation"/>.</summary>
        QuickAccessToolbarLocation = 16,

        /// <summary><see cref="Ribbon.QuickAccessToolbarMode"/>.</summary>
        QuickAccessToolbarMode = 32,

        /// <summary><see cref="Ribbon.QuickAccessToolbarShowLabels"/>.</summary>
        QuickAccessToolbarAllowLabels = 64,

        /// <summary>Every Quick Access Toolbar preference.</summary>
        QuickAccessToolbar = QuickAccessToolbarItems | QuickAccessToolbarLocation | QuickAccessToolbarMode | QuickAccessToolbarAllowLabels,

        /// <summary>
        /// The selected tab, identified by <see cref="RibbonTabItem.Key"/> (or its header text). A tab that no longer
        /// exists, or is hidden or disabled when restored, is skipped.
        /// </summary>
        SelectedTab = 128,

        /// <summary>Every supported preference.</summary>
        All = MinimizedState | LayoutMode | UserInterfaceDensity | QuickAccessToolbar | SelectedTab
    }
}
