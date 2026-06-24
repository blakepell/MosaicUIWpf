using System.Windows;

namespace Mosaic.UI.Wpf.AvalonDock.Themes.VisualStudio
{
    /// <summary>
    /// Resource key management class to keep track of all resources
    /// that can be re-styled in applications that make use of the implemented controls.
    /// </summary>
    public static class ResourceKeys
    {
        /// <summary>
        /// Accent Color Key - This Color key is used to accent elements in the UI
        /// (e.g.: Color of Activated Normal Window Frame, ResizeGrip, Focus or MouseOver input elements)
        /// </summary>
        public static readonly ComponentResourceKey ControlAccentColorKey = new(typeof(ResourceKeys), "ControlAccentColorKey");

        /// <summary>
        /// Accent Brush Key - This Brush key is used to accent elements in the UI
        /// (e.g.: Color of Activated Normal Window Frame, ResizeGrip, Focus or MouseOver input elements)
        /// </summary>
        public static readonly ComponentResourceKey ControlAccentBrushKey = new(typeof(ResourceKeys), "ControlAccentBrushKey");

        // General

        /// <summary>
        /// Gets the resource key for the background.
        /// </summary>
        public static readonly ComponentResourceKey Background = new(typeof(ResourceKeys), "Background");

        /// <summary>
        /// Gets the resource key for the panel border brush.
        /// </summary>
        public static readonly ComponentResourceKey PanelBorderBrush = new(typeof(ResourceKeys), "PanelBorderBrush");

        /// <summary>
        /// Gets the resource key for the tab background.
        /// </summary>
        public static readonly ComponentResourceKey TabBackground = new(typeof(ResourceKeys), "TabBackground");

        // Auto Hide : Tab

        /// <summary>
        /// Gets the resource key for the auto hide tab default background.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabDefaultBackground = new(typeof(ResourceKeys), "AutoHideTabDefaultBackground");

        /// <summary>
        /// Gets the resource key for the auto hide tab default border.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabDefaultBorder = new(typeof(ResourceKeys), "AutoHideTabDefaultBorder");

        /// <summary>
        /// Gets the resource key for the auto hide tab default text.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabDefaultText = new(typeof(ResourceKeys), "AutoHideTabDefaultText");

        /// <summary>
        /// Gets the resource key for the auto hide tab hovered background.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabHoveredBackground = new(typeof(ResourceKeys), "AutoHideTabHoveredBackground");

        /// <summary>
        /// Gets the resource key for the auto hide tab hovered border.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabHoveredBorder = new(typeof(ResourceKeys), "AutoHideTabHoveredBorder");

        /// <summary>
        /// Gets the resource key for the auto hide tab hovered text.
        /// </summary>
        public static readonly ComponentResourceKey AutoHideTabHoveredText = new(typeof(ResourceKeys), "AutoHideTabHoveredText");

        // Document Well : Overflow Button

        /// <summary>
        /// Gets the resource key for the document well overflow button default glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonDefaultGlyph = new(typeof(ResourceKeys), "DocumentWellOverflowButtonDefaultGlyph");

        /// <summary>
        /// Gets the resource key for the document well overflow button hovered background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonHoveredBackground = new(typeof(ResourceKeys), "DocumentWellOverflowButtonHoveredBackground");

        /// <summary>
        /// Gets the resource key for the document well overflow button hovered border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonHoveredBorder = new(typeof(ResourceKeys), "DocumentWellOverflowButtonHoveredBorder");

        /// <summary>
        /// Gets the resource key for the document well overflow button hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonHoveredGlyph = new(typeof(ResourceKeys), "DocumentWellOverflowButtonHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the document well overflow button pressed background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonPressedBackground = new(typeof(ResourceKeys), "DocumentWellOverflowButtonPressedBackground");

        /// <summary>
        /// Gets the resource key for the document well overflow button pressed border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonPressedBorder = new(typeof(ResourceKeys), "DocumentWellOverflowButtonPressedBorder");

