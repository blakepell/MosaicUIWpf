/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Mosaic.UI.Wpf.Input;
using Mosaic.UI.Wpf.Themes;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls.Primitives;
using System.Xml;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A code editor built on AvalonEdit's <see cref="TextEditor"/> that integrates with the Mosaic
    /// theming system and provides bundled, theme-aware syntax highlighting selected via the
    /// <see cref="Language"/> property.
    /// </summary>
    /// <remarks>
    /// Custom HotKeys/Chords:
    ///
    /// - Comment Selection: Ctrl+K, Ctrl+C
    /// - Uncomment Selection: Ctrl+K, Ctrl+U
    /// - Move Line or Selection Up: Ctrl+Up
    /// - Move Line or Selection Down: Ctrl+Down
    /// - Go To Line: Ctrl+G
    /// 
    /// The editor defaults to Consolas 12 with line numbers enabled. Colors for the editing surface
    /// (background, foreground, line numbers, selection, current line) are derived from the Mosaic
    /// theme indicated by <see cref="Theme"/>. When <see cref="FollowGlobalTheme"/> is <c>true</c>
    /// (the default) the editor also tracks <see cref="ThemeManager.ThemeChanged"/>.
    /// </remarks>
    [TemplatePart(Name = PartStatusBar, Type = typeof(StatusBar))]
    [TemplatePart(Name = PartLineTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PartColumnTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PartCharacterTextBlock, Type = typeof(TextBlock))]
    [DefaultEvent(nameof(ContextMenuRequested))]
    [DefaultProperty(nameof(Language))]
    public class SyntaxEditor : TextEditor
    {
        private const string PartStatusBar = "PART_StatusBar";
        private const string PartLineTextBlock = "PART_LineTextBlock";
        private const string PartColumnTextBlock = "PART_ColumnTextBlock";
        private const string PartCharacterTextBlock = "PART_CharacterTextBlock";
        private const string XshdResourceFormat = "Mosaic.UI.Wpf.Assets.{0}.{1}.xshd";
        private static readonly Uri SearchPanelResourceUri = new("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/AvalonEdit/SearchPanel.xaml", UriKind.Absolute);

        // Cache parsed highlighting definitions keyed by "Base|ThemeSuffix" so repeated theme/language
        // toggles do not re-parse the embedded xshd each time.
        private static readonly Dictionary<string, IHighlightingDefinition> HighlightingCache = new();

        // Cache the per-theme resource dictionaries so we resolve the theme color tokens only once.
        private static readonly Dictionary<MosaicThemeMode, ResourceDictionary> ThemeDictionaryCache = new();

        private SearchPanel? _searchPanel;
        private bool _subscribedToGlobalTheme;
        private StatusBar? _statusBar;
        private TextBlock? _lineTextBlock;
        private TextBlock? _columnTextBlock;
        private TextBlock? _characterTextBlock;

        #region Routed Commands

        /// <summary>
        /// Command that re-formats (pretty-prints) the JSON document with indentation.
        /// </summary>
        public static readonly RoutedUICommand FormatJsonCommand = new("Format JSON", nameof(FormatJsonCommand), typeof(SyntaxEditor));

        /// <summary>
        /// Command that minifies (compacts) the JSON document onto a single line.
        /// </summary>
        public static readonly RoutedUICommand MinifyJsonCommand = new("Minify JSON", nameof(MinifyJsonCommand), typeof(SyntaxEditor));

        /// <summary>
        /// Command that validates the JSON document and reports the result.
        /// </summary>
        public static readonly RoutedUICommand ValidateJsonCommand = new("Validate JSON", nameof(ValidateJsonCommand), typeof(SyntaxEditor));

        /// <summary>
        /// Command that comments the selected lines, or the current line when there is no selection.
        /// </summary>
        public static readonly RoutedUICommand CommentSelectionCommand = new(
            "Comment Selection",
            nameof(CommentSelectionCommand),
            typeof(SyntaxEditor),
            new InputGestureCollection { new KeyChordGesture(ModifierKeys.Control, Key.K, ModifierKeys.Control, Key.C) });

        /// <summary>
        /// Command that uncomments the selected lines, or the current line when there is no selection.
        /// </summary>
        public static readonly RoutedUICommand UncommentSelectionCommand = new(
            "Uncomment Selection",
            nameof(UncommentSelectionCommand),
            typeof(SyntaxEditor),
            new InputGestureCollection { new KeyChordGesture(ModifierKeys.Control, Key.K, ModifierKeys.Control, Key.U) });

        /// <summary>
        /// Command that moves the selected lines, or the current line, up by one line.
        /// </summary>
        public static readonly RoutedUICommand MoveSelectionUpCommand = new(
            "Move Selection Up",
            nameof(MoveSelectionUpCommand),
            typeof(SyntaxEditor),
            new InputGestureCollection { new KeyGesture(Key.Up, ModifierKeys.Control) });

        /// <summary>
        /// Command that moves the selected lines, or the current line, down by one line.
        /// </summary>
        public static readonly RoutedUICommand MoveSelectionDownCommand = new(
            "Move Selection Down",
            nameof(MoveSelectionDownCommand),
            typeof(SyntaxEditor),
            new InputGestureCollection { new KeyGesture(Key.Down, ModifierKeys.Control) });

        /// <summary>
        /// Command that prompts for a line number and moves the caret to that line.
        /// </summary>
        public static readonly RoutedUICommand GoToLineCommand = new(
            "Go To Line",
            nameof(GoToLineCommand),
            typeof(SyntaxEditor),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="ContextMenuRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ContextMenuRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(ContextMenuRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<SyntaxEditorContextMenuEventArgs>),
            typeof(SyntaxEditor));

        /// <summary>
        /// Raised after the editor has populated the standard and language-specific context menu items
        /// but before the menu is shown, allowing consumers to customize the menu for the current file type.
        /// </summary>
        public event EventHandler<SyntaxEditorContextMenuEventArgs> ContextMenuRequested
        {
            add => this.AddHandler(ContextMenuRequestedEvent, value);
            remove => this.RemoveHandler(ContextMenuRequestedEvent, value);
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Theme"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
            nameof(Theme),
            typeof(MosaicThemeMode),
            typeof(SyntaxEditor),
            new FrameworkPropertyMetadata(MosaicThemeMode.Light, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnThemeChanged));

        /// <summary>
        /// Gets or sets the Mosaic theme that drives the editor's surface colors and the variant of the
        /// syntax highlighting definition that is loaded.
        /// </summary>
        [Category("Mosaic")]
        [Description("The Mosaic theme that drives the editor's surface colors and syntax highlighting variant.")]
        public MosaicThemeMode Theme
        {
            get => (MosaicThemeMode)this.GetValue(ThemeProperty);
            set => this.SetValue(ThemeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Language"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LanguageProperty = DependencyProperty.Register(
            nameof(Language),
            typeof(SyntaxLanguage),
            typeof(SyntaxEditor),
            new FrameworkPropertyMetadata(SyntaxLanguage.None, OnLanguageChanged));

        /// <summary>
        /// Gets or sets the language whose bundled syntax highlighting definition should be applied.
        /// </summary>
        [Category("Mosaic")]
        [Description("The language whose bundled syntax highlighting definition should be applied.")]
        public SyntaxLanguage Language
        {
            get => (SyntaxLanguage)this.GetValue(LanguageProperty);
            set => this.SetValue(LanguageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FollowGlobalTheme"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FollowGlobalThemeProperty = DependencyProperty.Register(
            nameof(FollowGlobalTheme),
            typeof(bool),
            typeof(SyntaxEditor),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the editor automatically tracks
        /// <see cref="ThemeManager.ThemeChanged"/> and updates its <see cref="Theme"/> accordingly.
        /// Defaults to <c>true</c>. Setting <see cref="Theme"/> directly always overrides the current value.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the editor automatically tracks the global ThemeManager theme.")]
        public bool FollowGlobalTheme
        {
            get => (bool)this.GetValue(FollowGlobalThemeProperty);
            set => this.SetValue(FollowGlobalThemeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClearVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearVisibleProperty = DependencyProperty.Register(
            nameof(ClearVisible),
            typeof(bool),
            typeof(SyntaxEditor),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether a "Clear All" item is included in the context menu.
        /// When <c>true</c>, the item sets the editor's text to an empty string.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether a Clear All item appears in the context menu.")]
        public bool ClearVisible
        {
            get => (bool)this.GetValue(ClearVisibleProperty);
            set => this.SetValue(ClearVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StatusBarVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StatusBarVisibleProperty = DependencyProperty.Register(
            nameof(StatusBarVisible),
            typeof(bool),
            typeof(SyntaxEditor),
            new PropertyMetadata(true, OnStatusBarVisibleChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the status bar that displays the current line,
        /// column, and character code is visible. Defaults to <c>true</c>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the caret status bar is visible.")]
        public bool StatusBarVisible
        {
            get => (bool)this.GetValue(StatusBarVisibleProperty);
            set => this.SetValue(StatusBarVisibleProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxEditor"/> class.
        /// </summary>
        public SyntaxEditor()
        {
            this.FontFamily = new FontFamily("Consolas");
            this.FontSize = 12;
            this.ShowLineNumbers = true;
            this.Options.HighlightCurrentLine = true;

            this.CommandBindings.Add(new CommandBinding(FormatJsonCommand, (_, _) => this.FormatJson()));
            this.CommandBindings.Add(new CommandBinding(MinifyJsonCommand, (_, _) => this.MinifyJson()));
            this.CommandBindings.Add(new CommandBinding(ValidateJsonCommand, (_, _) => this.ValidateJson()));
            this.CommandBindings.Add(new CommandBinding(CommentSelectionCommand, (_, _) => this.CommentSelectedLines(), this.OnCommentCommandCanExecute));
            this.CommandBindings.Add(new CommandBinding(UncommentSelectionCommand, (_, _) => this.UncommentSelectedLines(), this.OnCommentCommandCanExecute));
            this.CommandBindings.Add(new CommandBinding(MoveSelectionUpCommand, (_, _) => this.MoveSelectedLinesUp(), this.OnLineMoveCommandCanExecute));
            this.CommandBindings.Add(new CommandBinding(MoveSelectionDownCommand, (_, _) => this.MoveSelectedLinesDown(), this.OnLineMoveCommandCanExecute));
            this.CommandBindings.Add(new CommandBinding(GoToLineCommand, this.OnGoToLineCommandExecuted, this.OnGoToLineCommandCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, this.OnFindCommandExecuted, this.OnFindCommandCanExecute));
            this.TextArea.InputBindings.Add(new KeyBinding(CommentSelectionCommand, new KeyChordGesture(ModifierKeys.Control, Key.K, Key.C)) { CommandTarget = this });
            this.TextArea.InputBindings.Add(new KeyBinding(UncommentSelectionCommand, new KeyChordGesture(ModifierKeys.Control, Key.K, Key.U)) { CommandTarget = this });
            this.TextArea.InputBindings.Add(new KeyBinding(MoveSelectionUpCommand, new KeyGesture(Key.Up, ModifierKeys.Control)) { CommandTarget = this });
            this.TextArea.InputBindings.Add(new KeyBinding(MoveSelectionDownCommand, new KeyGesture(Key.Down, ModifierKeys.Control)) { CommandTarget = this });
            this.TextArea.InputBindings.Add(new KeyBinding(GoToLineCommand, new KeyGesture(Key.G, ModifierKeys.Control)) { CommandTarget = this });
            this.TextArea.InputBindings.Add(new KeyBinding(ApplicationCommands.Find, new KeyGesture(Key.F, ModifierKeys.Control)) { CommandTarget = this });

            this.ContextMenu = new ContextMenu();
            this.ContextMenuOpening += this.OnContextMenuOpening;
            this.PreviewKeyDown += this.OnPreviewKeyDown;
            this.TextChanged += this.OnTextChanged;
            this.TextArea.Caret.PositionChanged += this.OnCaretPositionChanged;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;

            this.EnsureSearchPanelResources();
            this.EnsureSearchPanelInstalled();
            this.ApplyTheme();
            this.ReloadHighlighting();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.InstallStatusBar();
            this.UpdateStatusBarVisibility();
            this.UpdateStatusBar();
        }

        /// <summary>
        /// Wraps AvalonEdit's default scroll viewer with a status bar without replacing its template.
        /// </summary>
        private void InstallStatusBar()
        {
            if (_statusBar?.Parent != null)
            {
                return;
            }

            if (this.GetTemplateChild("PART_ScrollViewer") is not ScrollViewer scrollViewer)
            {
                return;
            }

            var statusBar = this.CreateStatusBar();
            var dockPanel = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(statusBar, Dock.Bottom);
            dockPanel.Children.Add(statusBar);

            switch (scrollViewer.Parent)
            {
                case Decorator decorator:
                    if (decorator.Child != scrollViewer)
                    {
                        return;
                    }

                    decorator.Child = null;
                    dockPanel.Children.Add(scrollViewer);
                    decorator.Child = dockPanel;
                    break;

                case Panel panel:
                    int index = panel.Children.IndexOf(scrollViewer);
                    if (index < 0)
                    {
                        return;
                    }

                    panel.Children.RemoveAt(index);
                    dockPanel.Children.Add(scrollViewer);
                    panel.Children.Insert(index, dockPanel);
                    break;

                default:
                    return;
            }

            _statusBar = statusBar;
        }

        /// <summary>
        /// Creates the status bar shown under the editor surface.
        /// </summary>
        /// <returns>A configured status bar.</returns>
        private StatusBar CreateStatusBar()
        {
            var statusBar = new StatusBar
            {
                Name = PartStatusBar
            };
            statusBar.SetResourceReference(BackgroundProperty, MosaicTheme.ControlBackgroundBrush);
            statusBar.SetResourceReference(ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);

            _lineTextBlock = CreateStatusTextBlock(PartLineTextBlock, "Ln 1");
            _columnTextBlock = CreateStatusTextBlock(PartColumnTextBlock, "Col 1");
            _characterTextBlock = CreateStatusTextBlock(PartCharacterTextBlock, "Ch 32");

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            panel.Children.Add(_lineTextBlock);
            panel.Children.Add(_columnTextBlock);
            panel.Children.Add(_characterTextBlock);

            statusBar.Items.Add(new StatusBarItem
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = panel
            });

            return statusBar;
        }

        /// <summary>
        /// Creates a status bar text block with the shared sizing and spacing used by caret indicators.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <param name="text">The initial text.</param>
        /// <returns>A configured text block.</returns>
        private static TextBlock CreateStatusTextBlock(string name, string text)
        {
            return new TextBlock
            {
                Name = name,
                MinWidth = 40,
                Margin = new Thickness(7, 0, 7, 0),
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Ensures that the shared AvalonEdit search panel resources are available to this editor.
        /// </summary>
        private void EnsureSearchPanelResources()
        {
            foreach (var dictionary in this.Resources.MergedDictionaries)
            {
                if (dictionary.Source == SearchPanelResourceUri)
                {
                    return;
                }
            }

            this.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = SearchPanelResourceUri });
        }

        /// <summary>
        /// Ensures that a search panel instance is attached to the editor.
        /// </summary>
        private void EnsureSearchPanelInstalled()
        {
            this.EnsureSearchPanelResources();

            if (_searchPanel == null)
            {
                _searchPanel = SearchPanel.Install(this);
            }

            this.EnsureSearchPanelResources(_searchPanel.Resources);
            this.ApplySearchPanelStyle();
        }

        /// <summary>
        /// Ensures that the shared AvalonEdit search panel resources are available in the specified dictionary.
        /// </summary>
        /// <param name="resources">The resource dictionary to update.</param>
        private void EnsureSearchPanelResources(ResourceDictionary resources)
        {
            foreach (var dictionary in resources.MergedDictionaries)
            {
                if (dictionary.Source == SearchPanelResourceUri)
                {
                    return;
                }
            }

            resources.MergedDictionaries.Add(new ResourceDictionary { Source = SearchPanelResourceUri });
        }

        /// <summary>
        /// Applies the current AvalonEdit search panel style if one is available.
        /// </summary>
        private void ApplySearchPanelStyle()
        {
            if (_searchPanel == null)
            {
                return;
            }

            if (_searchPanel.TryFindResource(typeof(SearchPanel)) is Style style)
            {
                _searchPanel.Style = style;
            }
        }

        /// <summary>
        /// Opens the search panel and restores its visual state.
        /// </summary>
        private void OpenSearchPanel()
        {
            this.EnsureSearchPanelInstalled();

            if (_searchPanel == null)
            {
                return;
            }

            this.ApplySearchPanelStyle();

            if (_searchPanel.IsClosed)
            {
                try
                {
                    _searchPanel.Open();
                }
                catch (ArgumentException)
                {
                    this.ReplaceSearchPanel();
                    _searchPanel?.Open();
                }
            }

            if (_searchPanel == null)
            {
                return;
            }

            _searchPanel.ApplyTemplate();
            _searchPanel.Reactivate();
        }

        /// <summary>
        /// Recreates the search panel so it can pick up the current theme resources.
        /// </summary>
        private void ReplaceSearchPanel()
        {
            string? searchPattern = _searchPanel?.SearchPattern;
            bool matchCase = _searchPanel?.MatchCase ?? false;
            bool wholeWords = _searchPanel?.WholeWords ?? false;
            bool useRegex = _searchPanel?.UseRegex ?? false;

            if (_searchPanel != null)
            {
                _searchPanel.Uninstall();
                _searchPanel = null;
            }

            this.EnsureSearchPanelInstalled();

            if (_searchPanel == null)
            {
                return;
            }

            _searchPanel.SearchPattern = searchPattern ?? string.Empty;
            _searchPanel.MatchCase = matchCase;
            _searchPanel.WholeWords = wholeWords;
            _searchPanel.UseRegex = useRegex;
        }

        /// <summary>
        /// Rebuilds the search panel after a theme change and reopens it when necessary.
        /// </summary>
        private void ResetSearchPanelForThemeChange()
        {
            bool reopen = _searchPanel is { IsClosed: false };

            this.ReplaceSearchPanel();

            if (reopen)
            {
                this.Dispatcher.BeginInvoke(new Action(this.OpenSearchPanel), DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Opens the search panel in response to the find command.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command execution.</param>
        private void OnFindCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.OpenSearchPanel();
            e.Handled = true;
        }

        /// <summary>
        /// Reports whether the find command can execute.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command query.</param>
        private void OnFindCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.IsEnabled;
            e.Handled = true;
        }

        /// <summary>
        /// Opens the go-to-line prompt and moves the caret to the confirmed line.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command execution.</param>
        private async void OnGoToLineCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Document == null)
            {
                return;
            }

            e.Handled = true;

            int? lineNumber = await this.PromptForLineNumberAsync();
            if (lineNumber.HasValue)
            {
                this.GoToLine(lineNumber.Value);
            }
        }

        /// <summary>
        /// Reports whether the go-to-line command can execute.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command query.</param>
        private void OnGoToLineCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.IsEnabled && this.Document != null && this.Document.LineCount > 0;
            e.Handled = true;
        }

        /// <summary>
        /// Prompts the user for a valid document line number.
        /// </summary>
        /// <returns>The confirmed line number; otherwise, <see langword="null"/> when cancelled.</returns>
        private async Task<int?> PromptForLineNumberAsync()
        {
            if (this.Document == null)
            {
                return null;
            }

            int lineCount = Math.Max(1, this.Document.LineCount);
            var input = new TextBox
            {
                MinWidth = 220,
                Margin = new Thickness(0, 0, 0, 12),
                Text = Math.Clamp(this.TextArea.Caret.Line, 1, lineCount).ToString()
            };

            var okButton = new Button
            {
                Content = "OK",
                IsDefault = true,
                MinWidth = 80,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                MinWidth = 80,
                Height = 25

            };

            var buttons = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal
            };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var content = new StackPanel
            {
                Margin = new Thickness(4)
            };

            content.Children.Add(input);
            content.Children.Add(buttons);

            var dialog = new ModalDialog
            {
                Title = "Go To Line",
                Description = $"Enter a line number between 1 and {lineCount}:",
                Content = content,
                BlurRadius = 0,
                CloseOnBackdropClick = false,
                CloseOnEscape = true
            };

            int? result = null;
            okButton.Click += (_, _) =>
            {
                if (!int.TryParse(input.Text, out int lineNumber) || lineNumber < 1 || lineNumber > lineCount)
                {
                    MessageBox.Show($"Enter a line number from 1 to {lineCount}.", "Go To Line", MessageBoxButton.OK, MessageBoxImage.Information);
                    input.Focus();
                    input.SelectAll();
                    return;
                }

                result = lineNumber;
                dialog.Close(true);
            };

            cancelButton.Click += (_, _) => dialog.Close(null);

            dialog.Opened += (_, _) =>
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
                {
                    input.Focus();
                    input.SelectAll();
                });
            };

            return await dialog.ShowAsync(this.GetDialogHost()) == true ? result : null;
        }

        /// <summary>
        /// Resolves the host element used by <see cref="ModalDialog"/> for its adorner layer.
        /// </summary>
        /// <returns>The root window content when available; otherwise this editor.</returns>
        private UIElement GetDialogHost()
        {
            return Window.GetWindow(this)?.Content as UIElement ?? this;
        }

        /// <summary>
        /// Moves the caret to the start of the requested line and scrolls it into view.
        /// </summary>
        /// <param name="lineNumber">The one-based line number to focus.</param>
        private void GoToLine(int lineNumber)
        {
            if (this.Document == null || lineNumber < 1 || lineNumber > this.Document.LineCount)
            {
                return;
            }

            DocumentLine line = this.Document.GetLineByNumber(lineNumber);
            this.CaretOffset = line.Offset;
            this.SelectionLength = 0;
            this.ScrollToLine(lineNumber);
            this.Focus();
            this.TextArea.Focus();
            this.TextArea.Caret.BringCaretToView();
            this.UpdateStatusBar();
        }

        /// <summary>
        /// Intercepts Ctrl+F so the search panel can open before the default handler runs.
        /// </summary>
        /// <param name="sender">The source of the keyboard event.</param>
        /// <param name="e">The event data for the key press.</param>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                this.OpenSearchPanel();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Responds to a change in the <see cref="Theme"/> dependency property.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SyntaxEditor editor)
            {
                editor.ApplyTheme();
                editor.ResetSearchPanelForThemeChange();
                editor.ReloadHighlighting();
            }
        }

        /// <summary>
        /// Reloads syntax highlighting after the <see cref="Language"/> dependency property changes.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SyntaxEditor)?.ReloadHighlighting();
        }

        /// <summary>
        /// Applies the status bar visibility when <see cref="StatusBarVisible"/> changes.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnStatusBarVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SyntaxEditor)?.UpdateStatusBarVisibility();
        }

        /// <summary>
        /// Updates the status bar when the caret moves.
        /// </summary>
        /// <param name="sender">The source of the caret event.</param>
        /// <param name="e">The event data for the caret event.</param>
        private void OnCaretPositionChanged(object? sender, EventArgs e)
        {
            this.UpdateStatusBar();
        }

        /// <summary>
        /// Updates the status bar when text changes without moving the caret.
        /// </summary>
        /// <param name="sender">The source of the text event.</param>
        /// <param name="e">The event data for the text event.</param>
        private void OnTextChanged(object? sender, EventArgs e)
        {
            this.UpdateStatusBar();
        }

        /// <summary>
        /// Applies the current <see cref="StatusBarVisible"/> value to the status bar template part.
        /// </summary>
        private void UpdateStatusBarVisibility()
        {
            if (_statusBar != null)
            {
                _statusBar.Visibility = this.StatusBarVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Recomputes the caret line, column, and character displayed in the status bar.
        /// </summary>
        private void UpdateStatusBar()
        {
            if (_lineTextBlock == null || _columnTextBlock == null || _characterTextBlock == null || this.Document == null)
            {
                return;
            }

            var caret = this.TextArea.Caret;
            _lineTextBlock.Text = $"Ln {caret.Line}";
            _columnTextBlock.Text = $"Col {caret.Column}";

            int offset = Math.Clamp(this.CaretOffset, 0, this.Document.TextLength);
            char ch = offset < this.Document.TextLength ? this.Document.GetCharAt(offset) : ' ';
            _characterTextBlock.Text = $"Ch {(int)ch}";
        }

        #region Global theme tracking

        /// <summary>
        /// Subscribes to global theme updates when the editor is loaded.
        /// </summary>
        /// <param name="sender">The source of the loaded event.</param>
        /// <param name="e">The event data for the load operation.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.FollowGlobalTheme && !_subscribedToGlobalTheme)
            {
                ThemeManager.ThemeChanged += this.OnGlobalThemeChanged;
                _subscribedToGlobalTheme = true;

                // Adopt the current global theme (if one can be located) on first load.
                var current = TryGetGlobalTheme();
                if (current.HasValue)
                {
                    this.Theme = current.Value;
                }
            }
        }

        /// <summary>
        /// Unsubscribes from global theme updates when the editor is unloaded.
        /// </summary>
        /// <param name="sender">The source of the unloaded event.</param>
        /// <param name="e">The event data for the unload operation.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedToGlobalTheme)
            {
                ThemeManager.ThemeChanged -= this.OnGlobalThemeChanged;
                _subscribedToGlobalTheme = false;
            }
        }

        /// <summary>
        /// Applies the notified global theme when theme tracking is enabled.
        /// </summary>
        /// <param name="sender">The source of the theme notification.</param>
        /// <param name="theme">The theme that was selected globally.</param>
        private void OnGlobalThemeChanged(object? sender, MosaicThemeMode theme)
        {
            if (this.FollowGlobalTheme)
            {
                this.Theme = theme;
            }
        }

        /// <summary>
        /// Attempts to locate a <see cref="ThemeManager"/> in the application's merged resource
        /// dictionaries so the editor can adopt the current theme without explicit wiring.
        /// </summary>
        private static MosaicThemeMode? TryGetGlobalTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null)
            {
                return null;
            }

            foreach (var dictionary in app.Resources.MergedDictionaries)
            {
                if (dictionary is ThemeManager manager)
                {
                    return manager.Theme;
                }
            }

            return null;
        }

        #endregion

        #region Theming

        /// <summary>
        /// Resolves the Mosaic theme color tokens for the current <see cref="Theme"/> and applies them to
        /// the editor's background, foreground, line numbers, selection, current line, and border.
        /// </summary>
        private void ApplyTheme()
        {
            var dictionary = GetThemeDictionary(this.Theme);
            if (dictionary == null)
            {
                return;
            }

            var background = ResolveColor(dictionary, MosaicTheme.ControlTextBackgroundColor, MosaicTheme.ControlBackgroundColor, Colors.White);
            var foreground = ResolveColor(dictionary, MosaicTheme.ControlTextForegroundColor, MosaicTheme.ControlForegroundColor, Colors.Black);
            var lineNumbers = ResolveColor(dictionary, MosaicTheme.PlaceholderTextColor, null, Colors.Gray);
            var selectionBg = ResolveColor(dictionary, MosaicTheme.SelectionBackgroundColor, MosaicTheme.AccentColor, Colors.RoyalBlue);
            var selectionFg = ResolveColor(dictionary, MosaicTheme.SelectionForegroundColor, null, Colors.White);
            var border = ResolveColor(dictionary, MosaicTheme.ControlBorderColor, null, Colors.Gray);
            var hover = ResolveColor(dictionary, MosaicTheme.ControlHoverBackgroundColor, null, lineNumbers);

            this.Background = new SolidColorBrush(background);
            this.Foreground = new SolidColorBrush(foreground);
            this.LineNumbersForeground = new SolidColorBrush(lineNumbers);

            // A translucent selection keeps the underlying text legible while honoring the theme accent.
            this.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x99, selectionBg.R, selectionBg.G, selectionBg.B));
            this.TextArea.SelectionForeground = new SolidColorBrush(selectionFg);
            this.TextArea.SelectionBorder = new Pen(new SolidColorBrush(selectionBg), 1.0);

            this.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromArgb(0x28, hover.R, hover.G, hover.B));
            this.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.FromArgb(0x40, hover.R, hover.G, hover.B)), 1.0);

            this.BorderBrush = new SolidColorBrush(border);
            this.BorderThickness = new Thickness(1);
        }

        /// <summary>
        /// Loads and caches the resource dictionary for the specified theme.
        /// </summary>
        /// <param name="theme">One of the enumeration values that specifies the theme to resolve.</param>
        /// <returns>A theme resource dictionary; otherwise, <see langword="null" /> when the dictionary cannot be loaded.</returns>
        private static ResourceDictionary? GetThemeDictionary(MosaicThemeMode theme)
        {
            if (ThemeDictionaryCache.TryGetValue(theme, out var cached))
            {
                return cached;
            }

            try
            {
                var dictionary = new ResourceDictionary { Source = ThemeDictionaryUris.GetThemeUri(theme) };
                ThemeDictionaryCache[theme] = dictionary;
                return dictionary;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves a theme color from the specified dictionary with an optional fallback key.
        /// </summary>
        /// <param name="dictionary">The resource dictionary to inspect.</param>
        /// <param name="primaryKey">The primary resource key to look up.</param>
        /// <param name="fallbackKey">The fallback resource key to look up when the primary key is missing.</param>
        /// <param name="defaultColor">The color to return when neither key is available.</param>
        /// <returns>The resolved color value.</returns>
        private static Color ResolveColor(ResourceDictionary dictionary, ComponentResourceKey primaryKey, ComponentResourceKey? fallbackKey, Color defaultColor)
        {
            if (dictionary.Contains(primaryKey) && dictionary[primaryKey] is Color primary)
            {
                return primary;
            }

            if (fallbackKey != null && dictionary.Contains(fallbackKey) && dictionary[fallbackKey] is Color fallback)
            {
                return fallback;
            }

            return defaultColor;
        }

        #endregion

        #region Syntax highlighting

        /// <summary>
        /// Loads (or clears) the syntax highlighting definition for the current <see cref="Language"/> and
        /// <see cref="Theme"/>.
        /// </summary>
        private void ReloadHighlighting()
        {
            string? baseName = SyntaxLanguageMap.GetResourceBaseName(this.Language);
            if (baseName == null)
            {
                this.SyntaxHighlighting = null;
                return;
            }

            // Blue and HighContrast fall back to the Dark highlighting variant.
            string themeSuffix = this.Theme == MosaicThemeMode.Light ? "Light" : "Dark";
            string cacheKey = $"{baseName}|{themeSuffix}";

            try
            {
                if (!HighlightingCache.TryGetValue(cacheKey, out var definition))
                {
                    string resourceName = string.Format(XshdResourceFormat, baseName, themeSuffix);
                    using var stream = typeof(SyntaxEditor).Assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        this.SyntaxHighlighting = null;
                        return;
                    }

                    using var reader = XmlReader.Create(stream);
                    definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingCache[cacheKey] = definition;
                }

                this.SyntaxHighlighting = definition;
            }
            catch
            {
                this.SyntaxHighlighting = null;
            }
        }

        /// <summary>
        /// Sets <see cref="Language"/> based on a file path or extension (e.g. <c>".cs"</c> or
        /// <c>"data.json"</c>). Unrecognized extensions set <see cref="SyntaxLanguage.None"/>.
        /// </summary>
        /// <param name="pathOrExtension">The file path or extension to inspect.</param>
        public void SetSyntaxFromFileExtension(string? pathOrExtension)
        {
            this.Language = SyntaxLanguageMap.FromExtension(pathOrExtension);
        }

        #endregion

        #region Context menu

        /// <summary>
        /// Rebuilds the context menu immediately before it is displayed.
        /// </summary>
        /// <param name="sender">The source of the context menu event.</param>
        /// <param name="e">The event data for the menu opening.</param>
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var menu = this.ContextMenu;
            if (menu == null)
            {
                return;
            }

            menu.Items.Clear();

            // Undo/Redo/Cut/Paste route through ApplicationCommands so they follow AvalonEdit's
            // command state. Copy/Select All are wired to direct handlers so they remain available
            // regardless of read-only state.
            menu.Items.Add(CreateItem("Undo", ApplicationCommands.Undo, "Ctrl+Z"));
            menu.Items.Add(CreateItem("Redo", ApplicationCommands.Redo, "Ctrl+Y"));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateItem("Cut", ApplicationCommands.Cut, "Ctrl+X"));
            menu.Items.Add(CreateItem("Copy", (_, _) => this.Copy(), "Ctrl+C"));
            menu.Items.Add(CreateItem("Paste", ApplicationCommands.Paste, "Ctrl+V"));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateItem("Select All", (_, _) => this.SelectAll(), "Ctrl+A"));

            if (this.ClearVisible)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateItem("Clear All", (_, _) => { this.Document.Text = string.Empty; }));
            }

            AddLanguageItems(menu);

            // Allow consumers to customize the populated menu before it is displayed.
            this.RaiseEvent(new SyntaxEditorContextMenuEventArgs(ContextMenuRequestedEvent, this, menu, this.Language));
        }

        /// <summary>
        /// Adds language-specific commands to the context menu.
        /// </summary>
        /// <param name="menu">The context menu to extend.</param>
        private void AddLanguageItems(ContextMenu menu)
        {
            if (SyntaxLanguageMap.GetLineCommentDefinition(this.Language) != null)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateItem("Comment Selection", CommentSelectionCommand, "Ctrl+K Ctrl+C"));
                menu.Items.Add(CreateItem("Uncomment Selection", UncommentSelectionCommand, "Ctrl+K Ctrl+U"));
            }

            if (this.Language == SyntaxLanguage.Json)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateItem("Format JSON", FormatJsonCommand));
                menu.Items.Add(CreateItem("Minify JSON", MinifyJsonCommand));
                menu.Items.Add(CreateItem("Validate JSON", ValidateJsonCommand));
            }
        }

        /// <summary>
        /// Creates a command-backed context menu item.
        /// </summary>
        /// <param name="header">The item header text.</param>
        /// <param name="command">The command to invoke.</param>
        /// <param name="inputGestureText">The shortcut text to display.</param>
        /// <returns>A configured menu item.</returns>
        private MenuItem CreateItem(string header, ICommand command, string? inputGestureText = null)
        {
            return new MenuItem
            {
                Header = header,
                Command = command,
                CommandTarget = this,
                InputGestureText = inputGestureText
            };
        }

        /// <summary>
        /// Creates a click-handled context menu item.
        /// </summary>
        /// <param name="header">The item header text.</param>
        /// <param name="onClick">The click handler to attach.</param>
        /// <param name="inputGestureText">The shortcut text to display.</param>
        /// <returns>A configured menu item.</returns>
        private static MenuItem CreateItem(string header, RoutedEventHandler onClick, string? inputGestureText = null)
        {
            var item = new MenuItem
            {
                Header = header,
                InputGestureText = inputGestureText
            };
            item.Click += onClick;
            return item;
        }

        #endregion

        #region Comment operations

        /// <summary>
        /// Reports whether line-comment commands can execute.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command query.</param>
        private void OnCommentCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !this.IsReadOnly && this.Document != null && SyntaxLanguageMap.GetLineCommentDefinition(this.Language) != null;
            e.Handled = true;
        }

        /// <summary>
        /// Comments the selected lines or the current line when no selection exists.
        /// </summary>
        private void CommentSelectedLines()
        {
            var comment = SyntaxLanguageMap.GetLineCommentDefinition(this.Language);
            if (comment == null || this.Document == null || this.IsReadOnly)
            {
                return;
            }

            this.TransformSelectedLines(lineText => CommentLine(lineText, comment));
        }

        /// <summary>
        /// Removes line comments from the selected lines or the current line when no selection exists.
        /// </summary>
        private void UncommentSelectedLines()
        {
            var comment = SyntaxLanguageMap.GetLineCommentDefinition(this.Language);
            if (comment == null || this.Document == null || this.IsReadOnly)
            {
                return;
            }

            this.TransformSelectedLines(lineText => UncommentLine(lineText, comment));
        }

        /// <summary>
        /// Applies a line transformation across the current line selection.
        /// </summary>
        /// <param name="transform">The transformation to apply to each selected line.</param>
        private void TransformSelectedLines(Func<string, string> transform)
        {
            var range = this.GetSelectedLineRange();
            if (range == null)
            {
                return;
            }

            int firstLineNumber = range.Value.FirstLineNumber;
            int lastLineNumber = range.Value.LastLineNumber;

            this.Document.BeginUpdate();
            try
            {
                for (int lineNumber = lastLineNumber; lineNumber >= firstLineNumber; lineNumber--)
                {
                    DocumentLine line = this.Document.GetLineByNumber(lineNumber);
                    string lineText = this.Document.GetText(line.Offset, line.Length);
                    string transformed = transform(lineText);

                    if (!string.Equals(lineText, transformed, StringComparison.Ordinal))
                    {
                        this.Document.Replace(line.Offset, line.Length, transformed);
                    }
                }
            }
            finally
            {
                this.Document.EndUpdate();
            }

            DocumentLine firstLine = this.Document.GetLineByNumber(firstLineNumber);
            DocumentLine lastLine = this.Document.GetLineByNumber(Math.Min(lastLineNumber, this.Document.LineCount));

            if (this.SelectionLength > 0)
            {
                this.SelectionStart = firstLine.Offset;
                this.SelectionLength = lastLine.EndOffset - firstLine.Offset;
            }
            else
            {
                this.CaretOffset = Math.Min(this.CaretOffset, this.Document.TextLength);
            }
        }

        /// <summary>
        /// Resolves the first and last line numbers covered by the current selection.
        /// </summary>
        /// <returns>A line-number range; otherwise, <see langword="null" /> when the document is empty.</returns>
        private (int FirstLineNumber, int LastLineNumber)? GetSelectedLineRange()
        {
            if (this.Document == null || this.Document.TextLength == 0)
            {
                return null;
            }

            int startOffset = Math.Clamp(this.SelectionStart, 0, this.Document.TextLength);

            if (this.SelectionLength <= 0)
            {
                var line = this.Document.GetLineByOffset(Math.Clamp(this.CaretOffset, 0, this.Document.TextLength));
                return (line.LineNumber, line.LineNumber);
            }

            int endOffset = Math.Clamp(this.SelectionStart + this.SelectionLength, 0, this.Document.TextLength);
            DocumentLine startLine = this.Document.GetLineByOffset(startOffset);
            DocumentLine endLine = this.GetSelectionEndLine(startOffset, endOffset);

            return (startLine.LineNumber, endLine.LineNumber);
        }

        /// <summary>
        /// Resolves the line that should be treated as the end of the current selection.
        /// </summary>
        /// <param name="startOffset">The starting offset of the selection.</param>
        /// <param name="endOffset">The ending offset of the selection.</param>
        /// <returns>The document line that closes the selection.</returns>
        private DocumentLine GetSelectionEndLine(int startOffset, int endOffset)
        {
            if (endOffset <= startOffset)
            {
                return this.Document.GetLineByOffset(startOffset);
            }

            if (endOffset < this.Document.TextLength)
            {
                DocumentLine lineAtEndOffset = this.Document.GetLineByOffset(endOffset);
                if (lineAtEndOffset.LineNumber > 1 && endOffset == lineAtEndOffset.Offset)
                {
                    return this.Document.GetLineByNumber(lineAtEndOffset.LineNumber - 1);
                }
            }

            return this.Document.GetLineByOffset(Math.Max(startOffset, endOffset - 1));
        }

        /// <summary>
        /// Adds a line comment to a single line of text.
        /// </summary>
        /// <param name="lineText">The line text to transform.</param>
        /// <param name="comment">The comment definition to apply.</param>
        /// <returns>The commented line text.</returns>
        private static string CommentLine(string lineText, SyntaxCommentDefinition comment)
        {
            int indentLength = GetIndentLength(lineText);
            string indentation = lineText[..indentLength];
            string content = lineText[indentLength..];

            if (comment.LineSuffix == null)
            {
                return $"{indentation}{comment.LinePrefix} {content}";
            }

            return content.Length == 0
                ? $"{indentation}{comment.LinePrefix} {comment.LineSuffix}"
                : $"{indentation}{comment.LinePrefix} {content} {comment.LineSuffix}";
        }

        /// <summary>
        /// Removes a line comment from a single line of text when one is present.
        /// </summary>
        /// <param name="lineText">The line text to transform.</param>
        /// <param name="comment">The comment definition to apply.</param>
        /// <returns>The uncommented line text.</returns>
        private static string UncommentLine(string lineText, SyntaxCommentDefinition comment)
        {
            int indentLength = GetIndentLength(lineText);
            string indentation = lineText[..indentLength];
            string content = lineText[indentLength..];

            if (!StartsWithCommentPrefix(content, comment.LinePrefix))
            {
                return lineText;
            }

            string uncommented = content[comment.LinePrefix.Length..];
            if (uncommented.StartsWith(' '))
            {
                uncommented = uncommented[1..];
            }

            if (comment.LineSuffix != null && uncommented.EndsWith(comment.LineSuffix, StringComparison.Ordinal))
            {
                uncommented = uncommented[..^comment.LineSuffix.Length];
                if (uncommented.EndsWith(' '))
                {
                    uncommented = uncommented[..^1];
                }
            }

            return indentation + uncommented;
        }

        /// <summary>
        /// Determines whether the specified content begins with a standalone comment prefix.
        /// </summary>
        /// <param name="content">The content to inspect.</param>
        /// <param name="prefix">The comment prefix to match.</param>
        /// <returns><see langword="true" /> if the content starts with the prefix; otherwise, <see langword="false" />.</returns>
        private static bool StartsWithCommentPrefix(string content, string prefix)
        {
            if (!content.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return !char.IsLetter(prefix[^1])
                   || content.Length == prefix.Length
                   || char.IsWhiteSpace(content[prefix.Length]);
        }

        /// <summary>
        /// Measures the length of the leading whitespace on a line.
        /// </summary>
        /// <param name="lineText">The line text to inspect.</param>
        /// <returns>The number of leading whitespace characters.</returns>
        private static int GetIndentLength(string lineText)
        {
            int indentLength = 0;
            while (indentLength < lineText.Length && char.IsWhiteSpace(lineText[indentLength]))
            {
                indentLength++;
            }

            return indentLength;
        }

        #endregion

        #region Line movement operations

        /// <summary>
        /// Reports whether line-move commands can execute.
        /// </summary>
        /// <param name="sender">The source of the routed command.</param>
        /// <param name="e">The event data for the command query.</param>
        private void OnLineMoveCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !this.IsReadOnly && this.Document != null && this.Document.TextLength > 0;
            e.Handled = true;
        }

        /// <summary>
        /// Moves the selected lines one position earlier in the document.
        /// </summary>
        private void MoveSelectedLinesUp()
        {
            var range = this.GetSelectedLineRange();
            if (range == null || this.Document == null || this.IsReadOnly)
            {
                return;
            }

            int firstLineNumber = range.Value.FirstLineNumber;
            int lastLineNumber = range.Value.LastLineNumber;

            if (firstLineNumber <= 1)
            {
                this.SelectWholeLines(firstLineNumber, lastLineNumber);
                return;
            }

            DocumentLine previousLine = this.Document.GetLineByNumber(firstLineNumber - 1);
            DocumentLine firstLine = this.Document.GetLineByNumber(firstLineNumber);
            DocumentLine lastLine = this.Document.GetLineByNumber(lastLineNumber);

            int selectedOffset = firstLine.Offset;
            int selectedLength = this.GetWholeLineSelectionLength(firstLine, lastLine);
            string selectedBlock = this.Document.GetText(selectedOffset, selectedLength);
            int selectionStart;
            int selectionLength;

            if (lastLine.LineNumber == this.Document.LineCount)
            {
                string previousContent = this.Document.GetText(previousLine.Offset, previousLine.Length);
                string previousDelimiter = this.Document.GetText(previousLine.EndOffset, previousLine.DelimiterLength);
                this.Document.Replace(previousLine.Offset, previousLine.TotalLength + selectedLength, selectedBlock + previousDelimiter + previousContent);
                selectionStart = previousLine.Offset;
                selectionLength = selectedBlock.Length + previousDelimiter.Length;
            }
            else
            {
                string previousBlock = this.Document.GetText(previousLine.Offset, previousLine.TotalLength);
                this.Document.Replace(previousLine.Offset, previousBlock.Length + selectedBlock.Length, selectedBlock + previousBlock);
                selectionStart = previousLine.Offset;
                selectionLength = selectedBlock.Length;
            }

            this.SelectionStart = selectionStart;
            this.SelectionLength = selectionLength;
        }

        /// <summary>
        /// Moves the selected lines one position later in the document.
        /// </summary>
        private void MoveSelectedLinesDown()
        {
            var range = this.GetSelectedLineRange();
            if (range == null || this.Document == null || this.IsReadOnly)
            {
                return;
            }

            int firstLineNumber = range.Value.FirstLineNumber;
            int lastLineNumber = range.Value.LastLineNumber;

            if (lastLineNumber >= this.Document.LineCount)
            {
                this.SelectWholeLines(firstLineNumber, lastLineNumber);
                return;
            }

            DocumentLine firstLine = this.Document.GetLineByNumber(firstLineNumber);
            DocumentLine lastLine = this.Document.GetLineByNumber(lastLineNumber);
            DocumentLine nextLine = this.Document.GetLineByNumber(lastLineNumber + 1);

            int selectedOffset = firstLine.Offset;
            int selectedLength = nextLine.Offset - selectedOffset;
            string selectedBlock = this.Document.GetText(selectedOffset, selectedLength);
            int selectionStart;
            int selectionLength;

            if (nextLine.LineNumber == this.Document.LineCount)
            {
                string selectedContent = this.Document.GetText(selectedOffset, lastLine.EndOffset - selectedOffset);
                string selectedDelimiter = this.Document.GetText(lastLine.EndOffset, lastLine.DelimiterLength);
                string nextContent = this.Document.GetText(nextLine.Offset, nextLine.Length);
                this.Document.Replace(selectedOffset, selectedLength + nextLine.Length, nextContent + selectedDelimiter + selectedContent);
                selectionStart = selectedOffset + nextContent.Length + selectedDelimiter.Length;
                selectionLength = selectedContent.Length;
            }
            else
            {
                string nextBlock = this.Document.GetText(nextLine.Offset, nextLine.TotalLength);
                this.Document.Replace(selectedOffset, selectedBlock.Length + nextBlock.Length, nextBlock + selectedBlock);
                selectionStart = selectedOffset + nextBlock.Length;
                selectionLength = selectedBlock.Length;
            }

            this.SelectionStart = selectionStart;
            this.SelectionLength = selectionLength;
        }

        /// <summary>
        /// Reselects the specified inclusive line range using whole-line boundaries.
        /// </summary>
        /// <param name="firstLineNumber">The first line number in the range.</param>
        /// <param name="lastLineNumber">The last line number in the range.</param>
        private void SelectWholeLines(int firstLineNumber, int lastLineNumber)
        {
            DocumentLine firstLine = this.Document.GetLineByNumber(firstLineNumber);
            DocumentLine lastLine = this.Document.GetLineByNumber(lastLineNumber);

            this.SelectionStart = firstLine.Offset;
            this.SelectionLength = this.GetWholeLineSelectionLength(firstLine, lastLine);
        }

        /// <summary>
        /// Calculates the character length needed to select complete lines.
        /// </summary>
        /// <param name="firstLine">The first line in the selection.</param>
        /// <param name="lastLine">The last line in the selection.</param>
        /// <returns>The length of the whole-line selection.</returns>
        private int GetWholeLineSelectionLength(DocumentLine firstLine, DocumentLine lastLine)
        {
            if (lastLine.LineNumber >= this.Document.LineCount)
            {
                return this.Document.TextLength - firstLine.Offset;
            }

            DocumentLine nextLine = this.Document.GetLineByNumber(lastLine.LineNumber + 1);
            return nextLine.Offset - firstLine.Offset;
        }

        #endregion

        #region JSON operations

        /// <summary>
        /// Formats the document as indented JSON.
        /// </summary>
        private void FormatJson()
        {
            this.TransformJson(static node => node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Formats the document as compact JSON.
        /// </summary>
        private void MinifyJson()
        {
            this.TransformJson(static node => node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        }

        /// <summary>
        /// Parses the document as JSON and replaces it with transformed output.
        /// </summary>
        /// <param name="transform">The transformation to apply to the parsed JSON node.</param>
        private void TransformJson(Func<JsonNode, string> transform)
        {
            string text = this.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                var node = JsonNode.Parse(text, documentOptions: options);
                if (node != null)
                {
                    this.Document.Text = transform(node);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"The document is not valid JSON and could not be transformed.\r\n\r\n{ex.Message}", "Invalid JSON", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Validates the current document as JSON and reports the result to the user.
        /// </summary>
        private void ValidateJson()
        {
            string text = this.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("The document is empty.", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                using var _ = JsonDocument.Parse(text, options);
                MessageBox.Show("The JSON is valid.", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"The JSON is not valid.\r\n\r\n{ex.Message}", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
