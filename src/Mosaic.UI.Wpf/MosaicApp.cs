/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;

namespace Mosaic.UI.Wpf
{
    /// <summary>
    /// Mosaic UI Application class to manage themes and resources.
    /// </summary>
    public class MosaicApp : Application
    {
        /// <summary>
        /// Represents the current theme setting for the application.
        /// </summary>
        public static string Theme = "Dark";

        /// <summary>
        /// Event that is raised when the theme changes.
        /// </summary>
        public static event EventHandler<string> ThemeChanged;

        /// <summary>
        /// Gets or sets a thread-safe collection of cached resource dictionaries, keyed by their identifiers.
        /// </summary>
        private static ConcurrentDictionary<string, ResourceDictionary> CachedResources { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="MosaicApp"/> class.
        /// </summary>
        static MosaicApp()
        {
            CachedResources = new();
        }

        /// <summary>
        /// Toggles the application's theme between "Light" and "Dark".
        /// </summary>
        public static void ToggleTheme()
        {
            if (Theme == "Light")
            {
                Theme = "Dark";
                ChangeTheme(Theme);
            }
            else
            {
                Theme = "Light";
                ChangeTheme(Theme);
            }
        }

        /// <summary>
        /// Changes the application's theme by updating the resource dictionaries with the specified theme.
        /// </summary>
        /// <param name="themeName">The name of the theme to apply. Supported values are <see langword="Light"/> and <see langword="Dark"/>.</param>
        public static void ChangeTheme(string themeName)
        {
            Application.Current.Resources.MergedDictionaries.Clear();

            if (themeName == "Light")
            {
                if (CachedResources.TryGetValue("Light", out var cachedResource1))
                {
                    Application.Current.Resources.MergedDictionaries.Add(cachedResource1);
                }
                else
                {
                    var dict = new ResourceDictionary()
                    {
                        Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Light.xaml")
                    };

                    CachedResources.TryAdd("Light", dict);
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                if (CachedResources.TryGetValue("aero2.normalcolor.xaml", out var cachedResource2))
                {
                    Application.Current.Resources.MergedDictionaries.Add(cachedResource2);
                }
                else
                {
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native/aero2.normalcolor.xaml")
                    };

                    CachedResources.TryAdd("aero2.normalcolor.xaml", dict);
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }
            }
            else if (themeName == "Dark")
            {
                if (CachedResources.TryGetValue("Dark", out var cachedResource1))
                {
                    Application.Current.Resources.MergedDictionaries.Add(cachedResource1);
                }
                else
                {
                    var dict = new ResourceDictionary()
                    {
                        Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Dark.xaml")
                    };

                    CachedResources.TryAdd("Dark", dict);
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                if (CachedResources.TryGetValue("aero2.darkcolor.xaml", out var cachedResource2))
                {
                    Application.Current.Resources.MergedDictionaries.Add(cachedResource2);
                }
                else
                {
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native/aero2.darkcolor.xaml")
                    };

                    CachedResources.TryAdd("aero2.darkcolor.xaml", dict);
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }
            }

            // Notify subscribers about the theme change
            ThemeChanged?.Invoke(null, themeName);
        }
    }
}