        /// <summary>
        /// Gets the resource key for the document well overflow button pressed glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellOverflowButtonPressedGlyph = new(typeof(ResourceKeys), "DocumentWellOverflowButtonPressedGlyph");

        // Document Well : Tab

        /// <summary>
        /// Gets the resource key for the document well tab selected active background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabSelectedActiveBackground = new(typeof(ResourceKeys), "DocumentWellTabSelectedActiveBackground");

        /// <summary>
        /// Gets the resource key for the document well tab selected active text.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabSelectedActiveText = new(typeof(ResourceKeys), "DocumentWellTabSelectedActiveText");

        /// <summary>
        /// Gets the resource key for the document well tab selected inactive background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabSelectedInactiveBackground = new(typeof(ResourceKeys), "DocumentWellTabSelectedInactiveBackground");

        /// <summary>
        /// Gets the resource key for the document well tab selected inactive text.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabSelectedInactiveText = new(typeof(ResourceKeys), "DocumentWellTabSelectedInactiveText");

        /// <summary>
        /// Gets the resource key for the document well tab unselected background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabUnselectedBackground = new(typeof(ResourceKeys), "DocumentWellTabUnselectedBackground");

        /// <summary>
        /// Gets the resource key for the document well tab unselected text.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabUnselectedText = new(typeof(ResourceKeys), "DocumentWellTabUnselectedText");

        /// <summary>
        /// Gets the resource key for the document well tab unselected hovered background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabUnselectedHoveredBackground = new(typeof(ResourceKeys), "DocumentWellTabUnselectedHoveredBackground");

        /// <summary>
        /// Gets the resource key for the document well tab unselected hovered text.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabUnselectedHoveredText = new(typeof(ResourceKeys), "DocumentWellTabUnselectedHoveredText");

        // Document Well : Tab : Button

        /// <summary>
        /// Gets the resource key for the document well tab button selected active glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActiveGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActiveGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active hovered background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActiveHoveredBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActiveHoveredBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active hovered border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActiveHoveredBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActiveHoveredBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActiveHoveredGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActiveHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active pressed background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActivePressedBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActivePressedBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active pressed border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActivePressedBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActivePressedBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button selected active pressed glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedActivePressedGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedActivePressedGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactiveGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactiveGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive hovered background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactiveHoveredBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactiveHoveredBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive hovered border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactiveHoveredBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactiveHoveredBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactiveHoveredGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactiveHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive pressed background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactivePressedBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactivePressedBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive pressed border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactivePressedBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactivePressedBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button selected inactive pressed glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonSelectedInactivePressedGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonSelectedInactivePressedGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button hovered background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonHoveredBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonHoveredBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button hovered border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonHoveredBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonHoveredBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonHoveredGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button pressed background.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonPressedBackground = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonPressedBackground");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button pressed border.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonPressedBorder = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonPressedBorder");

        /// <summary>
        /// Gets the resource key for the document well tab button unselected tab hovered button pressed glyph.
        /// </summary>
        public static readonly ComponentResourceKey DocumentWellTabButtonUnselectedTabHoveredButtonPressedGlyph = new(typeof(ResourceKeys), "DocumentWellTabButtonUnselectedTabHoveredButtonPressedGlyph");

        // Tool Window : Caption

        /// <summary>
        /// Gets the resource key for the tool window caption active background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionActiveBackground = new(typeof(ResourceKeys), "ToolWindowCaptionActiveBackground");

        /// <summary>
        /// Gets the resource key for the tool window caption active grip.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionActiveGrip = new(typeof(ResourceKeys), "ToolWindowCaptionActiveGrip");

        /// <summary>
        /// Gets the resource key for the tool window caption active text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionActiveText = new(typeof(ResourceKeys), "ToolWindowCaptionActiveText");

