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
using AvalonDockMosaicTheme = Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme;
using FilesControl = Mosaic.UI.Wpf.Controls.Files;

namespace MosaicTextEditor
{
    /// <summary>
    /// The main MosaicTextEditor window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppViewModel _appViewModel;
        private readonly Dictionary<Control, EditorDocument> _documentsByControl = new();
        private readonly Dictionary<EditorDocument, LayoutDocument> _layoutDocumentsByDocument = new();
        private readonly MainWindowViewModel _viewModel;
        private LayoutAnchorable? _filesAnchorable;
        private LayoutAnchorable? _outputAnchorable;
        private LayoutAnchorable? _propertiesAnchorable;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            _filesAnchorable = this.FilesAnchorable;
            _propertiesAnchorable = this.PropertiesAnchorable;
            _outputAnchorable = this.OutputAnchorable;

            var appSettings = AppServices.GetRequiredService<AppSettings>();
            _appViewModel = AppServices.GetRequiredService<AppViewModel>();
            _viewModel = new MainWindowViewModel(appSettings, new EditorDialogService());
            this.DataContext = _viewModel;

            _viewModel.DocumentAdded += this.ViewModel_OnDocumentAdded;
            _viewModel.DocumentFocusRequested += this.ViewModel_OnDocumentFocusRequested;
            _viewModel.ToolWindowRequested += this.ViewModel_OnToolWindowRequested;
            _viewModel.ExitRequested += this.ViewModel_OnExitRequested;

            ThemeManager.ThemeChanged += this.ThemeManager_OnThemeChanged;
            this.Unloaded += this.MainWindow_OnUnloaded;
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
            else if (ReferenceEquals(e.Anchorable, _outputAnchorable))
            {
                _outputAnchorable = null;
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
                    _filesAnchorable = this.ShowToolWindow(_filesAnchorable, this.CreateFilesToolWindow, AnchorableShowStrategy.Left);
                    break;
                case "Properties":
                    _propertiesAnchorable = this.ShowToolWindow(_propertiesAnchorable, this.CreatePropertiesToolWindow, AnchorableShowStrategy.Right);
                    break;
                case "Output":
                    _outputAnchorable = this.ShowToolWindow(_outputAnchorable, this.CreateOutputToolWindow, AnchorableShowStrategy.Bottom);
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

        private LayoutAnchorable ShowToolWindow(LayoutAnchorable? anchorable, Func<LayoutAnchorable> createToolWindow, AnchorableShowStrategy strategy)
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

            return CreateToolWindow("Files", "files", files);
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

        private LayoutAnchorable CreateOutputToolWindow()
        {
            var output = new TextBox
            {
                Margin = new Thickness(0),
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
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

        private void ThemeManager_OnThemeChanged(object? sender, MosaicThemeMode e)
        {
            this.Dispatcher.Invoke(() => this.DockingManager.Theme = new AvalonDockMosaicTheme());
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
