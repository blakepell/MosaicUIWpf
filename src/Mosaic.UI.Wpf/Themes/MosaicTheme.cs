namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// ComponentResourceKeys so XAML can strongly-reference resources:
    ///   {DynamicResource {x:Static themes:MosaicTheme.ControlBackgroundBrush}}
    /// </summary>
    public static class MosaicTheme
    {
        // General Accents
        public static ComponentResourceKey AccentBrush { get; } = new(typeof(MosaicTheme), "AccentBrush");
        public static ComponentResourceKey AccentColor { get; } = new(typeof(MosaicTheme), "AccentColor");

        public static ComponentResourceKey AccentBorderBrush { get; } = new(typeof(MosaicTheme), "AccentBorderBrush");
        public static ComponentResourceKey AccentBorderColor { get; } = new(typeof(MosaicTheme), "AccentBorderColor");

        public static ComponentResourceKey SelectionBackgroundBrush { get; } = new(typeof(MosaicTheme), "SelectionBackgroundBrush");
        public static ComponentResourceKey SelectionBackgroundColor { get; } = new(typeof(MosaicTheme), "SelectionBackgroundColor");

        public static ComponentResourceKey SelectionForegroundBrush { get; } = new(typeof(MosaicTheme), "SelectionForegroundBrush");
        public static ComponentResourceKey SelectionForegroundColor { get; } = new(typeof(MosaicTheme), "SelectionForegroundColor");

        public static ComponentResourceKey InactiveSelectionBackgroundBrush { get; } = new(typeof(MosaicTheme), "InactiveSelectionBackgroundBrush");
        public static ComponentResourceKey InactiveSelectionBackgroundColor { get; } = new(typeof(MosaicTheme), "InactiveSelectionBackgroundColor");

        public static ComponentResourceKey InactiveSelectionBorderBrush { get; } = new(typeof(MosaicTheme), "InactiveSelectionBorderBrush");
        public static ComponentResourceKey InactiveSelectionBorderColor { get; } = new(typeof(MosaicTheme), "InactiveSelectionBorderColor");

        public static ComponentResourceKey PlaceholderTextBrush { get; } = new(typeof(MosaicTheme), "PlaceholderTextBrush");
        public static ComponentResourceKey PlaceholderTextColor { get; } = new(typeof(MosaicTheme), "PlaceholderTextColor");

        // Status / Semantics (Brush)
        public static ComponentResourceKey InfoBrush { get; } = new(typeof(MosaicTheme), "InfoBrush");
        public static ComponentResourceKey InfoColor { get; } = new(typeof(MosaicTheme), "InfoColor");
        public static ComponentResourceKey SuccessBrush { get; } = new(typeof(MosaicTheme), "SuccessBrush");
        public static ComponentResourceKey SuccessColor { get; } = new(typeof(MosaicTheme), "SuccessColor");
        public static ComponentResourceKey WarningBrush { get; } = new(typeof(MosaicTheme), "WarningBrush");
        public static ComponentResourceKey WarningColor { get; } = new(typeof(MosaicTheme), "WarningColor");
        public static ComponentResourceKey ErrorBrush { get; } = new(typeof(MosaicTheme), "ErrorBrush");
        public static ComponentResourceKey ErrorColor { get; } = new(typeof(MosaicTheme), "ErrorColor");

        // Window
        public static ComponentResourceKey WindowBackgroundBrush { get; } = new(typeof(MosaicTheme), "WindowBackgroundBrush");
        public static ComponentResourceKey WindowBackgroundColor{ get; } = new(typeof(MosaicTheme), "WindowBackgroundColor");
        public static ComponentResourceKey WindowForegroundBrush { get; } = new(typeof(MosaicTheme), "WindowForegroundBrush");
        public static ComponentResourceKey WindowForegroundColor { get; } = new(typeof(MosaicTheme), "WindowForegroundColor");

        // Control
        public static ComponentResourceKey ControlBackgroundBrush { get; } = new(typeof(MosaicTheme), "ControlBackgroundBrush");
        public static ComponentResourceKey ControlBackgroundColor { get; } = new(typeof(MosaicTheme), "ControlBackgroundColor");

        public static ComponentResourceKey ControlBackgroundLightBrush { get; } = new(typeof(MosaicTheme), "ControlBackgroundLightBrush");
        public static ComponentResourceKey ControlBackgroundLightColor { get; } = new(typeof(MosaicTheme), "ControlBackgroundLightColor");

        public static ComponentResourceKey ControlForegroundBrush { get; } = new(typeof(MosaicTheme), "ControlForegroundBrush");
        public static ComponentResourceKey ControlForegroundColor { get; } = new(typeof(MosaicTheme), "ControlForegroundColor");

        public static ComponentResourceKey ControlBorderBrush { get; } = new(typeof(MosaicTheme), "ControlBorderBrush");
        public static ComponentResourceKey ControlBorderColor { get; } = new(typeof(MosaicTheme), "ControlBorderColor");
        
        public static ComponentResourceKey ControlSelectedBorderBrush { get; } = new(typeof(MosaicTheme), "ControlSelectedBorderBrush");
        public static ComponentResourceKey ControlSelectedBorderColor { get; } = new(typeof(MosaicTheme), "ControlSelectedBorderColor");

        public static ComponentResourceKey ControlHoverBackgroundBrush { get; } = new(typeof(MosaicTheme), "ControlHoverBackgroundBrush");
        public static ComponentResourceKey ControlHoverBackgroundColor { get; } = new(typeof(MosaicTheme), "ControlHoverBackgroundColor");

        public static ComponentResourceKey ControlHoverBorderBrush { get; } = new(typeof(MosaicTheme), "ControlHoverBorderBrush");
        public static ComponentResourceKey ControlHoverBorderColor { get; } = new(typeof(MosaicTheme), "ControlHoverBorderColor");

        public static ComponentResourceKey ControlTextBackgroundBrush { get; } = new(typeof(MosaicTheme), "ControlTextBackgroundBrush");
        public static ComponentResourceKey ControlTextBackgroundColor { get; } = new(typeof(MosaicTheme), "ControlTextBackgroundColor");

        public static ComponentResourceKey ControlTextForegroundBrush { get; } = new(typeof(MosaicTheme), "ControlTextForegroundBrush");
        public static ComponentResourceKey ControlTextForegroundColor { get; } = new(typeof(MosaicTheme), "ControlTextForegroundColor");

        public static ComponentResourceKey ControlTextSecondaryForegroundBrush { get; } = new(typeof(MosaicTheme), "ControlTextSecondaryForegroundBrush");
        public static ComponentResourceKey ControlTextSecondaryForegroundColor { get; } = new(typeof(MosaicTheme), "ControlTextSecondaryForegroundColor");

        public static ComponentResourceKey ControlPressedBackgroundBrush { get; } = new(typeof(MosaicTheme), "ControlPressedBackgroundBrush");
        public static ComponentResourceKey ControlPressedBackgroundColor { get; } = new(typeof(MosaicTheme), "ControlPressedBackgroundColor");

        public static ComponentResourceKey ControlPressedBorderBrush { get; } = new(typeof(MosaicTheme), "ControlPressedBorderBrush");
        public static ComponentResourceKey ControlPressedBorderColor { get; } = new(typeof(MosaicTheme), "ControlPressedBorderColor");

        public static ComponentResourceKey ControlDisabledBackgroundBrush { get; } = new(typeof(MosaicTheme), "ControlDisabledBackgroundBrush");
        public static ComponentResourceKey ControlDisabledBackgroundColor { get; } = new(typeof(MosaicTheme), "ControlDisabledBackgroundColor");

        public static ComponentResourceKey ControlDisabledForegroundBrush { get; } = new(typeof(MosaicTheme), "ControlDisabledForegroundBrush");
        public static ComponentResourceKey ControlDisabledForegroundColor { get; } = new(typeof(MosaicTheme), "ControlDisabledForegroundColor");

        public static ComponentResourceKey ControlDisabledBorderBrush { get; } = new(typeof(MosaicTheme), "ControlDisabledBorderBrush");
        public static ComponentResourceKey ControlDisabledBorderColor { get; } = new(typeof(MosaicTheme), "ControlDisabledBorderColor");

        public static ComponentResourceKey ControlSeparatorBrush { get; } = new(typeof(MosaicTheme), "ControlSeparatorBrush");

        public static ComponentResourceKey ControlSeparatorColor { get; } = new(typeof(MosaicTheme), "ControlSeparatorColor");

        public static ComponentResourceKey HyperLinkBrush { get; } = new(typeof(MosaicTheme), "HyperLinkBrush");
        public static ComponentResourceKey HyperLinkColor { get; } = new(typeof(MosaicTheme), "HyperLinkColor");
        public static ComponentResourceKey HyperLinkHoverBrush { get; } = new(typeof(MosaicTheme), "HyperLinkHoverBrush");
        public static ComponentResourceKey HyperLinkHoverColor { get; } = new(typeof(MosaicTheme), "HyperLinkHoverColor");
        public static ComponentResourceKey HyperLinkVisitedBrush { get; } = new(typeof(MosaicTheme), "HyperLinkVisitedBrush");
        public static ComponentResourceKey HyperLinkVisitedColor { get; } = new(typeof(MosaicTheme), "HyperLinkVisitedColor");

        public static ComponentResourceKey TabItemSelectedBackgroundBrush { get; } = new(typeof(MosaicTheme), "TabItemSelectedBackgroundBrush");
        public static ComponentResourceKey TabItemSelectedBackgroundColor { get; } = new(typeof(MosaicTheme), "TabItemSelectedBackgroundColor");

        public static ComponentResourceKey TabItemBackgroundBrush { get; } = new(typeof(MosaicTheme), "TabItemBackgroundBrush");
        public static ComponentResourceKey TabItemBackgroundColor { get; } = new(typeof(MosaicTheme), "TabItemBackgroundColor");
        
        public static ComponentResourceKey TabItemBorderBrush { get; } = new(typeof(MosaicTheme), "TabItemBorderBrush");
        public static ComponentResourceKey TabItemBorderColor { get; } = new(typeof(MosaicTheme), "TabItemBorderColor");
        
        public static ComponentResourceKey TabItemForegroundBrush { get; } = new(typeof(MosaicTheme), "TabItemForegroundBrush");
        public static ComponentResourceKey TabItemForegroundColor { get; } = new(typeof(MosaicTheme), "TabItemForegroundColor");

        public static ComponentResourceKey SidebarSelectedBackgroundColor { get; } = new(typeof(MosaicTheme), "SidebarSelectedBackgroundColor");
        public static ComponentResourceKey SidebarSelectedBackgroundBrush { get; } = new(typeof(MosaicTheme), "SidebarSelectedBackgroundBrush");
    }
}
