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
    /// Manages Mosaic theme resources and supports runtime theme switching.
    /// </summary>
    /// <remarks>
    /// This dictionary is self-contained and updates its own <see cref="ResourceDictionary.MergedDictionaries"/>
    /// collection only. This lets consumers scope theme resources at the application, window, or control level
    /// without mutating unrelated resource dictionaries.
    /// </remarks>
    public class ThemeManager : ResourceDictionary, ISupportInitialize
    {
        private bool _updating;
        private bool _initializing;
        private readonly List<ResourceDictionary> _managedDictionaries = new();

        private ThemeMode _theme = ThemeMode.Light;
        private bool _native;
        private bool _systemColors = true;
        private bool _typography = true;
        private bool _controlTemplates = true;

        /// <summary>
        /// Gets or sets the active theme mode.
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

                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether native WPF controls should be restyled.
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

                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether system color keys should be overridden.
        /// </summary>
        public bool SystemColors
        {
            get => _systemColors;
            set
            {
                if (_systemColors == value)
                {
                    return;
                }

                _systemColors = value;

                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Mosaic typography resources are loaded.
        /// </summary>
        public bool Typography
        {
            get => _typography;
            set
            {
                if (_typography == value)
                {
                    return;
                }

                _typography = value;

                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Mosaic control template dictionaries are loaded.
        /// </summary>
        public bool ControlTemplates
        {
            get => _controlTemplates;
            set
            {
                if (_controlTemplates == value)
                {
                    return;
                }

                _controlTemplates = value;

                if (!_initializing)
                {
                    UpdateMergedDictionaries();
                }
            }
        }

        /// <summary>
        /// Event that is raised when the active theme mode changes.
        /// </summary>
        public static event EventHandler<ThemeMode>? ThemeChanged;

        /// <summary>
        /// Invokes the <see cref="ThemeChanged"/> event.
        /// </summary>
        internal static void OnThemeChanged(ThemeMode themeMode)
        {
            ThemeChanged?.Invoke(null, themeMode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManager"/> class.
        /// </summary>
        public ThemeManager()
        {
            if (!AppServices.IsSingletonRegistered<ThemeManager>())
            {
                AppServices.AddSingleton(this);
            }
        }

        /// <summary>
        /// Called before XAML property initialization.
        /// </summary>
        public new void BeginInit()
        {
            _initializing = true;
            base.BeginInit();
        }

        void ISupportInitialize.BeginInit()
        {
            BeginInit();
        }

        /// <summary>
        /// Called after XAML property initialization.
        /// </summary>
        public new void EndInit()
        {
            base.EndInit();
            _initializing = false;
            UpdateMergedDictionaries();
        }

        void ISupportInitialize.EndInit()
        {
            EndInit();
        }

        private void UpdateMergedDictionaries()
        {
            if (_updating)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(UpdateMergedDictionaries);
                return;
            }

            _updating = true;

            try
            {
                var existingManagedDictionaries = _managedDictionaries.ToList();
                var nextManagedDictionaries = BuildManagedDictionaries();

                // Add first so lookups never see a transient "resource not found" gap while switching themes.
                foreach (var dictionary in nextManagedDictionaries)
                {
                    MergedDictionaries.Add(dictionary);
                }

                foreach (var dictionary in existingManagedDictionaries)
                {
                    MergedDictionaries.Remove(dictionary);
                }

                _managedDictionaries.Clear();
                _managedDictionaries.AddRange(nextManagedDictionaries);

                OnThemeChanged(Theme);
            }
            finally
            {
                _updating = false;
            }
        }

        private List<ResourceDictionary> BuildManagedDictionaries()
        {
            var dictionaries = new List<ResourceDictionary>();

            if (Typography)
            {
                dictionaries.Add(CreateDictionary(ThemeDictionaryUris.Typography));
            }

            if (SystemColors)
            {
                dictionaries.Add(CreateDictionary(ThemeDictionaryUris.GetSystemColorsUri(Theme)));
            }

            dictionaries.Add(CreateDictionary(ThemeDictionaryUris.GetThemeUri(Theme)));
            dictionaries.Add(CreateDictionary(ThemeDictionaryUris.WindowChrome));

            if (ControlTemplates)
            {
                dictionaries.Add(CreateDictionary(ThemeDictionaryUris.Generic));
            }

            if (Native)
            {
                dictionaries.Add(CreateDictionary(ThemeDictionaryUris.Native));
            }

            return dictionaries;
        }

        private static ResourceDictionary CreateDictionary(Uri source)
        {
            return new ResourceDictionary { Source = source };
        }

        /// <summary>
        /// Toggles between light and dark themes.
        /// </summary>
        public void ToggleTheme()
        {
            Theme = Theme switch
            {
                ThemeMode.Light => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                ThemeMode.HighContrast => ThemeMode.Light,
                _ => ThemeMode.Light
            };
        }

        /// <summary>
        /// Cycles through Light, Dark, and HighContrast themes.
        /// </summary>
        public void CycleTheme()
        {
            Theme = Theme switch
            {
                ThemeMode.Light => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.HighContrast,
                ThemeMode.HighContrast => ThemeMode.Light,
                _ => ThemeMode.Light
            };
        }
    }
}
