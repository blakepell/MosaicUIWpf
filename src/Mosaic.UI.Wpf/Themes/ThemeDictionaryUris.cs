namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Centralized pack URIs for Mosaic theme dictionaries.
    /// </summary>
    internal static class ThemeDictionaryUris
    {
        private const string BasePath = "pack://application:,,,/Mosaic.UI.Wpf;component/Themes";

        /// <summary>
        /// Gets the pack URI for the theme-independent Foundation color palette.
        /// </summary>
        public static readonly Uri Palette = new($"{BasePath}/Foundation/Palette.xaml", UriKind.Absolute);
        public static readonly Uri Typography = new($"{BasePath}/Foundation/Typography.xaml", UriKind.Absolute);
        public static readonly Uri WindowChrome = new($"{BasePath}/Foundation/WindowChrome.xaml", UriKind.Absolute);
        public static readonly Uri Generic = new($"{BasePath}/Generic.xaml", UriKind.Absolute);
        public static readonly Uri Native = new($"{BasePath}/Native.xaml", UriKind.Absolute);

        public static readonly Uri Light = new($"{BasePath}/Light/Light.xaml", UriKind.Absolute);
        public static readonly Uri Dark = new($"{BasePath}/Dark/Dark.xaml", UriKind.Absolute);
        public static readonly Uri Blue = new($"{BasePath}/Blue/Blue.xaml", UriKind.Absolute);
        public static readonly Uri Black = new($"{BasePath}/Black/Black.xaml", UriKind.Absolute);
        public static readonly Uri Blackest = new($"{BasePath}/Blackest/Blackest.xaml", UriKind.Absolute);
        public static readonly Uri HighContrast = new($"{BasePath}/HighContrast/HighContrast.xaml", UriKind.Absolute);

        public static readonly Uri LightSystemColors = new($"{BasePath}/Light/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri DarkSystemColors = new($"{BasePath}/Dark/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri BlueSystemColors = new($"{BasePath}/Blue/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri BlackSystemColors = new($"{BasePath}/Black/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri HighContrastSystemColors = new($"{BasePath}/HighContrast/SystemColors.xaml", UriKind.Absolute);

        private static readonly Uri RoundedButtonsLight = new($"{BasePath}/Buttons/RoundedLight.xaml", UriKind.Absolute);
        private static readonly Uri RoundedButtonsDark = new($"{BasePath}/Buttons/RoundedDark.xaml", UriKind.Absolute);
        private static readonly Uri RoundedButtonsBlackest = new($"{BasePath}/Buttons/RoundedBlackest.xaml", UriKind.Absolute);
        private static readonly Uri RoundedButtonsBlue = new($"{BasePath}/Buttons/RoundedBlue.xaml", UriKind.Absolute);

        public static Uri? GetRoundedButtonPaletteUri(MosaicThemeMode theme)
        {
            return theme switch
            {
                MosaicThemeMode.Light => RoundedButtonsLight,
                MosaicThemeMode.Blue => RoundedButtonsBlue,
                MosaicThemeMode.Blackest => RoundedButtonsBlackest,
                MosaicThemeMode.HighContrast => null,
                _ => RoundedButtonsDark
            };
        }

        public static Uri GetThemeUri(MosaicThemeMode MosaicThemeMode)
        {
            return MosaicThemeMode switch
            {
                MosaicThemeMode.Light => Light,
                MosaicThemeMode.Dark => Dark,
                MosaicThemeMode.Blue => Blue,
                MosaicThemeMode.Black => Black,
                MosaicThemeMode.Blackest => Blackest,
                MosaicThemeMode.HighContrast => HighContrast,
                _ => Dark
            };
        }

        public static Uri GetSystemColorsUri(MosaicThemeMode MosaicThemeMode)
        {
            return MosaicThemeMode switch
            {
                MosaicThemeMode.Light => LightSystemColors,
                MosaicThemeMode.Dark => DarkSystemColors,
                MosaicThemeMode.Blue => BlueSystemColors,
                MosaicThemeMode.Black or MosaicThemeMode.Blackest => BlackSystemColors,
                MosaicThemeMode.HighContrast => HighContrastSystemColors,
                _ => DarkSystemColors
            };
        }
    }
}
