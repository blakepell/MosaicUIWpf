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
        private bool _shutdownStarted;
        private bool _shutdownComplete;
        private AppSettings Settings { get; }

        /// <summary>
        /// Initializes the main application window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Settings = AppServices.GetRequiredService<AppSettings>();
            DataContext = Settings;
            ThemeManager.ThemeChanged += ThemeManager_OnThemeChanged;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, AddBbs_OnClick));
            InputBindings.Add(new KeyBinding(ApplicationCommands.New, Key.N, ModifierKeys.Control));
        }

        private BbsProfile? SelectedProfile => BbsTree.SelectedItem as BbsProfile;

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (BbsTree.Items.Count > 0)
            {
                BbsTree.Focus();
            }
        }

        private void ThemeManager_OnThemeChanged(object? sender, MosaicThemeMode e)
        {
            DockingManager.Theme = new Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme();
        }

        private void ToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var themeManager = AppServices.GetRequiredService<ThemeManager>();
            themeManager.ToggleTheme();
            Settings.Theme = themeManager.Theme;
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

            var terminal = new BbsTerminalView(profile, Settings.FontSize, Settings.ReconnectDelaySeconds);
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

        private void Options_OnClick(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(Settings) { Owner = this }.ShowDialog();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                "BBS Navigator\n\nA Mosaic UI terminal client for classic bulletin board systems.",
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