        /// <summary>
        /// Gets the resource key for the tool window caption inactive background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionInactiveBackground = new(typeof(ResourceKeys), "ToolWindowCaptionInactiveBackground");

        /// <summary>
        /// Gets the resource key for the tool window caption inactive grip.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionInactiveGrip = new(typeof(ResourceKeys), "ToolWindowCaptionInactiveGrip");

        /// <summary>
        /// Gets the resource key for the tool window caption inactive text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionInactiveText = new(typeof(ResourceKeys), "ToolWindowCaptionInactiveText");

        // Tool Window : Caption : Button

        /// <summary>
        /// Gets the resource key for the tool window caption button active glyph.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActiveGlyph = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActiveGlyph");

        /// <summary>
        /// Gets the resource key for the tool window caption button active hovered background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActiveHoveredBackground = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActiveHoveredBackground");

        /// <summary>
        /// Gets the resource key for the tool window caption button active hovered border.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActiveHoveredBorder = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActiveHoveredBorder");

        /// <summary>
        /// Gets the resource key for the tool window caption button active hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActiveHoveredGlyph = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActiveHoveredGlyph");

        /// <summary>
        /// Gets the resource key for the tool window caption button active pressed background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActivePressedBackground = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActivePressedBackground");

        /// <summary>
        /// Gets the resource key for the tool window caption button active pressed border.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActivePressedBorder = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActivePressedBorder");

        /// <summary>
        /// Gets the resource key for the tool window caption button active pressed glyph.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonActivePressedGlyph = new(typeof(ResourceKeys), "ToolWindowCaptionButtonActivePressedGlyph");

        /// <summary>
        /// Gets the resource key for the tool window caption button inactive glyph.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonInactiveGlyph = new(typeof(ResourceKeys), "ToolWindowCaptionButtonInactiveGlyph");

        /// <summary>
        /// Gets the resource key for the tool window caption button inactive hovered background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonInactiveHoveredBackground = new(typeof(ResourceKeys), "ToolWindowCaptionButtonInactiveHoveredBackground");

        /// <summary>
        /// Gets the resource key for the tool window caption button inactive hovered border.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonInactiveHoveredBorder = new(typeof(ResourceKeys), "ToolWindowCaptionButtonInactiveHoveredBorder");

        /// <summary>
        /// Gets the resource key for the tool window caption button inactive hovered glyph.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowCaptionButtonInactiveHoveredGlyph = new(typeof(ResourceKeys), "ToolWindowCaptionButtonInactiveHoveredGlyph");

        // Tool Window : Tab

        /// <summary>
        /// Gets the resource key for the tool window tab selected active background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabSelectedActiveBackground = new(typeof(ResourceKeys), "ToolWindowTabSelectedActiveBackground");

        /// <summary>
        /// Gets the resource key for the tool window tab selected active text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabSelectedActiveText = new(typeof(ResourceKeys), "ToolWindowTabSelectedActiveText");

        /// <summary>
        /// Gets the resource key for the tool window tab selected inactive background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabSelectedInactiveBackground = new(typeof(ResourceKeys), "ToolWindowTabSelectedInactiveBackground");

        /// <summary>
        /// Gets the resource key for the tool window tab selected inactive text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabSelectedInactiveText = new(typeof(ResourceKeys), "ToolWindowTabSelectedInactiveText");

        /// <summary>
        /// Gets the resource key for the tool window tab unselected background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabUnselectedBackground = new(typeof(ResourceKeys), "ToolWindowTabUnselectedBackground");

        /// <summary>
        /// Gets the resource key for the tool window tab unselected text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabUnselectedText = new(typeof(ResourceKeys), "ToolWindowTabUnselectedText");

        /// <summary>
        /// Gets the resource key for the tool window tab unselected hovered background.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabUnselectedHoveredBackground = new(typeof(ResourceKeys), "ToolWindowTabUnselectedHoveredBackground");

