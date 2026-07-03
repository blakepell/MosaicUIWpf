/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.Themes;
using MosaicTextEditor.Common;
using MosaicTextEditor.Models;
using MosaicTextEditor.Services;
using MosaicTextEditor.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonDockMosaicTheme = Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme;
using FileItem = Mosaic.UI.Wpf.Controls.FileItem;
using FilesControl = Mosaic.UI.Wpf.Controls.Files;
using PropertyGridControl = Mosaic.UI.Wpf.Controls.PropertyGrid;
using SearchBoxControl = Mosaic.UI.Wpf.Controls.SearchBox;

namespace MosaicTextEditor
{
    /// <summary>
    /// The main MosaicTextEditor window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double DefaultFilesDockWidth = 300;
        private const double DefaultPropertiesDockWidth = 280;
        private const double DefaultOutputDockHeight = 150;
        private const double DefaultToolWindowMinWidth = 220;
        private const double DefaultOutputDockMinHeight = 100;

        private readonly AppViewModel _appViewModel;
        private readonly AppSettings _appSettings;
        private readonly Dictionary<Control, EditorDocument> _documentsByControl = new();
        private readonly HashSet<LayoutDocument> _documentsClosingAfterPrompt = new();
        private readonly Dictionary<EditorDocument, LayoutDocument> _layoutDocumentsByDocument = new();
        private readonly MainWindowViewModel _viewModel;
        private LayoutAnchorable? _filesAnchorable;
        private LayoutAnchorable? _outputAnchorable;
        private LayoutAnchorable? _propertiesAnchorable;
        private LayoutAnchorable? _settingsAnchorable;
        private bool _initialized;
        private bool _isClosingConfirmed;
        private bool _suppressDocumentClosePrompt;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            _filesAnchorable = this.FilesAnchorable;
            _propertiesAnchorable = this.PropertiesAnchorable;
            _outputAnchorable = this.OutputAnchorable;

            _appSettings = AppServices.GetRequiredService<AppSettings>();
            _appViewModel = AppServices.GetRequiredService<AppViewModel>();
            _viewModel = new MainWindowViewModel(_appSettings, new EditorDialogService());
            this.DataContext = _viewModel;

            _viewModel.DocumentAdded += this.ViewModel_OnDocumentAdded;
            _viewModel.DocumentFocusRequested += this.ViewModel_OnDocumentFocusRequested;
            _viewModel.ToolWindowRequested += this.ViewModel_OnToolWindowRequested;
            _viewModel.ExitRequested += this.ViewModel_OnExitRequested;

            ThemeManager.ThemeChanged += this.ThemeManager_OnThemeChanged;
            this.Unloaded += this.MainWindow_OnUnloaded;

