namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Centralized pack URIs for Mosaic theme dictionaries.
    /// </summary>
    internal static class ThemeDictionaryUris
    {
        private const string BasePath = "pack://application:,,,/Mosaic.UI.Wpf;component/Themes";

        public static readonly Uri Typography = new($"{BasePath}/Foundation/Typography.xaml", UriKind.Absolute);
        public static readonly Uri Generic = new($"{BasePath}/Generic.xaml", UriKind.Absolute);
        public static readonly Uri Native = new($"{BasePath}/Native.xaml", UriKind.Absolute);

        public static readonly Uri Light = new($"{BasePath}/Light/Light.xaml", UriKind.Absolute);
        public static readonly Uri Dark = new($"{BasePath}/Dark/Dark.xaml", UriKind.Absolute);
        public static readonly Uri HighContrast = new($"{BasePath}/HighContrast/HighContrast.xaml", UriKind.Absolute);

        public static readonly Uri LightSystemColors = new($"{BasePath}/Light/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri DarkSystemColors = new($"{BasePath}/Dark/SystemColors.xaml", UriKind.Absolute);
        public static readonly Uri HighContrastSystemColors = new($"{BasePath}/HighContrast/SystemColors.xaml", UriKind.Absolute);

        public static Uri GetThemeUri(ThemeMode themeMode)
        {
            return themeMode switch
            {
                ThemeMode.Light => Light,
                ThemeMode.Dark => Dark,
                ThemeMode.HighContrast => HighContrast,
                _ => Dark
            };
        }

        public static Uri GetSystemColorsUri(ThemeMode themeMode)
        {
            return themeMode switch
            {
                ThemeMode.Light => LightSystemColors,
                ThemeMode.Dark => DarkSystemColors,
                ThemeMode.HighContrast => HighContrastSystemColors,
                _ => DarkSystemColors
            };
        }
    }
}
