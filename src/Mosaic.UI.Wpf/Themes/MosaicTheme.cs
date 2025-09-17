using System.Windows;

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

        public static ComponentResourceKey SidebarSelectedAccentColor { get; } = new(typeof(MosaicTheme), "SidebarSelectedAccentColor");
        public static ComponentResourceKey SidebarSelectedAccentBrush { get; } = new(typeof(MosaicTheme), "SidebarSelectedAccentBrush");

        public static ComponentResourceKey SidebarForegroundColor { get; } = new(typeof(MosaicTheme), "SidebarForegroundColor");
        public static ComponentResourceKey SidebarForegroundBrush { get; } = new(typeof(MosaicTheme), "SidebarForegroundBrush");

        /*
         *  General color palette:
         */
        public static ComponentResourceKey Blue50 { get; } = new(typeof(MosaicTheme), "Blue50");
        public static ComponentResourceKey Blue100 { get; } = new(typeof(MosaicTheme), "Blue100");
        public static ComponentResourceKey Blue200 { get; } = new(typeof(MosaicTheme), "Blue200");
        public static ComponentResourceKey Blue300 { get; } = new(typeof(MosaicTheme), "Blue300");
        public static ComponentResourceKey Blue400 { get; } = new(typeof(MosaicTheme), "Blue400");
        public static ComponentResourceKey Blue500 { get; } = new(typeof(MosaicTheme), "Blue500");
        public static ComponentResourceKey Blue600 { get; } = new(typeof(MosaicTheme), "Blue600");
        public static ComponentResourceKey Blue700 { get; } = new(typeof(MosaicTheme), "Blue700");
        public static ComponentResourceKey Blue800 { get; } = new(typeof(MosaicTheme), "Blue800");
        public static ComponentResourceKey Blue900 { get; } = new(typeof(MosaicTheme), "Blue900");

        public static ComponentResourceKey Indigo50 { get; } = new(typeof(MosaicTheme), "Indigo50");
        public static ComponentResourceKey Indigo100 { get; } = new(typeof(MosaicTheme), "Indigo100");
        public static ComponentResourceKey Indigo200 { get; } = new(typeof(MosaicTheme), "Indigo200");
        public static ComponentResourceKey Indigo300 { get; } = new(typeof(MosaicTheme), "Indigo300");
        public static ComponentResourceKey Indigo400 { get; } = new(typeof(MosaicTheme), "Indigo400");
        public static ComponentResourceKey Indigo500 { get; } = new(typeof(MosaicTheme), "Indigo500");
        public static ComponentResourceKey Indigo600 { get; } = new(typeof(MosaicTheme), "Indigo600");
        public static ComponentResourceKey Indigo700 { get; } = new(typeof(MosaicTheme), "Indigo700");
        public static ComponentResourceKey Indigo800 { get; } = new(typeof(MosaicTheme), "Indigo800");
        public static ComponentResourceKey Indigo900 { get; } = new(typeof(MosaicTheme), "Indigo900");

        public static ComponentResourceKey Purple50 { get; } = new(typeof(MosaicTheme), "Purple50");
        public static ComponentResourceKey Purple100 { get; } = new(typeof(MosaicTheme), "Purple100");
        public static ComponentResourceKey Purple200 { get; } = new(typeof(MosaicTheme), "Purple200");
        public static ComponentResourceKey Purple300 { get; } = new(typeof(MosaicTheme), "Purple300");
        public static ComponentResourceKey Purple400 { get; } = new(typeof(MosaicTheme), "Purple400");
        public static ComponentResourceKey Purple500 { get; } = new(typeof(MosaicTheme), "Purple500");
        public static ComponentResourceKey Purple600 { get; } = new(typeof(MosaicTheme), "Purple600");
        public static ComponentResourceKey Purple700 { get; } = new(typeof(MosaicTheme), "Purple700");
        public static ComponentResourceKey Purple800 { get; } = new(typeof(MosaicTheme), "Purple800");
        public static ComponentResourceKey Purple900 { get; } = new(typeof(MosaicTheme), "Purple900");

        public static ComponentResourceKey Pink50 { get; } = new(typeof(MosaicTheme), "Pink50");
        public static ComponentResourceKey Pink100 { get; } = new(typeof(MosaicTheme), "Pink100");
        public static ComponentResourceKey Pink200 { get; } = new(typeof(MosaicTheme), "Pink200");
        public static ComponentResourceKey Pink300 { get; } = new(typeof(MosaicTheme), "Pink300");
        public static ComponentResourceKey Pink400 { get; } = new(typeof(MosaicTheme), "Pink400");
        public static ComponentResourceKey Pink500 { get; } = new(typeof(MosaicTheme), "Pink500");
        public static ComponentResourceKey Pink600 { get; } = new(typeof(MosaicTheme), "Pink600");
        public static ComponentResourceKey Pink700 { get; } = new(typeof(MosaicTheme), "Pink700");
        public static ComponentResourceKey Pink800 { get; } = new(typeof(MosaicTheme), "Pink800");
        public static ComponentResourceKey Pink900 { get; } = new(typeof(MosaicTheme), "Pink900");

        public static ComponentResourceKey Red50 { get; } = new(typeof(MosaicTheme), "Red50");
        public static ComponentResourceKey Red100 { get; } = new(typeof(MosaicTheme), "Red100");
        public static ComponentResourceKey Red200 { get; } = new(typeof(MosaicTheme), "Red200");
        public static ComponentResourceKey Red300 { get; } = new(typeof(MosaicTheme), "Red300");
        public static ComponentResourceKey Red400 { get; } = new(typeof(MosaicTheme), "Red400");
        public static ComponentResourceKey Red500 { get; } = new(typeof(MosaicTheme), "Red500");
        public static ComponentResourceKey Red600 { get; } = new(typeof(MosaicTheme), "Red600");
        public static ComponentResourceKey Red700 { get; } = new(typeof(MosaicTheme), "Red700");
        public static ComponentResourceKey Red800 { get; } = new(typeof(MosaicTheme), "Red800");
        public static ComponentResourceKey Red900 { get; } = new(typeof(MosaicTheme), "Red900");

        public static ComponentResourceKey Orange50 { get; } = new(typeof(MosaicTheme), "Orange50");
        public static ComponentResourceKey Orange100 { get; } = new(typeof(MosaicTheme), "Orange100");
        public static ComponentResourceKey Orange200 { get; } = new(typeof(MosaicTheme), "Orange200");
        public static ComponentResourceKey Orange300 { get; } = new(typeof(MosaicTheme), "Orange300");
        public static ComponentResourceKey Orange400 { get; } = new(typeof(MosaicTheme), "Orange400");
        public static ComponentResourceKey Orange500 { get; } = new(typeof(MosaicTheme), "Orange500");
        public static ComponentResourceKey Orange600 { get; } = new(typeof(MosaicTheme), "Orange600");
        public static ComponentResourceKey Orange700 { get; } = new(typeof(MosaicTheme), "Orange700");
        public static ComponentResourceKey Orange800 { get; } = new(typeof(MosaicTheme), "Orange800");
        public static ComponentResourceKey Orange900 { get; } = new(typeof(MosaicTheme), "Orange900");

        public static ComponentResourceKey Yellow50 { get; } = new(typeof(MosaicTheme), "Yellow50");
        public static ComponentResourceKey Yellow100 { get; } = new(typeof(MosaicTheme), "Yellow100");
        public static ComponentResourceKey Yellow200 { get; } = new(typeof(MosaicTheme), "Yellow200");
        public static ComponentResourceKey Yellow300 { get; } = new(typeof(MosaicTheme), "Yellow300");
        public static ComponentResourceKey Yellow400 { get; } = new(typeof(MosaicTheme), "Yellow400");
        public static ComponentResourceKey Yellow500 { get; } = new(typeof(MosaicTheme), "Yellow500");
        public static ComponentResourceKey Yellow600 { get; } = new(typeof(MosaicTheme), "Yellow600");
        public static ComponentResourceKey Yellow700 { get; } = new(typeof(MosaicTheme), "Yellow700");
        public static ComponentResourceKey Yellow800 { get; } = new(typeof(MosaicTheme), "Yellow800");
        public static ComponentResourceKey Yellow900 { get; } = new(typeof(MosaicTheme), "Yellow900");

        public static ComponentResourceKey Green50 { get; } = new(typeof(MosaicTheme), "Green50");
        public static ComponentResourceKey Green100 { get; } = new(typeof(MosaicTheme), "Green100");
        public static ComponentResourceKey Green200 { get; } = new(typeof(MosaicTheme), "Green200");
        public static ComponentResourceKey Green300 { get; } = new(typeof(MosaicTheme), "Green300");
        public static ComponentResourceKey Green400 { get; } = new(typeof(MosaicTheme), "Green400");
        public static ComponentResourceKey Green500 { get; } = new(typeof(MosaicTheme), "Green500");
        public static ComponentResourceKey Green600 { get; } = new(typeof(MosaicTheme), "Green600");
        public static ComponentResourceKey Green700 { get; } = new(typeof(MosaicTheme), "Green700");
        public static ComponentResourceKey Green800 { get; } = new(typeof(MosaicTheme), "Green800");
        public static ComponentResourceKey Green900 { get; } = new(typeof(MosaicTheme), "Green900");

        public static ComponentResourceKey Teal50 { get; } = new(typeof(MosaicTheme), "Teal50");
        public static ComponentResourceKey Teal100 { get; } = new(typeof(MosaicTheme), "Teal100");
        public static ComponentResourceKey Teal200 { get; } = new(typeof(MosaicTheme), "Teal200");
        public static ComponentResourceKey Teal300 { get; } = new(typeof(MosaicTheme), "Teal300");
        public static ComponentResourceKey Teal400 { get; } = new(typeof(MosaicTheme), "Teal400");
        public static ComponentResourceKey Teal500 { get; } = new(typeof(MosaicTheme), "Teal500");
        public static ComponentResourceKey Teal600 { get; } = new(typeof(MosaicTheme), "Teal600");
        public static ComponentResourceKey Teal700 { get; } = new(typeof(MosaicTheme), "Teal700");
        public static ComponentResourceKey Teal800 { get; } = new(typeof(MosaicTheme), "Teal800");
        public static ComponentResourceKey Teal900 { get; } = new(typeof(MosaicTheme), "Teal900");

        // Neutral gray scale (useful for surfaces/borders/disabled)
        public static ComponentResourceKey Gray50 { get; } = new(typeof(MosaicTheme), "Gray50");
        public static ComponentResourceKey Gray100 { get; } = new(typeof(MosaicTheme), "Gray100");
        public static ComponentResourceKey Gray200 { get; } = new(typeof(MosaicTheme), "Gray200");
        public static ComponentResourceKey Gray300 { get; } = new(typeof(MosaicTheme), "Gray300");
        public static ComponentResourceKey Gray400 { get; } = new(typeof(MosaicTheme), "Gray400");
        public static ComponentResourceKey Gray500 { get; } = new(typeof(MosaicTheme), "Gray500");
        public static ComponentResourceKey Gray600 { get; } = new(typeof(MosaicTheme), "Gray600");
        public static ComponentResourceKey Gray700 { get; } = new(typeof(MosaicTheme), "Gray700");
        public static ComponentResourceKey Gray800 { get; } = new(typeof(MosaicTheme), "Gray800");
        public static ComponentResourceKey Gray900 { get; } = new(typeof(MosaicTheme), "Gray900");
    }
}