            this.UpdateThemeMenuChecks(AppServices.GetRequiredService<ThemeManager>().Theme);
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            await _viewModel.InitializeAsync(_appViewModel.StartupPath);
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.ToggleTheme();
        }

        private void ThemeLightMenuItem_OnClick(object sender, RoutedEventArgs e) => this.ApplyTheme(MosaicThemeMode.Light);

        private void ThemeDarkMenuItem_OnClick(object sender, RoutedEventArgs e) => this.ApplyTheme(MosaicThemeMode.Dark);

        private void ThemeBlueMenuItem_OnClick(object sender, RoutedEventArgs e) => this.ApplyTheme(MosaicThemeMode.Blue);

        private void ApplyTheme(MosaicThemeMode themeMode)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.Theme = themeMode;
            _appSettings.Theme = themeMode;
            this.UpdateThemeMenuChecks(themeMode);
        }

        private void FilesSearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            this.ApplyFilesFilter(this.FileExplorerFilesControl, this.FilesSearchBox.Text);
        }

        private void DockingManager_OnActiveContentChanged(object? sender, EventArgs e)
        {
            var activeContent = this.DockingManager.Layout.ActiveContent;
            if (activeContent is LayoutDocument { Content: Control control } && _documentsByControl.TryGetValue(control, out var document))
            {
                _viewModel.SetActiveDocument(document);
                document.EditorControl.Dispatcher.BeginInvoke(document.FocusEditor);
            }
        }

        private void DockingManager_OnAnchorableClosed(object? sender, AnchorableClosedEventArgs e)
        {
            if (ReferenceEquals(e.Anchorable, _filesAnchorable))
            {
                _filesAnchorable = null;
            }
            else if (ReferenceEquals(e.Anchorable, _propertiesAnchorable))
            {
                _propertiesAnchorable = null;
            }
            else if (ReferenceEquals(e.Anchorable, _settingsAnchorable))
            {
                _settingsAnchorable = null;
            }
            else if (ReferenceEquals(e.Anchorable, _outputAnchorable))
            {
                _outputAnchorable = null;
            }
        }

        private async void DockingManager_OnDocumentClosing(object? sender, DocumentClosingEventArgs e)
        {
            if (_suppressDocumentClosePrompt || _documentsClosingAfterPrompt.Remove(e.Document))
            {
                return;
            }

            if (!this.TryGetEditorDocument(e.Document, out var document) || !document.IsModified)
            {
                return;
            }

            e.Cancel = true;

            if (await this.PromptToSaveDocumentAsync(document))
            {
                _documentsClosingAfterPrompt.Add(e.Document);
                e.Document.Close();
            }
        }

        private void EditorControl_OnPreviewKeyDown(LayoutDocument layoutDocument, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F4 &&
                System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                layoutDocument.Close();
                e.Handled = true;
            }
        }

        private void DockingManager_OnDocumentClosed(object? sender, DocumentClosedEventArgs e)
        {
            if (e.Document.Content is not Control control || !_documentsByControl.TryGetValue(control, out var document))
            {
                return;
            }

            document.PropertyChanged -= this.EditorDocument_OnPropertyChanged;
            _documentsByControl.Remove(control);
            _layoutDocumentsByDocument.Remove(document);
            _viewModel.RemoveDocument(document);
        }

        private void ViewModel_OnDocumentAdded(object? sender, EditorDocument document)
        {
            var layoutDocument = this.DockingManager.Add(document.EditorControl, document.Title, activate: true, canClose: true, moveToLast: true);
            layoutDocument.ContentId = document.FilePath ?? document.GetHashCode().ToString();
            layoutDocument.Description = document.FilePath ?? document.FileName;

            _documentsByControl[document.EditorControl] = document;
            _layoutDocumentsByDocument[document] = layoutDocument;
            document.EditorControl.AddHandler(System.Windows.Input.Keyboard.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler((_, e) => this.EditorControl_OnPreviewKeyDown(layoutDocument, e)), true);
            document.PropertyChanged += this.EditorDocument_OnPropertyChanged;

            this.FocusDocument(document);
        }

        private void ViewModel_OnDocumentFocusRequested(object? sender, EditorDocument document)
        {
            this.FocusDocument(document);
        }

        private void ViewModel_OnToolWindowRequested(object? sender, string toolWindow)
        {
            switch (toolWindow)
            {
                case "Files":
                    _filesAnchorable = this.ShowToolWindow(_filesAnchorable, this.CreateFilesToolWindow, AnchorableShowStrategy.Left, DefaultFilesDockWidth, DefaultOutputDockHeight);
                    break;
                case "Properties":
                    _propertiesAnchorable = this.ShowToolWindow(_propertiesAnchorable, this.CreatePropertiesToolWindow, AnchorableShowStrategy.Right, DefaultPropertiesDockWidth, DefaultOutputDockHeight);
                    break;
                case "Settings":
                    _settingsAnchorable = this.ShowToolWindow(_settingsAnchorable, this.CreateSettingsToolWindow, AnchorableShowStrategy.Right, DefaultPropertiesDockWidth, DefaultOutputDockHeight);
                    break;
                case "Output":
                    _outputAnchorable = this.ShowToolWindow(_outputAnchorable, this.CreateOutputToolWindow, AnchorableShowStrategy.Bottom, DefaultFilesDockWidth, DefaultOutputDockHeight);
                    break;
            }
        }

        private void ViewModel_OnExitRequested(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void EditorDocument_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not EditorDocument document || !_layoutDocumentsByDocument.TryGetValue(document, out var layoutDocument))
            {
                return;
            }

            if (e.PropertyName is nameof(EditorDocument.Title) or nameof(EditorDocument.FilePath) or nameof(EditorDocument.FileName))
            {
                layoutDocument.Title = document.Title;
                layoutDocument.Description = document.FilePath ?? document.FileName;

                if (!string.IsNullOrWhiteSpace(document.FilePath))
                {
                    layoutDocument.ContentId = document.FilePath;
                }
            }
        }

        private void FocusDocument(EditorDocument document)
        {
            if (!_layoutDocumentsByDocument.TryGetValue(document, out var layoutDocument))
            {
                return;
            }

            layoutDocument.IsSelected = true;
            layoutDocument.IsActive = true;
            _viewModel.SetActiveDocument(document);
            document.EditorControl.Dispatcher.BeginInvoke(document.FocusEditor);
        }

        private LayoutAnchorable ShowToolWindow(LayoutAnchorable? anchorable, Func<LayoutAnchorable> createToolWindow, AnchorableShowStrategy strategy, double dockWidth, double dockHeight)
        {
            if (anchorable == null)
            {
                anchorable = createToolWindow();
                anchorable.AddToLayout(this.DockingManager, strategy);
            }
            else if (anchorable.IsHidden)
            {
                anchorable.Show();
            }
            else if (!this.IsToolWindowInCurrentLayout(anchorable))
            {
                anchorable = createToolWindow();
                anchorable.AddToLayout(this.DockingManager, strategy);
            }

            ApplyToolWindowDockSize(anchorable, strategy, dockWidth, dockHeight);
            anchorable.IsSelected = true;
            anchorable.IsActive = true;
            return anchorable;
        }

        private bool IsToolWindowInCurrentLayout(LayoutAnchorable anchorable)
        {
            return this.DockingManager.Layout
                .Descendents()
                .OfType<LayoutAnchorable>()
                .Any(candidate => ReferenceEquals(candidate, anchorable));
        }

        private LayoutAnchorable CreateFilesToolWindow()
        {
            var files = new FilesControl
            {
                Background = this.TryFindResource(MosaicTheme.WindowBackgroundBrush) as Brush,
                EnableFileWatcher = true,
                Filter = "*",
                ShowFolders = true
            };
            files.SetBinding(FilesControl.DirectoryPathProperty, new Binding(nameof(MainWindowViewModel.CurrentFolder))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            files.SetBinding(FilesControl.FileActivatedCommandProperty, new Binding(nameof(MainWindowViewModel.OpenActivatedFileCommand)));

            var searchBox = new SearchBoxControl
            {
                Margin = new Thickness(6),
                Watermark = "Search files"
            };
            searchBox.TextChanged += (_, _) => this.ApplyFilesFilter(files, searchBox.Text);

            var panel = new DockPanel();
            DockPanel.SetDock(searchBox, Dock.Top);
            panel.Children.Add(searchBox);
            panel.Children.Add(files);

            return CreateToolWindow("File Explorer", "files", panel);
        }

        private LayoutAnchorable CreatePropertiesToolWindow()
        {
            var grid = new Grid { Margin = new Thickness(10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            AddPropertyRow(grid, 0, "Name", "ActiveDocument.FileName", "-");
            AddPropertyRow(grid, 1, "Path", "ActiveDocument.FilePath", "(unsaved)");
            AddPropertyRow(grid, 2, "Type", "ActiveDocument.Kind", "-");
            AddPropertyRow(grid, 3, "Modified", "ActiveDocument.IsModified", "False");

            return CreateToolWindow("Properties", "properties", grid);
        }

        private LayoutAnchorable CreateSettingsToolWindow()
        {
            var propertyGrid = new PropertyGridControl
            {
                Object = _appSettings,
                RevertInvalidValues = true
            };

            return CreateToolWindow("Settings", "settings", propertyGrid);
        }

        private LayoutAnchorable CreateOutputToolWindow()
        {
            var output = new TextBox
            {
                Margin = new Thickness(0),
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap
            };
            output.SetBinding(TextBox.TextProperty, new Binding(nameof(MainWindowViewModel.StatusText)));

            return CreateToolWindow("Output", "output", output);
        }

        private LayoutAnchorable CreateToolWindow(string title, string contentId, UIElement content)
        {
            if (content is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContext = _viewModel;
            }

            return new LayoutAnchorable
            {
                Title = title,
                ContentId = contentId,
                CanClose = true,
                Content = content
            };
        }

        private static void AddPropertyRow(Grid grid, int row, string label, string bindingPath, string targetNullValue)
        {
            var labelBlock = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                Text = label,
                Margin = row == 0 ? new Thickness(0) : new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(labelBlock, row);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Margin = row == 0 ? new Thickness(12, 0, 0, 0) : new Thickness(12, 8, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            valueBlock.SetBinding(TextBlock.TextProperty, new Binding(bindingPath) { TargetNullValue = targetNullValue });
            Grid.SetRow(valueBlock, row);
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);
        }

        private void ApplyFilesFilter(FilesControl files, string searchText)
        {
            var view = CollectionViewSource.GetDefaultView(files.Items);
            if (view == null)
            {
                return;
            }

            view.Filter = item => FileExplorerFilterMatches(item, searchText);
            view.Refresh();
        }

        private static bool FileExplorerFilterMatches(object item, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            if (item is not FileItem fileItem)
            {
                return false;
            }

            if (fileItem.IsParentNavigation)
            {
                return true;
            }

            return ContainsFilterText(fileItem.Name, searchText)
                || ContainsFilterText(fileItem.FullPath, searchText);
        }

        private static bool ContainsFilterText(string value, string searchText)
        {
            return value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void ThemeManager_OnThemeChanged(object? sender, MosaicThemeMode e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.DockingManager.Theme = new AvalonDockMosaicTheme();
                this.UpdateThemeMenuChecks(e);
            });
        }

        private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            // Once the user has answered every save prompt this handler re-enters with the flag set
            // and lets the close proceed unimpeded.
            if (_isClosingConfirmed)
            {
                return;
            }

            var modifiedDocuments = _viewModel.OpenDocuments.Where(document => document.IsModified).ToList();
            if (modifiedDocuments.Count == 0)
            {
                return;
            }

            // Cancel this close pass while we prompt. The save prompt can complete synchronously
            // (e.g. clicking "No"), so we must not call Close() from here - doing so re-enters the
            // Closing event and throws "Cannot ... Close ... while a Window is closing".
            e.Cancel = true;

            foreach (var document in modifiedDocuments)
            {
                if (!await this.PromptToSaveDocumentAsync(document))
                {
                    // User cancelled; keep the window open.
                    return;
                }
            }

            _isClosingConfirmed = true;
            _suppressDocumentClosePrompt = true;

            // Defer the real close so the current Closing event fully unwinds first, avoiding the
            // re-entrant Close() crash on the synchronous prompt paths.
            _ = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(this.Close));
        }

        private async Task<bool> PromptToSaveDocumentAsync(EditorDocument document)
        {
            this.FocusDocument(document);

            var result = MessageBox.Show(
                this,
                $"Do you want to save changes to \"{document.FileName}\"?",
                "Save Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            return result switch
            {
                MessageBoxResult.Yes => await _viewModel.SaveDocumentWithPromptAsync(document),
                MessageBoxResult.No => true,
                _ => false
            };
        }

        private bool TryGetEditorDocument(LayoutDocument layoutDocument, out EditorDocument document)
        {
            if (layoutDocument.Content is Control control && _documentsByControl.TryGetValue(control, out var editorDocument))
            {
                document = editorDocument;
                return true;
            }

            document = null!;
            return false;
        }

        private static void ApplyToolWindowDockSize(LayoutAnchorable anchorable, AnchorableShowStrategy strategy, double dockWidth, double dockHeight)
        {
            if (anchorable.Parent is not LayoutAnchorablePane pane)
            {
                return;
            }

            bool isHorizontalDock = (strategy & (AnchorableShowStrategy.Left | AnchorableShowStrategy.Right)) != 0;
            if (isHorizontalDock)
            {
                pane.DockMinWidth = DefaultToolWindowMinWidth;
                pane.DockWidth = new GridLength(dockWidth);
                return;
            }

            pane.DockMinHeight = DefaultOutputDockMinHeight;
            pane.DockHeight = new GridLength(dockHeight);
        }

        private void UpdateThemeMenuChecks(MosaicThemeMode themeMode)
        {
            this.ThemeLightMenuItem.IsChecked = themeMode == MosaicThemeMode.Light;
            this.ThemeDarkMenuItem.IsChecked = themeMode == MosaicThemeMode.Dark;
            this.ThemeBlueMenuItem.IsChecked = themeMode == MosaicThemeMode.Blue;
        }

        private void MainWindow_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.DocumentAdded -= this.ViewModel_OnDocumentAdded;
            _viewModel.DocumentFocusRequested -= this.ViewModel_OnDocumentFocusRequested;
            _viewModel.ToolWindowRequested -= this.ViewModel_OnToolWindowRequested;
            _viewModel.ExitRequested -= this.ViewModel_OnExitRequested;

            ThemeManager.ThemeChanged -= this.ThemeManager_OnThemeChanged;
            this.Unloaded -= this.MainWindow_OnUnloaded;
        }
    }
}
