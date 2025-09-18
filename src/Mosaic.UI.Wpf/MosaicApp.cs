/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Collections;
using Argus.IO;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;
using System.Collections.Concurrent;
using System.Runtime;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf
{
    /// <summary>
    /// Static helper class for MosaicApp functionality that doesn't require generic type parameters.
    /// </summary>
    public static class MosaicApp
    {
        /// <summary>
        /// Represents the current theme setting for the application.
        /// </summary>
        public static string Theme { get; set; } = "Dark";

        /// <summary>
        /// Event that is raised when the theme changes.
        /// </summary>
        public static event EventHandler<string>? ThemeChanged;

        /// <summary>
        /// Invokes the ThemeChanged event.
        /// </summary>
        /// <param name="themeName">The new theme name.</param>
        internal static void OnThemeChanged(string themeName)
        {
            Theme = themeName;
            ThemeChanged?.Invoke(null, themeName);
        }
    }

    /// <summary>
    /// Mosaic UI Application class to manage themes and resources.
    /// </summary>
    public abstract class MosaicApp<TSettings, TApplicationViewModel> : Application
        where TSettings : class, new()
        where TApplicationViewModel : class, new()
    {
        /// <summary>
        /// The name of the application folder to use wherever it's base folder is stored.
        /// </summary>
        public static string AppFolder { get; set; } = @"Apps\MosaicTemplateApp";

        /// <summary>
        /// Program ID used for file association with Windows.
        /// </summary>
        public static string ProgId { get; set; } = "Mosaic_MosaicTemplateApp";

        /// <summary>
        /// The command line arguments passed into the application on startup.
        /// </summary>
        public static string[]? CommandLineArguments { get; set; }

        /// <summary>
        /// Whether command line arguments were passed in or not.
        /// </summary>
        public static bool HasArguments { get; set; }

        /// <summary>
        /// If the App has completed initialization as per the ApexGateApp class.
        /// </summary>
        public static bool Initialized { get; set; } = false;

        /// <summary>
        /// If the App should use multicore JIT to speed startup times.  This if enabled will create some preprocessed
        /// code that the JIT will then not have to do at runtime.  It stores this in the location provided in OnStartup.
        /// </summary>
        public static bool UseMultiCoreJit { get; set; } = false;

        /// <summary>
        /// Cancellation tokens for this app that are in play or were in play.
        /// </summary>
        public static ThreadSafeList<CancellationTokenSource> CancellationTokenSource { get; set; } = new();

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

        protected new void OnStartup(StartupEventArgs e)
        {
            if (UseMultiCoreJit)
            {
                try
                {
                    // JIT performance optimization, disabled by default but can be changed by the calling program.
                    ProfileOptimization.SetProfileRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                    ProfileOptimization.StartProfile($"{ProgId}.Startup.Profile");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error: MultiCoreJit", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Populate the command line arguments if we have any.
            if (e.Args.Length > 0)
            {
                HasArguments = true;
                CommandLineArguments = e.Args;
            }

            // Load our persisted settings.
            LoadSettings();

            // Create a new instance of the application view model and register it with DI.
            var appVm = new TApplicationViewModel();
            AppServices.AddSingleton(appVm);

            Initialized = true;

        }

        /// <summary>
        /// OnExit that the calling application should call into for any shared code to run on
        /// application exit.
        /// </summary>
        /// <param name="e"></param>
        protected new void OnExit(ExitEventArgs e)
        {
            SaveSettings();
        }

        /// <summary>
        /// Loads the AppSettings.
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                // First, get the client settings file that will tell us where we load our AppSettings from.
                var appFolderFs = new JsonFileService(Environment.SpecialFolder.LocalApplicationData, AppFolder);
                string localSettings = Path.Combine(appFolderFs.FolderPath, "LocalSettings.json");

                // If the file is 0 length it's corrupt, delete it and let it re-initialize.
                var fi = new FileInfo(Path.Combine(localSettings));

                if (fi is { Exists: true, Length: 0 })
                {
                    fi.Delete();
                }

                // Didn't exist, instantiate it and save it after making sure there's a DocumentsFolder on it.
                var clientSettings = appFolderFs.Read<LocalSettings>("LocalSettings.json") ?? new();

                // If there is no document folder set, use the default document folder in the users documents.
                if (string.IsNullOrWhiteSpace(clientSettings.DocumentsFolder))
                {
                    clientSettings.DocumentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppFolder);
                }

                appFolderFs.Save(clientSettings, "LocalSettings.json");

                // Put the client settings in DI.
                AppServices.AddSingleton(clientSettings);

                // Set up our JSON file service for the folder the user designates to save app content in.
                var fs = new JsonFileService(clientSettings.DocumentsFolder);

                // Add the documents JsonFileService into DI since it will be our most used.
                AppServices.AddSingleton(fs);

                // Try to read the settings in from the location.
                var appSettings = fs.Read<TSettings>("AppSettings.json");

                // Didn't exist, instantiate it and save it.
                if (appSettings == null)
                {
                    appSettings = new();
                    fs.Save(appSettings, "AppSettings.json");
                }

                if (appSettings is IAppSettings settings)
                {
                    settings.ApplicationDataFolder = appFolderFs.FolderPath;

                    // This won't save with the AppSettings, but it is being placed on the AppSettings
                    // so the user can edit it with the settings in a PropertyGrid.
                    settings.ClientSettings = clientSettings;

                    // If a theme is specified, set it.
                    if (!string.IsNullOrWhiteSpace(settings.Theme))
                    {
                        ChangeTheme(settings.Theme);
                    }
                }

                // Finally, add the settings into the DI container.
                AppServices.AddSingleton(appSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppServices.AddSingleton(new TSettings());
            }
        }

        /// <summary>
        /// Saves the AppSettings.
        /// </summary>
        public static void SaveSettings()
        {
            // Save the client settings
            SaveClientSettings();

            // Save the app settings.
            SaveAppSettings();
        }

        /// <summary>
        /// Saves AppSettings.json in the ClientSettings documents folder.
        /// </summary>
        public static void SaveAppSettings()
        {
            try
            {
                // Save the AppSettings.
                var appSettings = AppServices.GetRequiredService<TSettings>() ?? new TSettings();

                if (!AppServices.IsRegistered<JsonFileService>())
                {
                    MessageBox.Show("SaveAppSettings(): JsonFileService as not registered. Save aborted", "Save Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Backup the existing file.
                var fs = AppServices.GetRequiredService<JsonFileService>();

                // Save the settings file.
                fs.JsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                fs.Save(appSettings, "AppSettings.json.bak");

                string backupFile = Path.Combine(fs.FolderPath, "AppSettings.json.bak");
                string settingsFile = Path.Combine(fs.FolderPath, "AppSettings.json");

                // Only copy the settings file if it exists.
                if (File.Exists(backupFile))
                {
                    File.Copy(backupFile, settingsFile, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the LocalSettings.json file.  This file is specific to this computer and it holds the pointer
        /// to where the AppSettings file is saved.
        /// </summary>
        public static void SaveClientSettings()
        {
            try
            {
                if (!AppServices.IsRegistered<LocalSettings>())
                {
                    MessageBox.Show("SaveClientSettings(): LocalSettings was not registered. Save aborted.", "Save Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var clientSettings = AppServices.GetRequiredService<LocalSettings>();
                var appFolderFs = new JsonFileService(Environment.SpecialFolder.LocalApplicationData, AppFolder);
                appFolderFs.Save(clientSettings, "LocalSettings.json");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Toggles the application's theme between "Light" and "Dark".
        /// </summary>
        public static void ToggleTheme()
        {
            if (MosaicApp.Theme == "Light")
            {
                ChangeTheme("Dark");
            }
            else
            {
                ChangeTheme("Light");
            }
        }

        /// <summary>
        /// Changes the application's theme by updating the resource dictionaries with the specified theme.
        /// </summary>
        /// <param name="themeName">The name of the theme to apply. Supported values are <see langword="Light"/> and <see langword="Dark"/>.</param>
        public static void ChangeTheme(string themeName)
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
                        source.Contains("aero2.normalcolor.xaml") ||
                        source.Contains("aero2.darkcolor.xaml") ||
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

            // Load resources in correct order: aero2 first, then our theme colors to override
            if (themeName == "Light")
            {
                // 1. Load aero2 base styles first
                //mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native/aero2.normalcolor.xaml") });
                // 2. Load our theme colors to override aero2 where needed
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Light/Light.xaml") });
            }
            else // Dark
            {
                // 1. Load aero2 base styles first
                //mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native/aero2.darkcolor.xaml") });
                // 2. Load our theme colors to override aero2 where needed
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Dark/Dark.xaml") });
            }

            // 3. Re-add the BRUSH dictionary
            mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Brushes.xaml") });
            
            // 4. Re-add the custom control dictionaries
            mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Generic.xaml") });
            
            // 5. Re-add the native control dictionaries LAST so they can use the theme brushes
            mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Native.xaml") });

            // Notify subscribers about the theme change
            MosaicApp.OnThemeChanged(themeName);
        }
    }
}