        /// <summary>
        /// Gets the resource key for the tool window tab unselected hovered text.
        /// </summary>
        public static readonly ComponentResourceKey ToolWindowTabUnselectedHoveredText = new(typeof(ResourceKeys), "ToolWindowTabUnselectedHoveredText");

        // Floating Document Window

        /// <summary>
        /// Gets the resource key for the floating document window background.
        /// </summary>
        public static readonly ComponentResourceKey FloatingDocumentWindowBackground = new(typeof(ResourceKeys), "FloatingDocumentWindowBackground");

        /// <summary>
        /// Gets the resource key for the floating document window border.
        /// </summary>
        public static readonly ComponentResourceKey FloatingDocumentWindowBorder = new(typeof(ResourceKeys), "FloatingDocumentWindowBorder");

        // Floating Tool Window

        /// <summary>
        /// Gets the resource key for the floating tool window background.
        /// </summary>
        public static readonly ComponentResourceKey FloatingToolWindowBackground = new(typeof(ResourceKeys), "FloatingToolWindowBackground");

        /// <summary>
        /// Gets the resource key for the floating tool window border.
        /// </summary>
        public static readonly ComponentResourceKey FloatingToolWindowBorder = new(typeof(ResourceKeys), "FloatingToolWindowBorder");

        // Navigator Window

        /// <summary>
        /// Gets the resource key for the navigator window background.
        /// </summary>
        public static readonly ComponentResourceKey NavigatorWindowBackground = new(typeof(ResourceKeys), "NavigatorWindowBackground");

        /// <summary>
        /// Gets the resource key for the navigator window foreground.
        /// </summary>
        public static readonly ComponentResourceKey NavigatorWindowForeground = new(typeof(ResourceKeys), "NavigatorWindowForeground");

        /// <summary>
        /// Gets the resource key for the navigator window selected background.
        /// </summary>
        public static readonly ComponentResourceKey NavigatorWindowSelectedBackground = new(typeof(ResourceKeys), "NavigatorWindowSelectedBackground");

        /// <summary>
        /// Gets the resource key for the navigator window selected text.
        /// </summary>
        public static readonly ComponentResourceKey NavigatorWindowSelectedText = new(typeof(ResourceKeys), "NavigatorWindowSelectedText");

        // Docking Buttons

        /// <summary>
        /// Gets the resource key for the docking button width.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonWidthKey = new(typeof(ResourceKeys), "DockingButtonWidthKey");

        /// <summary>
        /// Gets the resource key for the docking button height.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonHeightKey = new(typeof(ResourceKeys), "DockingButtonHeightKey");

        /// <summary>
        /// Gets the resource key for the docking button foreground brush.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonForegroundBrushKey = new(typeof(ResourceKeys), "DockingButtonForegroundBrushKey");

        /// <summary>
        /// Gets the resource key for the docking button foreground arrow brush.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonForegroundArrowBrushKey = new(typeof(ResourceKeys), "DockingButtonForegroundArrowBrushKey");

        /// <summary>
        /// Gets the resource key for the docking button star border brush.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonStarBorderBrushKey = new(typeof(ResourceKeys), "DockingButtonStarBorderBrushKey");

        /// <summary>
        /// Gets the resource key for the docking button star background brush.
        /// </summary>
        public static readonly ComponentResourceKey DockingButtonStarBackgroundBrushKey = new(typeof(ResourceKeys), "DockingButtonStarBackgroundBrushKey");

        // Preview Box

        /// <summary>
        /// Gets the resource key for the preview box border brush.
        /// </summary>
        public static readonly ComponentResourceKey PreviewBoxBorderBrushKey = new(typeof(ResourceKeys), "PreviewBoxBorderBrushKey");

        /// <summary>
        /// Gets the resource key for the preview box background brush.
        /// </summary>
        public static readonly ComponentResourceKey PreviewBoxBackgroundBrushKey = new(typeof(ResourceKeys), "PreviewBoxBackgroundBrushKey");
    }
}
