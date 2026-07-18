/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using BbsNavigator.Common;
using BbsNavigator.Models;
using BbsNavigator.Views;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.Themes;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace BbsNavigator
{
    /// <summary>
    /// Hosts the BBS directory and docked terminal documents.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Guid, LayoutDocument> _documents = new();
        private LayoutDocument? _userGuideDocument;
        private bool _shutdownStarted;
        private bool _shutdownComplete;
        private AppSettings Settings { get; }

        /// <summary>
        /// Initializes the main application window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _userGuideDocument = InitialUserGuideDocument;
            Settings = AppServices.GetRequiredService<AppSettings>();
            DataContext = Settings;
            ThemeManager.ThemeChanged += ThemeManager_OnThemeChanged;
            UpdateThemeMenuChecks(Settings.Theme);
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, AddBbs_OnClick));
            InputBindings.Add(new KeyBinding(ApplicationCommands.New, Key.N, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, UserGuide_OnClick));
        }

        private BbsProfile? SelectedProfile => BbsTree.SelectedItem as BbsProfile;

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                await app.LoadBbsProfilesAsync(Settings);
            }

            if (BbsTree.Items.Count > 0)
            {
                BbsTree.Focus();
            }
        }

        private void ThemeManager_OnThemeChanged(object? sender, MosaicThemeMode e)
        {
            DockingManager.Theme = new Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme();
            Settings.Theme = e;
            UpdateThemeMenuChecks(e);
        }

        private void ToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var themeManager = AppServices.GetRequiredService<ThemeManager>();
            themeManager.ToggleTheme();
        }

        private void LightTheme_OnClick(object sender, RoutedEventArgs e)
        {
            SetTheme(MosaicThemeMode.Light);
        }

        private void DarkTheme_OnClick(object sender, RoutedEventArgs e)
        {
            SetTheme(MosaicThemeMode.Dark);
        }

        private void BlueTheme_OnClick(object sender, RoutedEventArgs e)
        {
            SetTheme(MosaicThemeMode.Blue);
        }

        /// <summary>
        /// Applies one of the themes exposed by the Setup menu.
        /// </summary>
        private static void SetTheme(MosaicThemeMode theme)
        {
            AppServices.GetRequiredService<ThemeManager>().Theme = theme;
        }

        /// <summary>
        /// Keeps the Theme submenu's check marks synchronized with the active Mosaic theme.
        /// </summary>
        private void UpdateThemeMenuChecks(MosaicThemeMode theme)
        {
            LightThemeMenuItem.IsChecked = theme == MosaicThemeMode.Light;
            DarkThemeMenuItem.IsChecked = theme == MosaicThemeMode.Dark;
            BlueThemeMenuItem.IsChecked = theme == MosaicThemeMode.Blue;
        }

        private void BbsTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedProfile != null)
            {
                OpenProfile(SelectedProfile);
                e.Handled = true;
            }
        }

        private void ConnectSelected_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedProfile != null)
            {
                OpenProfile(SelectedProfile);
            }
        }

        private async void DisconnectSelected_OnClick(object sender, RoutedEventArgs e)
        {
            LayoutDocument? document = DockingManager.Layout?.ActiveContent as LayoutDocument;
            if (document?.Content is BbsTerminalView terminal)
            {
                await terminal.DisposeAsync();
                document.Close();
            }
        }

        private void OpenProfile(BbsProfile profile)
        {
            if (_documents.TryGetValue(profile.Id, out LayoutDocument? existing))
            {
                existing.IsActive = true;
                return;
            }

            var terminal = new BbsTerminalView(profile, Settings);
            LayoutDocument document = DockingManager.Add(terminal, profile.Name, activate: true, canClose: true);
            document.ContentId = $"bbs-{profile.Id:N}";
            _documents[profile.Id] = document;
        }

        private void AddBbs_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new BbsEditorWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                Settings.BbsProfiles.Add(editor.Profile);
            }
        }

        private void ImportBbsList_OnClick(object sender, RoutedEventArgs e)
        {
            string[]? fileNames = WpfUtilities.OpenFilesRequest(
                "Import BBS List",
                "CSV files (*.csv)|*.csv|All files (*.*)|*.*");

            if (fileNames == null || fileNames.Length == 0)
            {
                return;
            }

            int importedCount = 0;
            int skippedCount = 0;
            var errors = new List<string>();

            foreach (string fileName in fileNames)
            {
                try
                {
                    BbsListImportResult result = BbsListCsvImporter.Import(fileName);
                    foreach (BbsProfile profile in result.Profiles)
                    {
                        Settings.BbsProfiles.Add(profile);
                    }

                    importedCount += result.Profiles.Count;
                    skippedCount += result.SkippedCount;
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(fileName)}: {ex.Message}");
                }
            }

            string summary = $"Imported {importedCount:N0} BBS profile{(importedCount == 1 ? string.Empty : "s")}.";
            if (skippedCount > 0)
            {
                summary += $"\nSkipped {skippedCount:N0} row{(skippedCount == 1 ? string.Empty : "s")} without a valid TelnetAddress and bbsPort.";
            }

            if (errors.Count > 0)
            {
                summary += $"\n\nCould not import:\n{string.Join("\n", errors)}";
            }

            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                summary,
                "Import BBS List",
                MessageBoxButton.OK,
                errors.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void EditBbs_OnClick(object sender, RoutedEventArgs e)
        {
            BbsProfile? profile = SelectedProfile;
            if (profile == null)
            {
                return;
            }

            var editor = new BbsEditorWindow(profile) { Owner = this };
            if (editor.ShowDialog() != true)
            {
                return;
            }

            bool endpointChanged = !string.Equals(profile.Host, editor.Profile.Host, StringComparison.OrdinalIgnoreCase) ||
                                   profile.Port != editor.Profile.Port;
            profile.Name = editor.Profile.Name;
            profile.Host = editor.Profile.Host;
            profile.Port = editor.Profile.Port;
            profile.Description = editor.Profile.Description;
            profile.AutoReconnect = editor.Profile.AutoReconnect;
            profile.LocalEcho = editor.Profile.LocalEcho;
            profile.BackspaceSendsDelete = editor.Profile.BackspaceSendsDelete;
            profile.TerminalEncoding = editor.Profile.TerminalEncoding;

            if (_documents.TryGetValue(profile.Id, out LayoutDocument? document))
            {
                if (endpointChanged)
                {
                    document.Close();
                }
                else
                {
                    document.Title = profile.Name;
                }
            }
        }

        private void RemoveBbs_OnClick(object sender, RoutedEventArgs e)
        {
            BbsProfile? profile = SelectedProfile;
            if (profile == null)
            {
                return;
            }

            MessageBoxResult result = Mosaic.UI.Wpf.Controls.MessageBox.Show(
                $"Remove ‘{profile.Name}’ from the directory?",
                "BBS Navigator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (_documents.TryGetValue(profile.Id, out LayoutDocument? document))
            {
                document.Close();
            }

            Settings.BbsProfiles.Remove(profile);
        }

        /// <summary>
        /// Returns the terminal in the active document tab, if any.
        /// </summary>
        private BbsTerminalView? ActiveTerminal =>
            (DockingManager.Layout?.ActiveContent as LayoutDocument)?.Content as BbsTerminalView;

        private async void UploadFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActiveTerminal is { } terminal)
            {
                await terminal.StartUploadAsync();
            }
            else
            {
                ShowNoActiveSessionMessage();
            }
        }

        private async void DownloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActiveTerminal is { } terminal)
            {
                await terminal.StartDownloadAsync();
            }
            else
            {
                ShowNoActiveSessionMessage();
            }
        }

        private void ToggleCapture_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActiveTerminal is { } terminal)
            {
                terminal.ToggleCapture();
            }
            else
            {
                ShowNoActiveSessionMessage();
            }
        }

        private void OpenDownloadFolder_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string folder = Settings.ResolveDownloadFolder();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    $"The download folder could not be opened.\n\n{ex.Message}",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static void ShowNoActiveSessionMessage()
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                "Open a BBS session first: double-click a system in the directory.",
                "BBS Navigator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Options_OnClick(object sender, RoutedEventArgs e)
        {
            ShowOptions();
        }

        /// <summary>
        /// Opens the application options dialog. Also used by <c>app:options</c> links in the
        /// user guide.
        /// </summary>
        public void ShowOptions()
        {
            new SettingsWindow(Settings) { Owner = this }.ShowDialog();
        }

        /// <summary>
        /// Opens the add-BBS dialog. Also used by <c>app:add-bbs</c> links in the user guide.
        /// </summary>
        public void ShowAddBbs()
        {
            AddBbs_OnClick(this, new RoutedEventArgs());
        }

        private void UserGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (_userGuideDocument != null)
            {
                _userGuideDocument.IsActive = true;
                return;
            }

            var view = new UserGuideView();
            _userGuideDocument = DockingManager.Add(view, "User Guide", activate: true, canClose: true);
            _userGuideDocument.ContentId = "user-guide";
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                "BBS Navigator\n\n" +
                "A Mosaic UI terminal client for classic bulletin board systems.\n\n" +
                "• Telnet with CP437, UTF-8, and Latin-1 text encodings\n" +
                "• ZMODEM, YMODEM, and XMODEM file transfers (auto-detects ZMODEM downloads)\n" +
                "• Session capture, keepalives, and automatic reconnection\n\n" +
                "Tip: hold Ctrl and scroll the mouse wheel to zoom the terminal font.",
                "About BBS Navigator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void DockingManager_OnDocumentClosed(object? sender, DocumentClosedEventArgs e)
        {
            if (e.Document.Content is UserGuideView)
            {
                _userGuideDocument = null;
                return;
            }

            if (e.Document.Content is not BbsTerminalView terminal)
            {
                return;
            }

            _documents.Remove(terminal.Profile.Id);
            await terminal.DisposeAsync();
        }

        /// <inheritdoc />
        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_shutdownComplete)
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            base.OnClosing(e);

            if (_shutdownStarted)
            {
                return;
            }

            _shutdownStarted = true;

            foreach (LayoutDocument document in _documents.Values.ToArray())
            {
                if (document.Content is BbsTerminalView terminal)
                {
                    await terminal.DisposeAsync();
                }
            }

            _documents.Clear();
            _shutdownComplete = true;
            _ = Dispatcher.BeginInvoke(Close);
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_OnThemeChanged;
            base.OnClosed(e);
        }
    }
}
