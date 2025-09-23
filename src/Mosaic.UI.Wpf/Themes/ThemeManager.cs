/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Manages the application's theme and related resources, including support for toggling between light and dark
    /// themes, applying native control styles, and dynamically updating resource dictionaries.
    /// </summary>
    /// <remarks>The <see cref="ThemeManager"/> class provides functionality to manage and apply themes in a
    /// WPF application. It supports dynamic updates to resource dictionaries based on the selected theme mode and
    /// whether native control styles are enabled. The class also raises the <see cref="ThemeChanged"/> event whenever
    /// the theme is updated, allowing subscribers to respond to theme changes.  This class is typically used as a
    /// singleton and is automatically registered as such in the application's service container if not already
    /// registered.</remarks>
    public class ThemeManager : ResourceDictionary, ISupportInitialize
    {
        private bool _updating;
        private bool _initializing;
        private ThemeMode _theme;
        private bool _native;

        /// <summary>
        /// Gets or sets the current theme mode for the application.
        /// </summary>
        public ThemeMode Theme
        {
            get => _theme;
            set
            {
                if (_theme == value)
                {
                    return;
                }

                _theme = value;

                // If we're being initialized from XAML, defer loading until EndInit.
                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the native controls are styled.
        /// </summary>
        public bool Native
        {
            get => _native;
            set
            {
                if (_native == value)
                {
                    return;
                }

                _native = value;

                // If we're being initialized from XAML, defer loading until EndInit.
                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Event that is raised when the theme changes.
        /// </summary>
        public static event EventHandler<ThemeMode>? ThemeChanged;

        /// <summary>
        /// Invokes the ThemeChanged event.
        /// </summary>
        /// <param name="themeName">The new theme name.</param>
        internal static void OnThemeChanged(ThemeMode themeName)
        {
            ThemeChanged?.Invoke(null, themeName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManager"/> class.
        /// </summary>
        /// <remarks>If an instance of <see cref="ThemeManager"/> is not already registered as a singleton
        /// in the application's service container, this constructor registers the current instance.</remarks>
        public ThemeManager()
        {
            if (!AppServices.IsSingletonRegistered<ThemeManager>())
            {
                AppServices.AddSingleton(this);
            }
        }

        /// <summary>
        /// ISupportInitialize - XAML calls BeginInit before setting properties.
        /// </summary>
        public new void BeginInit()
        {
            _initializing = true;
        }

        /// <summary>
        /// ISupportInitialize - XAML calls EndInit after setting properties.
        /// </summary>
        public new void EndInit()
        {
            _initializing = false;
            UpdateMergedDictionaries();
        }

        private void UpdateMergedDictionaries()
        {
            if (_updating)
            {
                return;
            }

            _updating = true;

            try
            {
                var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

                // Find and remove existing theme and brush dictionaries
                var toRemove = new List<ResourceDictionary>();
                foreach (var dict in mergedDictionaries)
                {
                    if (dict.Source != null)
                    {
                        var source = dict.Source.ToString();
                        if (source.Contains("/Themes/Light.xaml") ||
                            source.Contains("/Themes/Dark.xaml") ||
                            source.Contains("SystemColors.xaml") ||
                            source.Contains("/Brushes.xaml") ||
                            source.Contains("/Generic.xaml") ||
                            source.Contains("/Native.xaml")) // Also remove Generic.xaml and Native.xaml
                        {
                            toRemove.Add(dict);
                        }
                    }
                }

                foreach (var dict in toRemove)
                {
                    mergedDictionaries.Remove(dict);
                }

                /*
                 * We're going to load the Light or Dark theme, then the shared Brushes that reference the theme, then our
                 * custom controls in Generic.xaml and finally if the user specifies we'll load the Native controls.
                 */

                if (Theme == ThemeMode.Light)
                {
                    mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Light/Light.xaml") });
                }
                else
                {
                    mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Dark/Dark.xaml") });
                }

                // 3. Re-add the BRUSH dictionary
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Brushes.xaml") });

                // 4. Re-add the custom control dictionaries
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Generic.xaml") });

                // 5. Re-add the native control dictionaries LAST so they can use the theme brushes
                if (Native)
                {
                    mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native.xaml") });
                }

                // Notify subscribers about the theme change
                OnThemeChanged(Theme);
            }
            finally
            {
                _updating = false;
            }
        }

        /// <summary>
        /// Toggles the application's theme between "Light" and "Dark".
        /// </summary>
        public void ToggleTheme()
        {
            Theme = Theme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        }
    }
}