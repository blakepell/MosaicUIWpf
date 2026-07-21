/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Collections.ObjectModel;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A lookless control that lists the contents of a directory using a three-column view: <c>Name</c>
    /// (with the operating-system shell icon for the file type), <c>Date Modified</c>, and <c>Size</c>
    /// (formatted in friendly units such as <c>525 KB</c> or <c>2.1 MB</c>). When folder navigation is
    /// enabled (see <see cref="ShowFolders"/>, on by default) sub-folders are listed with a folder icon
    /// and a <c>..</c> parent entry, and double-clicking a folder navigates into it one level at a time;
    /// this behavior can be toggled off to list files only. It supports single or multiple selection, an
    /// optional file-system watcher that keeps the listing in sync with the folder, a manual
    /// <see cref="Refresh"/> method, and raises <see cref="FileDoubleClick"/> (with the
    /// <see cref="FileInfo"/>) when a file is double-clicked. While the list has keyboard focus,
    /// incremental typing selects matching names, <see cref="Key.Enter"/> activates the selection, and
    /// <see cref="Key.F2"/> starts an inline filesystem rename.
    /// </summary>
    [DefaultEvent(nameof(FileDoubleClick))]
    [DefaultProperty(nameof(DirectoryPath))]
    [TemplatePart(Name = PartListView, Type = typeof(ListView))]
    public class Files : Control
    {
        private const string PartListView = "PART_ListView";

        private const string AscendingHeaderTemplateKey = "FilesAscendingHeaderTemplate";
        private const string DescendingHeaderTemplateKey = "FilesDescendingHeaderTemplate";
        private static readonly TimeSpan IncrementalSearchTimeout = TimeSpan.FromSeconds(1);

        private ListView? _listView;
        private FileSystemWatcher? _watcher;
        private readonly DispatcherTimer _refreshTimer;
        private readonly DispatcherTimer _incrementalSearchTimer;
        private GridViewColumn? _sortColumn;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private string _incrementalSearchText = string.Empty;

        /// <summary>
        /// Initializes static metadata for the <see cref="Files"/> class.
        /// </summary>
        static Files()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Files), new FrameworkPropertyMetadata(typeof(Files)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Files"/> class.
        /// </summary>
        public Files()
        {
            this.SetValue(ItemsPropertyKey, new ObservableCollection<FileItem>());

            // Coalesce bursts of file-system change notifications into a single refresh.
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _refreshTimer.Tick += (_, _) =>
            {
                _refreshTimer.Stop();
                this.Refresh();
            };

            _incrementalSearchTimer = new DispatcherTimer { Interval = IncrementalSearchTimeout };
            _incrementalSearchTimer.Tick += (_, _) => this.ResetIncrementalSearch();

            this.Unloaded += (_, _) =>
            {
                this.DisposeWatcher();
                this.ResetIncrementalSearch();
            };
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="DirectoryPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DirectoryPathProperty = DependencyProperty.Register(
            nameof(DirectoryPath), typeof(string), typeof(Files),
            new FrameworkPropertyMetadata(string.Empty, OnDirectoryPathChanged));

        /// <summary>
        /// Gets or sets the full path of the directory whose files are listed.
        /// </summary>
        [Category("Common")]
        [Description("The full path of the directory whose files are listed.")]
        public string DirectoryPath
        {
            get => (string)GetValue(DirectoryPathProperty);
            set => SetValue(DirectoryPathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Filter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
            nameof(Filter), typeof(string), typeof(Files),
            new FrameworkPropertyMetadata("*", OnFilterChanged));

        /// <summary>
        /// Gets or sets the search pattern used to filter the listed files (for example <c>*.txt</c>).
        /// Defaults to <c>*</c>, which lists every file.
        /// </summary>
        [Category("Common")]
        [Description("The search pattern used to filter the listed files (for example *.txt). Defaults to * (all files).")]
        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowHiddenFiles"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowHiddenFilesProperty = DependencyProperty.Register(
            nameof(ShowHiddenFiles), typeof(bool), typeof(Files),
            new FrameworkPropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Gets or sets a value indicating whether files marked hidden or system are included in the listing.
        /// The default is <c>false</c>.
        /// </summary>
        [Category("Common")]
        [Description("Whether files marked hidden or system are included in the listing.")]
        public bool ShowHiddenFiles
        {
            get => (bool)GetValue(ShowHiddenFilesProperty);
            set => SetValue(ShowHiddenFilesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectionMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            nameof(SelectionMode), typeof(SelectionMode), typeof(Files),
            new FrameworkPropertyMetadata(SelectionMode.Single));

        /// <summary>
        /// Gets or sets whether the control allows single or multiple file selection. Use
        /// <see cref="System.Windows.Controls.SelectionMode.Single"/> for single selection, or
        /// <see cref="System.Windows.Controls.SelectionMode.Extended"/>/<see cref="System.Windows.Controls.SelectionMode.Multiple"/>
        /// for multi-selection. The default is <see cref="System.Windows.Controls.SelectionMode.Single"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the control allows single or multiple file selection.")]
        public SelectionMode SelectionMode
        {
            get => (SelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableFileWatcher"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableFileWatcherProperty = DependencyProperty.Register(
            nameof(EnableFileWatcher), typeof(bool), typeof(Files),
            new FrameworkPropertyMetadata(false, OnEnableFileWatcherChanged));

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="FileSystemWatcher"/> monitors the
        /// directory and automatically refreshes the listing when its contents change. Disabled by default.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether a file-system watcher automatically refreshes the listing when the directory contents change. Disabled by default.")]
        public bool EnableFileWatcher
        {
            get => (bool)GetValue(EnableFileWatcherProperty);
            set => SetValue(EnableFileWatcherProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowFolders"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowFoldersProperty = DependencyProperty.Register(
            nameof(ShowFolders), typeof(bool), typeof(Files),
            new FrameworkPropertyMetadata(true, OnShowFoldersChanged));

        /// <summary>
        /// Gets or sets a value indicating whether folder navigation is enabled. When <c>true</c> (the
        /// default), sub-folders are listed alongside files — each shown with a folder icon — and a
        /// <c>..</c> entry is added when a parent folder exists. Double-clicking (or pressing
        /// <see cref="Key.Enter"/> on) a folder navigates into it, replacing the listing with that
        /// folder's contents and moving keyboard focus to the list; the <c>..</c> entry navigates up to
        /// the parent. Only one folder's contents are shown at a time (this is a flat listing, not a tree
        /// view). Set this to <c>false</c> to list files only and disable folder navigation entirely.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether folder navigation is enabled. When on (the default), sub-folders and a '..' parent entry are listed and double-clicking or pressing Enter on a folder navigates into it; this behavior can be toggled off to list files only.")]
        public bool ShowFolders
        {
            get => (bool)GetValue(ShowFoldersProperty);
            set => SetValue(ShowFoldersProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RootDirectory"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RootDirectoryProperty = DependencyProperty.Register(
            nameof(RootDirectory), typeof(string), typeof(Files),
            new FrameworkPropertyMetadata(string.Empty, OnRootDirectoryChanged));

        /// <summary>
        /// Gets or sets the full path of a folder that bounds upward navigation. When set, the user can
        /// browse this folder and its sub-folders (and back up toward it) but cannot navigate above it:
        /// the <c>..</c> parent entry is hidden once the listing reaches this folder, and any attempt to
        /// navigate to a folder outside this subtree is ignored. Leave empty (the default) to allow
        /// navigation anywhere the file system permits. Has no effect when <see cref="ShowFolders"/> is
        /// <c>false</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("A folder that bounds upward navigation. When set, the user cannot navigate above this folder (the '..' entry is hidden at this level). Leave empty to allow navigation anywhere.")]
        public string RootDirectory
        {
            get => (string)GetValue(RootDirectoryProperty);
            set => SetValue(RootDirectoryProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowNavigateErrorMessageBox"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowNavigateErrorMessageBoxProperty = DependencyProperty.Register(
            nameof(ShowNavigateErrorMessageBox), typeof(bool), typeof(Files),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether a warning <see cref="MessageBox"/> showing the error
        /// message is displayed to the user when folder navigation fails. This is independent of the
        /// <see cref="NavigateError"/> event, which is always raised regardless of this setting. The
        /// default is <c>true</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether a warning message box with the error message is shown to the user when folder navigation fails. The NavigateError event is raised regardless. Enabled by default.")]
        public bool ShowNavigateErrorMessageBox
        {
            get => (bool)GetValue(ShowNavigateErrorMessageBoxProperty);
            set => SetValue(ShowNavigateErrorMessageBoxProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowRenameErrorMessageBox"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowRenameErrorMessageBoxProperty = DependencyProperty.Register(
            nameof(ShowRenameErrorMessageBox), typeof(bool), typeof(Files),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether a warning message box is displayed when a file or
        /// folder rename fails. The <see cref="RenameError"/> event is raised regardless of this setting.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to show rename failures; otherwise, <see langword="false"/>. The default
        /// is <see langword="true"/>.
        /// </value>
        [Category("Behavior")]
        [Description("Whether a warning message box is shown when a file or folder rename fails. The RenameError event is raised regardless. Enabled by default.")]
        public bool ShowRenameErrorMessageBox
        {
            get => (bool)GetValue(ShowRenameErrorMessageBoxProperty);
            set => SetValue(ShowRenameErrorMessageBoxProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(FileItem), typeof(Files),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        /// <summary>
        /// Gets or sets the currently selected <see cref="FileItem"/>. In multi-selection mode this
        /// reflects the primary selected item; use <see cref="SelectedFiles"/> for the full set.
        /// </summary>
        [Category("Behavior")]
        [Description("The currently selected file item.")]
        public FileItem? SelectedItem
        {
            get => (FileItem?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static readonly DependencyPropertyKey ItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Items), typeof(ObservableCollection<FileItem>), typeof(Files),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Items"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = ItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the collection of <see cref="FileItem"/> rows currently displayed by the control.
        /// </summary>
        public ObservableCollection<FileItem> Items => (ObservableCollection<FileItem>)GetValue(ItemsProperty);

        /// <summary>
        /// Identifies the <see cref="FileContextMenu"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileContextMenuProperty = DependencyProperty.Register(
            nameof(FileContextMenu), typeof(ContextMenu), typeof(Files),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="ContextMenu"/> shown when a file row is right-clicked. When the menu
        /// opens, its <see cref="FrameworkElement.DataContext"/> is the right-clicked <see cref="FileItem"/>
        /// (and <see cref="ContextMenu.PlacementTarget"/> is the row's container), so menu items can bind
        /// their <c>CommandParameter</c> to <c>{Binding}</c> to receive it. Leave unset to show no menu for
        /// files. Folder rows use <see cref="FolderContextMenu"/> instead; the <c>..</c> parent entry never
        /// shows a menu.
        /// </summary>
        [Category("Behavior")]
        [Description("The context menu shown when a file row is right-clicked. The menu's DataContext is the right-clicked FileItem.")]
        public ContextMenu? FileContextMenu
        {
            get => (ContextMenu?)GetValue(FileContextMenuProperty);
            set => SetValue(FileContextMenuProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FolderContextMenu"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FolderContextMenuProperty = DependencyProperty.Register(
            nameof(FolderContextMenu), typeof(ContextMenu), typeof(Files),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="ContextMenu"/> shown when a folder row is right-clicked. When the menu
        /// opens, its <see cref="FrameworkElement.DataContext"/> is the right-clicked <see cref="FileItem"/>
        /// (and <see cref="ContextMenu.PlacementTarget"/> is the row's container), so menu items can bind
        /// their <c>CommandParameter</c> to <c>{Binding}</c> to receive it. Leave unset to show no menu for
        /// folders. The <c>..</c> parent-navigation entry never shows a menu. File rows use
        /// <see cref="FileContextMenu"/> instead.
        /// </summary>
        [Category("Behavior")]
        [Description("The context menu shown when a folder row (other than the '..' parent entry) is right-clicked. The menu's DataContext is the right-clicked FileItem.")]
        public ContextMenu? FolderContextMenu
        {
            get => (ContextMenu?)GetValue(FolderContextMenuProperty);
            set => SetValue(FolderContextMenuProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FileActivatedCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileActivatedCommandProperty = DependencyProperty.Register(
            nameof(FileActivatedCommand), typeof(ICommand), typeof(Files),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command executed when a file is double-clicked. The command parameter is the
        /// activated <see cref="FileInfo"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("A command executed when a file is double-clicked. The command parameter is the activated FileInfo.")]
        public ICommand? FileActivatedCommand
        {
            get => (ICommand?)GetValue(FileActivatedCommandProperty);
            set => SetValue(FileActivatedCommandProperty, value);
        }

        #endregion

        #region Read-only Computed Properties

        /// <summary>
        /// Gets the <see cref="FileInfo"/> for the currently selected item, or <c>null</c> if nothing is
        /// selected or the selected item is a folder.
        /// </summary>
        public FileInfo? SelectedFile => this.SelectedItem is { IsDirectory: false } item ? item.FileInfo : null;

        /// <summary>
        /// Gets the <see cref="FileInfo"/> for every selected item, excluding folders. In single-selection
        /// mode this contains at most one element.
        /// </summary>
        public IReadOnlyList<FileInfo> SelectedFiles
        {
            get
            {
                if (_listView == null)
                {
                    var single = this.SelectedItem;
                    return single is { IsDirectory: false } ? new[] { single.FileInfo } : Array.Empty<FileInfo>();
                }

                return _listView.SelectedItems
                    .OfType<FileItem>()
                    .Where(i => !i.IsDirectory)
                    .Select(i => i.FileInfo)
                    .ToList();
            }
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="FileDoubleClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent FileDoubleClickEvent = EventManager.RegisterRoutedEvent(
            nameof(FileDoubleClick), RoutingStrategy.Bubble, typeof(FileActivatedEventHandler), typeof(Files));

        /// <summary>
        /// Occurs when a file row is double-clicked. The event arguments carry the <see cref="FileInfo"/> of the file.
        /// </summary>
        public event FileActivatedEventHandler FileDoubleClick
        {
            add => AddHandler(FileDoubleClickEvent, value);
            remove => RemoveHandler(FileDoubleClickEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectionChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(Files));

        /// <summary>
        /// Occurs when the set of selected files changes, in either single- or multi-selection mode.
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="NavigateError"/> routed event.
        /// </summary>
        public static readonly RoutedEvent NavigateErrorEvent = EventManager.RegisterRoutedEvent(
            nameof(NavigateError), RoutingStrategy.Bubble, typeof(FilesNavigateErrorEventHandler), typeof(Files));

        /// <summary>
        /// Occurs when folder navigation fails (for example the target folder no longer exists or access
        /// is denied). The event arguments carry the attempted path and the <see cref="Exception"/>. The
        /// failure is handled gracefully so it never propagates to the host application.
        /// </summary>
        public event FilesNavigateErrorEventHandler NavigateError
        {
            add => AddHandler(NavigateErrorEvent, value);
            remove => RemoveHandler(NavigateErrorEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="RenameError"/> routed event.
        /// </summary>
        public static readonly RoutedEvent RenameErrorEvent = EventManager.RegisterRoutedEvent(
            nameof(RenameError), RoutingStrategy.Bubble, typeof(FilesRenameErrorEventHandler), typeof(Files));

        /// <summary>
        /// Occurs when an inline file or folder rename fails. The original name is restored before this
        /// event is raised.
        /// </summary>
        public event FilesRenameErrorEventHandler RenameError
        {
            add => AddHandler(RenameErrorEvent, value);
            remove => RemoveHandler(RenameErrorEvent, value);
        }

        #endregion

        #region Template / Selection Wiring

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ResetIncrementalSearch();

            if (_listView != null)
            {
                _listView.MouseDoubleClick -= this.OnListViewMouseDoubleClick;
                _listView.SelectionChanged -= this.OnListViewSelectionChanged;
                _listView.PreviewKeyDown -= this.OnListViewKeyDown;
                _listView.PreviewTextInput -= this.OnListViewPreviewTextInput;
                _listView.PreviewMouseDown -= this.OnListViewPreviewMouseDown;
                _listView.LostKeyboardFocus -= this.OnListViewLostKeyboardFocus;
                _listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.OnColumnHeaderClick));
            }

            // The previous template's column instances are gone; forget the active sort column (the
            // collection-view sort itself is reapplied below so the data order is preserved).
            _sortColumn = null;
            _listView = this.GetTemplateChild(PartListView) as ListView;

            if (_listView != null)
            {
                _listView.MouseDoubleClick += this.OnListViewMouseDoubleClick;
                _listView.SelectionChanged += this.OnListViewSelectionChanged;
                _listView.PreviewKeyDown += this.OnListViewKeyDown;
                _listView.PreviewTextInput += this.OnListViewPreviewTextInput;
                _listView.PreviewMouseDown += this.OnListViewPreviewMouseDown;
                _listView.LostKeyboardFocus += this.OnListViewLostKeyboardFocus;
                _listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.OnColumnHeaderClick));
                this.ApplyItemContainerStyle();
            }

            this.Refresh();
            this.RestoreSortIndicator();
        }

        /// <summary>
        /// Builds the <see cref="ListView"/> item-container style used to attach the per-row context menu.
        /// The style derives (via <see cref="Style.BasedOn"/>) from the themed <see cref="ListViewItem"/>
        /// style currently in scope so Mosaic's theming is preserved, then layers on the context-menu
        /// selection: files use <see cref="FileContextMenu"/>, folders use <see cref="FolderContextMenu"/>,
        /// and the <c>..</c> parent-navigation entry shows none. The themed style is resolved here at
        /// runtime — rather than with a XAML <c>StaticResource</c> in the template — because the native
        /// control styles are merged into the resource scope dynamically and are not present when this
        /// control's template is parsed.
        /// </summary>
        private void ApplyItemContainerStyle()
        {
            if (_listView == null)
            {
                return;
            }

            // Implicit styles are keyed by their target type; when the native styles are not enabled this
            // returns null and the WPF default theme style continues to apply as the lower-precedence base.
            var baseStyle = _listView.TryFindResource(typeof(ListViewItem)) as Style;
            var style = new Style(typeof(ListViewItem), baseStyle);

            style.Triggers.Add(this.BuildContextMenuTrigger(nameof(FileItem.IsDirectory), false, FileContextMenuProperty));
            style.Triggers.Add(this.BuildContextMenuTrigger(nameof(FileItem.IsDirectory), true, FolderContextMenuProperty));

            // Must follow the folder trigger: the ".." parent entry is also a directory and gets no menu.
            var parentTrigger = new DataTrigger { Binding = new Binding(nameof(FileItem.IsParentNavigation)), Value = true };
            parentTrigger.Setters.Add(new Setter(ContextMenuProperty, null));
            style.Triggers.Add(parentTrigger);

            _listView.ItemContainerStyle = style;
        }

        /// <summary>
        /// Creates a <see cref="DataTrigger"/> that, when the bound <see cref="FileItem"/> property equals
        /// <paramref name="value"/>, sets each row's <see cref="FrameworkElement.ContextMenu"/> to the menu
        /// held by <paramref name="menuProperty"/> on this control (bound so later changes propagate).
        /// </summary>
        private DataTrigger BuildContextMenuTrigger(string itemProperty, object value, DependencyProperty menuProperty)
        {
            var trigger = new DataTrigger { Binding = new Binding(itemProperty), Value = value };
            trigger.Setters.Add(new Setter(ContextMenuProperty, new Binding(menuProperty.Name) { Source = this }));
            return trigger;
        }

        /// <summary>
        /// Propagates selection changes from the template list view through the control's routed event.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The selection change details.</param>
        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectedItem is two-way bound in the template; just notify the computed selection helpers.
            this.OnPropertyChangedCompat();

            // Re-raise as a SelectionChanged routed event sourced from this control, carrying the
            // same added/removed items so handlers can inspect the delta.
            this.RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
        }

        /// <summary>
        /// Activates the file-system item beneath a double-click in the template list view.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The mouse button event details.</param>
        private void OnListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Resolve the row that was actually double-clicked (ignore clicks on empty space or headers).
            var item = this.GetFileItemFromEventSource(e.OriginalSource as DependencyObject);

            if (item != null)
            {
                this.ActivateItem(item);
            }
        }

        /// <summary>
        /// Processes printable text as an incremental search against visible item names.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The text composition event details.</param>
        private void OnListViewPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (IsTextEntryFocused() || string.IsNullOrEmpty(e.Text) || e.Text.Any(char.IsControl))
            {
                return;
            }

            bool cycle = _incrementalSearchText.Length == 1
                && e.Text.Length == 1
                && string.Equals(_incrementalSearchText, e.Text, StringComparison.CurrentCultureIgnoreCase);

            if (!cycle)
            {
                _incrementalSearchText += e.Text;
            }

            _incrementalSearchTimer.Stop();
            _incrementalSearchTimer.Start();

            this.SelectIncrementalSearchMatch(_incrementalSearchText, cycle);
            e.Handled = true;
        }

        /// <summary>
        /// Ends the current incremental search when mouse navigation begins.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The mouse button event details.</param>
        private void OnListViewPreviewMouseDown(object sender, MouseButtonEventArgs e) => this.ResetIncrementalSearch();

        /// <summary>
        /// Ends the current incremental search when keyboard focus leaves the template list view.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The keyboard focus change details.</param>
        private void OnListViewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_listView?.IsKeyboardFocusWithin != true)
            {
                this.ResetIncrementalSearch();
            }
        }

        /// <summary>
        /// Handles rename, activation, and navigation keys for the template list view.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The key event details.</param>
        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsTextEntryFocused())
            {
                return;
            }

            if (IsNavigationKey(e.Key))
            {
                this.ResetIncrementalSearch();
            }

            if (e.Key == Key.F2)
            {
                if (this.SelectedItem is { IsParentNavigation: false } renameItem)
                {
                    this.BeginRename(renameItem);
                    e.Handled = true;
                }

                return;
            }

            // Keyboard parity with double-click: Enter activates the focused row (navigate a folder or
            // raise the file-activated event/command).
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (this.SelectedItem is { } item)
            {
                this.ActivateItem(item);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Determines whether a key performs list navigation or activation.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the key performs list navigation or activation; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsNavigationKey(Key key)
        {
            return key is Key.Left or Key.Right or Key.Up or Key.Down
                or Key.Home or Key.End or Key.PageUp or Key.PageDown
                or Key.Enter or Key.Escape or Key.Tab;
        }

        /// <summary>
        /// Determines whether keyboard focus is currently inside a text-entry control.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if a text-entry control has keyboard focus; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsTextEntryFocused()
        {
            return Keyboard.FocusedElement is TextBoxBase or PasswordBox;
        }

        /// <summary>
        /// Selects the next visible item whose name begins with the accumulated search text.
        /// </summary>
        /// <param name="searchText">The accumulated search text.</param>
        /// <param name="cycle">
        /// <see langword="true"/> to begin after the current selection and wrap; otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a matching item is selected; otherwise, <see langword="false"/>.
        /// </returns>
        private bool SelectIncrementalSearchMatch(string searchText, bool cycle)
        {
            if (_listView == null || _listView.Items.Count == 0)
            {
                return false;
            }

            int count = _listView.Items.Count;
            int startIndex = cycle ? Math.Max(_listView.SelectedIndex + 1, 0) : 0;

            for (int offset = 0; offset < count; offset++)
            {
                int index = cycle ? (startIndex + offset) % count : offset;
                if (_listView.Items[index] is not FileItem item
                    || !item.Name.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                _listView.SelectedItem = item;
                _listView.ScrollIntoView(item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the accumulated incremental-search text and stops its reset timer.
        /// </summary>
        private void ResetIncrementalSearch()
        {
            _incrementalSearchTimer.Stop();
            _incrementalSearchText = string.Empty;
        }

        /// <summary>
        /// Places the editable name control for an item into rename mode.
        /// </summary>
        /// <param name="item">The item to rename.</param>
        private void BeginRename(FileItem item)
        {
            if (_listView == null)
            {
                return;
            }

            this.ResetIncrementalSearch();
            _listView.ScrollIntoView(item);
            _listView.UpdateLayout();

            if (_listView.ItemContainerGenerator.ContainerFromItem(item) is not ListViewItem container)
            {
                return;
            }

            var editor = FindVisualDescendant<EditableTextBlock>(container);
            if (editor == null)
            {
                return;
            }

            editor.ApplyTemplate();
            editor.TextUpdated -= this.OnEditableNameTextUpdated;
            editor.TextUpdated += this.OnEditableNameTextUpdated;
            editor.IsFilePath = !item.IsDirectory;
            editor.EditMode();
        }

        /// <summary>
        /// Validates an edited name and applies the corresponding file-system rename.
        /// </summary>
        /// <param name="sender">The editable name control.</param>
        /// <param name="e">The event details.</param>
        private void OnEditableNameTextUpdated(object? sender, EventArgs e)
        {
            if (sender is not EditableTextBlock editor || editor.DataContext is not FileItem item)
            {
                return;
            }

            editor.TextUpdated -= this.OnEditableNameTextUpdated;

            string originalName = item.Name;
            string requestedName = editor.Text.Trim();
            string sourcePath = item.FullPath;
            string destinationPath = sourcePath;

            try
            {
                ValidateFileName(requestedName);

                string? parentPath = Path.GetDirectoryName(sourcePath);
                if (string.IsNullOrEmpty(parentPath))
                {
                    throw new IOException("The item does not have a parent directory and cannot be renamed.");
                }

                destinationPath = Path.Combine(parentPath, requestedName);
                if (string.Equals(sourcePath, destinationPath, StringComparison.Ordinal))
                {
                    editor.Text = originalName;
                    return;
                }

                if (item.IsDirectory)
                {
                    Directory.Move(sourcePath, destinationPath);
                    item.Update(new DirectoryInfo(destinationPath), isParentNavigation: false);
                }
                else
                {
                    File.Move(sourcePath, destinationPath);
                    item.Update(new FileInfo(destinationPath));
                }

                CollectionViewSource.GetDefaultView(this.Items)?.Refresh();
                this.SelectedItem = item;
                _listView?.ScrollIntoView(item);
            }
            catch (Exception ex)
            {
                editor.Text = originalName;
                this.OnRenameError(sourcePath, destinationPath, ex);
            }
            finally
            {
                BindingOperations.SetBinding(
                    editor,
                    EditableTextBlock.TextProperty,
                    new Binding(nameof(FileItem.Name)) { Mode = BindingMode.OneWay });
            }
        }

        /// <summary>
        /// Validates a proposed file or folder name.
        /// </summary>
        /// <param name="fileName">The proposed file or folder name.</param>
        /// <exception cref="ArgumentException">
        /// The name is empty, reserved, or contains invalid file-name characters.
        /// </exception>
        private static void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("A file or folder name cannot be empty.", nameof(fileName));
            }

            if (fileName is "." or ".." || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"'{fileName}' is not a valid file or folder name.", nameof(fileName));
            }
        }

        /// <summary>
        /// Finds the first visual descendant of a specified type.
        /// </summary>
        /// <typeparam name="T">The descendant type to locate.</typeparam>
        /// <param name="parent">The visual parent to search.</param>
        /// <returns>The first matching descendant, or <see langword="null"/> when no match exists.</returns>
        private static T? FindVisualDescendant<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualDescendant<T>(child) is { } descendant)
                {
                    return descendant;
                }
            }

            return null;
        }

        /// <summary>
        /// Activates a row: when folder navigation is enabled and the row is a folder, navigates into it
        /// (the <c>..</c> entry navigates up) and moves focus to the list; otherwise raises
        /// <see cref="FileDoubleClick"/> and executes <see cref="FileActivatedCommand"/> for the file.
        /// </summary>
        private void ActivateItem(FileItem item)
        {
            if (this.ShowFolders && item.IsDirectory)
            {
                this.NavigateToFolder(item.FullPath);
                return;
            }

            var info = item.FileInfo;
            this.RaiseEvent(new FileActivatedEventArgs(FileDoubleClickEvent, this, info));

            if (this.FileActivatedCommand?.CanExecute(info) == true)
            {
                this.FileActivatedCommand.Execute(info);
            }
        }

        /// <summary>
        /// Navigates into the supplied folder, replacing the listing with that folder's contents and
        /// moving keyboard focus into the list. The folder is probed for access up front so that any
        /// failure is surfaced here — raising <see cref="NavigateError"/> (and optionally a warning
        /// message box) without changing the current directory — instead of crashing the host application.
        /// </summary>
        /// <param name="path">The full path of the folder to navigate into.</param>
        private void NavigateToFolder(string path)
        {
            if (!this.IsWithinRoot(path))
            {
                // The target is above the configured RootDirectory boundary; navigation is not permitted.
                return;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"The folder '{path}' could not be found.");
                }

                // Probe access before committing the change so a permission/IO error is attributed to this
                // navigation here, rather than being silently swallowed by Refresh after DirectoryPath has
                // already changed (which would leave the user stranded on an empty listing).
                _ = new DirectoryInfo(path).EnumerateFileSystemInfos().Any();

                this.DirectoryPath = path;
                this.Dispatcher.BeginInvoke(() => _listView?.Focus(), DispatcherPriority.Input);
            }
            catch (Exception ex)
            {
                this.OnNavigateError(path, ex);
            }
        }

        /// <summary>
        /// Raises the <see cref="NavigateError"/> event for a failed navigation and, when
        /// <see cref="ShowNavigateErrorMessageBox"/> is enabled, shows a warning message box with the
        /// exception message.
        /// </summary>
        /// <param name="path">The full path of the folder that navigation was attempted to.</param>
        /// <param name="exception">The exception that occurred while navigating.</param>
        protected virtual void OnNavigateError(string path, Exception exception)
        {
            this.RaiseEvent(new FilesNavigateErrorEventArgs(NavigateErrorEvent, this, path, exception));

            if (this.ShowNavigateErrorMessageBox)
            {
                MessageBox.Show(exception.Message, "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Raises the <see cref="RenameError"/> event and optionally displays the failure to the user.
        /// </summary>
        /// <param name="sourcePath">The original full path.</param>
        /// <param name="destinationPath">The requested full path.</param>
        /// <param name="exception">The exception raised by the file system.</param>
        protected virtual void OnRenameError(string sourcePath, string destinationPath, Exception exception)
        {
            this.RaiseEvent(new FilesRenameErrorEventArgs(
                RenameErrorEvent,
                this,
                sourcePath,
                destinationPath,
                exception));

            if (this.ShowRenameErrorMessageBox)
            {
                MessageBox.Show(exception.Message, "Rename Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Determines whether the supplied folder lies at or below the configured <see cref="RootDirectory"/>
        /// boundary. Returns <c>true</c> for every path when no root is configured.
        /// </summary>
        /// <param name="path">The full path of the folder to test.</param>
        private bool IsWithinRoot(string path)
        {
            string root = this.RootDirectory;

            if (string.IsNullOrWhiteSpace(root))
            {
                return true;
            }

            try
            {
                string normalizedRoot = NormalizePath(root);
                string normalizedPath = NormalizePath(path);

                return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                       || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
                // A malformed root or path can't bound anything meaningfully; fail open rather than trap the user.
                return true;
            }
        }

        /// <summary>
        /// Produces a canonical, comparable form of a path: fully qualified with any trailing directory
        /// separators removed.
        /// </summary>
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Walks up the visual tree from the event source to find the <see cref="FileItem"/> backing the
        /// containing <see cref="ListViewItem"/>, if any.
        /// </summary>
        private FileItem? GetFileItemFromEventSource(DependencyObject? source)
        {
            while (source != null && source != _listView)
            {
                if (source is ListViewItem container)
                {
                    return container.DataContext as FileItem;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        /// <summary>
        /// Notifies a control when its selected item changes.
        /// </summary>
        /// <param name="d">The control whose selected item changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Files)d).OnPropertyChangedCompat();
        }

        /// <summary>
        /// Raises selection-related change notifications so that data bindings against the computed
        /// <see cref="SelectedFile"/>/<see cref="SelectedFiles"/> helpers re-evaluate. These are CLR
        /// properties, so this is informational only for any external INotify consumers.
        /// </summary>
        private void OnPropertyChangedCompat()
        {
            // Intentionally minimal: SelectedFile/SelectedFiles are read on demand. This hook exists so
            // future change-tracking can be added without altering call sites.
        }

        #endregion

        #region Sorting

        /// <summary>
        /// Identifies the <c>SortMemberPath</c> attached property, set on a <see cref="GridViewColumn"/>
        /// to declare the <see cref="FileItem"/> property the column sorts by (for example <c>Name</c>,
        /// <c>DateModified</c>, or <c>Size</c>). The sort uses the underlying typed value, not the
        /// formatted display string, so dates and sizes sort chronologically and numerically.
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty = DependencyProperty.RegisterAttached(
            "SortMemberPath", typeof(string), typeof(Files), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Sets the <see cref="SortMemberPathProperty"/> attached property on a column.
        /// </summary>
        public static void SetSortMemberPath(DependencyObject element, string? value) => element.SetValue(SortMemberPathProperty, value);

        /// <summary>
        /// Gets the <see cref="SortMemberPathProperty"/> attached property from a column.
        /// </summary>
        public static string? GetSortMemberPath(DependencyObject element) => (string?)element.GetValue(SortMemberPathProperty);

        /// <summary>
        /// Applies or reverses sorting for a clicked column header.
        /// </summary>
        /// <param name="sender">The template list view.</param>
        /// <param name="e">The routed event details.</param>
        private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            // Ignore the trailing "padding" header (its Column is null) and any non-header sources.
            if (e.OriginalSource is not GridViewColumnHeader { Column: { } column })
            {
                return;
            }

            // Toggle direction when re-clicking the active column; otherwise start ascending.
            var direction = column == _sortColumn && _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            this.ApplySort(column, direction);
        }

        /// <summary>
        /// Sorts the listing by the <see cref="FileItem"/> property identified by <paramref name="memberPath"/>
        /// in the requested direction, updating the column sort indicator if a matching column is present.
        /// </summary>
        /// <param name="memberPath">The <see cref="FileItem"/> property name to sort by (for example <c>Name</c>, <c>DateModified</c>, <c>Size</c>).</param>
        /// <param name="direction">The sort direction.</param>
        public void SortBy(string memberPath, ListSortDirection direction = ListSortDirection.Ascending)
        {
            var column = this.FindColumnByMemberPath(memberPath);

            if (column != null)
            {
                this.ApplySort(column, direction);
            }
            else
            {
                // No column matches; still sort the data so programmatic callers get the requested order.
                this.ApplySortDescription(memberPath, direction);
            }
        }

        /// <summary>
        /// Applies the sort to the collection view and moves the up/down chevron indicator to the column.
        /// </summary>
        private void ApplySort(GridViewColumn column, ListSortDirection direction)
        {
            string? memberPath = GetSortMemberPath(column);

            if (string.IsNullOrEmpty(memberPath))
            {
                return;
            }

            this.ApplySortDescription(memberPath, direction);

            // Clear the indicator from the previously sorted column, if it differs.
            if (_sortColumn != null && _sortColumn != column)
            {
                _sortColumn.HeaderTemplate = null;
            }

            _sortColumn = column;
            _sortDirection = direction;

            column.HeaderTemplate = this.GetHeaderTemplate(direction);
        }

        /// <summary>
        /// Replaces the sort description on the default collection view with a single description on the
        /// supplied member path. Sorting the typed property (rather than the display string) yields correct
        /// chronological and numeric ordering for the date and size columns.
        /// </summary>
        private void ApplySortDescription(string memberPath, ListSortDirection direction)
        {
            var view = CollectionViewSource.GetDefaultView(this.Items);

            if (view == null)
            {
                return;
            }

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();

                // Keep folders (and the ".." entry) grouped above files regardless of the chosen column,
                // so navigation targets always stay together at the top of the listing.
                if (this.ShowFolders)
                {
                    view.SortDescriptions.Add(new SortDescription(nameof(FileItem.SortGroup), ListSortDirection.Ascending));
                }

                view.SortDescriptions.Add(new SortDescription(memberPath, direction));
            }
        }

        /// <summary>
        /// Reapplies the current chevron indicator after the template (and therefore the column instances)
        /// has been rebuilt, matching the active sort description by member path.
        /// </summary>
        private void RestoreSortIndicator()
        {
            var view = CollectionViewSource.GetDefaultView(this.Items);

            if (view == null || view.SortDescriptions.Count == 0)
            {
                return;
            }

            // The last description carries the user-selected column (a leading SortGroup description may
            // be present to keep folders grouped above files).
            var sort = view.SortDescriptions[view.SortDescriptions.Count - 1];
            var column = this.FindColumnByMemberPath(sort.PropertyName);

            if (column == null)
            {
                return;
            }

            _sortColumn = column;
            _sortDirection = sort.Direction;
            column.HeaderTemplate = this.GetHeaderTemplate(sort.Direction);
        }

        /// <summary>
        /// Locates the <see cref="GridViewColumn"/> whose <see cref="SortMemberPathProperty"/> matches the path.
        /// </summary>
        private GridViewColumn? FindColumnByMemberPath(string memberPath)
        {
            if (_listView?.View is not GridView gridView)
            {
                return null;
            }

            return gridView.Columns.FirstOrDefault(c => string.Equals(GetSortMemberPath(c), memberPath, StringComparison.Ordinal));
        }

        /// <summary>
        /// Resolves the ascending/descending header <see cref="DataTemplate"/> declared in the control template.
        /// </summary>
        private DataTemplate? GetHeaderTemplate(ListSortDirection direction)
        {
            string key = direction == ListSortDirection.Ascending ? AscendingHeaderTemplateKey : DescendingHeaderTemplateKey;
            return _listView?.TryFindResource(key) as DataTemplate;
        }

        #endregion

        #region Refresh / Population

        /// <summary>
        /// Re-reads the directory and synchronizes the displayed listing with the current set of files
        /// on disk. Existing rows are updated in place (preserving selection) where possible.
        /// </summary>
        public void Refresh()
        {
            var items = this.Items;

            if (items == null)
            {
                return;
            }

            string path = this.DirectoryPath;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                items.Clear();
                return;
            }

            var current = new List<FileEntry>();

            try
            {
                var directory = new DirectoryInfo(path);

                // When folder navigation is on, list sub-folders first (with a leading ".." entry when a
                // parent exists) so the user can drill in and out one folder at a time.
                if (this.ShowFolders)
                {
                    if (directory.Parent is { } parent && this.IsWithinRoot(parent.FullName))
                    {
                        current.Add(new FileEntry(parent, IsDirectory: true, IsParent: true));
                    }

                    foreach (var subDirectory in directory
                        .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                        .Where(this.IsVisible)
                        .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
                    {
                        current.Add(new FileEntry(subDirectory, IsDirectory: true, IsParent: false));
                    }
                }

                string pattern = string.IsNullOrWhiteSpace(this.Filter) ? "*" : this.Filter;

                foreach (var file in directory
                    .EnumerateFiles(pattern, SearchOption.TopDirectoryOnly)
                    .Where(this.IsVisible)
                    .OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    current.Add(new FileEntry(file, IsDirectory: false, IsParent: false));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The directory may have been removed or become inaccessible between checks.
                items.Clear();
                return;
            }

            this.SyncItems(items, current);
        }

        /// <summary>
        /// Determines whether a file-system entry should appear in the listing based on the
        /// <see cref="ShowHiddenFiles"/> setting.
        /// </summary>
        private bool IsVisible(FileSystemInfo entry)
        {
            if (this.ShowHiddenFiles)
            {
                return true;
            }

            return (entry.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0;
        }

        /// <summary>
        /// A single enumerated listing entry (a file, a sub-folder, or the <c>..</c> parent navigation entry),
        /// paired with the flags needed to build or update the matching <see cref="FileItem"/> row.
        /// </summary>
        private readonly record struct FileEntry(FileSystemInfo Info, bool IsDirectory, bool IsParent);

        /// <summary>
        /// Reconciles the displayed collection with the freshly enumerated set: updates rows that still
        /// exist, removes rows whose files are gone, and inserts rows for new files — all keyed by full path.
        /// </summary>
        private void SyncItems(ObservableCollection<FileItem> items, List<FileEntry> current)
        {
            // Key rows by full path plus kind: a folder and a file could in theory share a path across a
            // refresh (a delete + create of the same name), and we must not reuse a row of the wrong kind.
            var byKey = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                byKey[KeyFor(item.FullPath, item.IsDirectory)] = item;
            }

            var currentKeys = new HashSet<string>(current.Select(e => KeyFor(e.Info.FullName, e.IsDirectory)), StringComparer.OrdinalIgnoreCase);

            // Remove rows whose entries no longer exist (iterate backward for safe in-place removal).
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (!currentKeys.Contains(KeyFor(items[i].FullPath, items[i].IsDirectory)))
                {
                    items.RemoveAt(i);
                }
            }

            // Update existing rows and insert new ones at their sorted positions.
            for (int i = 0; i < current.Count; i++)
            {
                var entry = current[i];
                string key = KeyFor(entry.Info.FullName, entry.IsDirectory);

                if (byKey.TryGetValue(key, out var existing))
                {
                    UpdateItem(existing, entry);

                    int currentIndex = items.IndexOf(existing);
                    if (currentIndex != i && i < items.Count)
                    {
                        items.Move(currentIndex, i);
                    }
                }
                else
                {
                    var item = CreateItem(entry);

                    if (i < items.Count)
                    {
                        items.Insert(i, item);
                    }
                    else
                    {
                        items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a collection identity key from a full path and item kind.
        /// </summary>
        /// <param name="fullPath">The full path of the file-system item.</param>
        /// <param name="isDirectory">
        /// <see langword="true"/> if the item is a directory; otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>The identity key for the file-system item.</returns>
        private static string KeyFor(string fullPath, bool isDirectory) => (isDirectory ? "D:" : "F:") + fullPath;

        /// <summary>
        /// Creates a display item from an enumerated file-system entry.
        /// </summary>
        /// <param name="entry">The enumerated file-system entry.</param>
        /// <returns>The display item for the entry.</returns>
        private static FileItem CreateItem(FileEntry entry)
        {
            return entry.Info is DirectoryInfo directory
                ? new FileItem(directory, entry.IsParent)
                : new FileItem((FileInfo)entry.Info);
        }

        /// <summary>
        /// Updates an existing display item from an enumerated file-system entry.
        /// </summary>
        /// <param name="item">The display item to update.</param>
        /// <param name="entry">The enumerated file-system entry.</param>
        private static void UpdateItem(FileItem item, FileEntry entry)
        {
            if (entry.Info is DirectoryInfo directory)
            {
                item.Update(directory, entry.IsParent);
            }
            else
            {
                item.Update((FileInfo)entry.Info);
            }
        }

        /// <summary>
        /// Refreshes a control and recreates its watcher after the directory path changes.
        /// </summary>
        /// <param name="d">The control whose directory path changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnDirectoryPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Files)d;
            control.ResetIncrementalSearch();
            control.RecreateWatcherIfNeeded();
            control.Refresh();
        }

        /// <summary>
        /// Refreshes a control after its file filter changes.
        /// </summary>
        /// <param name="d">The control whose file filter changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Files)d).Refresh();
        }

        /// <summary>
        /// Refreshes a control after its folder visibility setting changes.
        /// </summary>
        /// <param name="d">The control whose folder visibility setting changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnShowFoldersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Files)d).Refresh();
        }

        /// <summary>
        /// Refreshes a control after its navigation root changes.
        /// </summary>
        /// <param name="d">The control whose navigation root changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnRootDirectoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The visibility of the ".." parent entry depends on the boundary, so re-read the listing.
            ((Files)d).Refresh();
        }

        #endregion

        #region File System Watcher

        /// <summary>
        /// Creates or disposes a control's file-system watcher when monitoring changes.
        /// </summary>
        /// <param name="d">The control whose monitoring setting changed.</param>
        /// <param name="e">The dependency property change details.</param>
        private static void OnEnableFileWatcherChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Files)d;

            if ((bool)e.NewValue)
            {
                control.RecreateWatcherIfNeeded();
            }
            else
            {
                control.DisposeWatcher();
            }
        }

        /// <summary>
        /// Creates (or recreates) the <see cref="FileSystemWatcher"/> to monitor the current directory,
        /// provided watching is enabled and the directory exists.
        /// </summary>
        private void RecreateWatcherIfNeeded()
        {
            this.DisposeWatcher();

            if (!this.EnableFileWatcher)
            {
                return;
            }

            string path = this.DirectoryPath;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.Attributes,
                    IncludeSubdirectories = false
                };

                _watcher.Created += this.OnWatcherChanged;
                _watcher.Deleted += this.OnWatcherChanged;
                _watcher.Renamed += this.OnWatcherChanged;
                _watcher.Changed += this.OnWatcherChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex) when (ex is IOException or ArgumentException)
            {
                this.DisposeWatcher();
            }
        }

        /// <summary>
        /// Schedules a debounced refresh after the watcher reports a file-system change.
        /// </summary>
        /// <param name="sender">The file-system watcher.</param>
        /// <param name="e">The file-system change details.</param>
        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {    
            this.Dispatcher.BeginInvoke(() =>
            {
                _refreshTimer.Stop();
                _refreshTimer.Start();
            });
        }

        /// <summary>
        /// Detaches event handlers and disposes the current file-system watcher.
        /// </summary>
        private void DisposeWatcher()
        {
            if (_watcher == null)
            {
                return;
            }

            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= this.OnWatcherChanged;
            _watcher.Deleted -= this.OnWatcherChanged;
            _watcher.Renamed -= this.OnWatcherChanged;
            _watcher.Changed -= this.OnWatcherChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        #endregion

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FilesAutomationPeer(this);
        }
    }
}
