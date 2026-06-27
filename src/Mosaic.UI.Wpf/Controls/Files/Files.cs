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
    /// <see cref="FileInfo"/>) when a file is double-clicked.
    /// </summary>
    [DefaultEvent(nameof(FileDoubleClick))]
    [DefaultProperty(nameof(DirectoryPath))]
    [TemplatePart(Name = PartListView, Type = typeof(ListView))]
    public class Files : Control
    {
        private const string PartListView = "PART_ListView";

        private const string AscendingHeaderTemplateKey = "FilesAscendingHeaderTemplate";
        private const string DescendingHeaderTemplateKey = "FilesDescendingHeaderTemplate";

        private ListView? _listView;
        private FileSystemWatcher? _watcher;
        private readonly DispatcherTimer _refreshTimer;
        private GridViewColumn? _sortColumn;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;

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

            this.Unloaded += (_, _) => this.DisposeWatcher();
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

        #endregion

        #region Template / Selection Wiring

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_listView != null)
            {
                _listView.MouseDoubleClick -= this.OnListViewMouseDoubleClick;
                _listView.SelectionChanged -= this.OnListViewSelectionChanged;
                _listView.KeyDown -= this.OnListViewKeyDown;
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
                _listView.KeyDown += this.OnListViewKeyDown;
                _listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.OnColumnHeaderClick));
            }

            this.Refresh();
            this.RestoreSortIndicator();
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectedItem is two-way bound in the template; just notify the computed selection helpers.
            this.OnPropertyChangedCompat();

            // Re-raise as a SelectionChanged routed event sourced from this control, carrying the
            // same added/removed items so handlers can inspect the delta.
            this.RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
        }

        private void OnListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Resolve the row that was actually double-clicked (ignore clicks on empty space or headers).
            var item = this.GetFileItemFromEventSource(e.OriginalSource as DependencyObject);

            if (item != null)
            {
                this.ActivateItem(item);
            }
        }

        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
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
                    if (directory.Parent is { } parent)
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

        private static string KeyFor(string fullPath, bool isDirectory) => (isDirectory ? "D:" : "F:") + fullPath;

        private static FileItem CreateItem(FileEntry entry)
        {
            return entry.Info is DirectoryInfo directory
                ? new FileItem(directory, entry.IsParent)
                : new FileItem((FileInfo)entry.Info);
        }

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

        private static void OnDirectoryPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Files)d;
            control.RecreateWatcherIfNeeded();
            control.Refresh();
        }

        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Files)d).Refresh();
        }

        private static void OnShowFoldersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Files)d).Refresh();
        }

        #endregion

        #region File System Watcher

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

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {    
            this.Dispatcher.BeginInvoke(() =>
            {
                _refreshTimer.Stop();
                _refreshTimer.Start();
            });
        }

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
