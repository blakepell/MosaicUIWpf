/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Specifies how document tabs are arranged when they exceed the available width.
    /// </summary>
    public enum DocumentTabOverflowMode
    {
        /// <summary>
        /// Arranges document tabs across multiple rows.
        /// </summary>
        Wrap,

        /// <summary>
        /// Arranges document tabs in one row and displays buttons for horizontal scrolling.
        /// </summary>
        Scroll
    }

    /// <summary>
    /// Displays an observable collection of closable, reorderable documents as tabs.
    /// </summary>
    [DefaultEvent(nameof(DocumentClosed))]
    [DefaultProperty(nameof(Documents))]
    [TemplatePart(Name = PartTabScrollViewer, Type = typeof(ScrollViewer))]
    public class DocumentContainer : TabControl
    {
        private const string DocumentDragDataFormat = "Mosaic.UI.Wpf.Controls.DocumentContainer.Document";
        private const string PartTabScrollViewer = "PART_TabScrollViewer";

        /// <summary>
        /// Identifies the command that requests that a document be closed.
        /// </summary>
        public static readonly RoutedUICommand CloseDocumentCommand = new(
            "Close document",
            nameof(CloseDocumentCommand),
            typeof(DocumentContainer),
            [new KeyGesture(Key.F4, ModifierKeys.Control)]);

        /// <summary>
        /// Identifies the command that scrolls the document tabs to the left.
        /// </summary>
        public static readonly RoutedUICommand ScrollTabsLeftCommand = new(
            "Scroll document tabs left",
            nameof(ScrollTabsLeftCommand),
            typeof(DocumentContainer));

        /// <summary>
        /// Identifies the command that scrolls the document tabs to the right.
        /// </summary>
        public static readonly RoutedUICommand ScrollTabsRightCommand = new(
            "Scroll document tabs right",
            nameof(ScrollTabsRightCommand),
            typeof(DocumentContainer));

        /// <summary>
        /// Identifies the <see cref="Documents"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocumentsProperty = DependencyProperty.Register(
            nameof(Documents),
            typeof(ObservableCollection<Document>),
            typeof(DocumentContainer),
            new PropertyMetadata(null, OnDocumentsChanged));

        /// <summary>
        /// Identifies the <see cref="ActiveDocument"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveDocumentProperty = DependencyProperty.Register(
            nameof(ActiveDocument),
            typeof(Document),
            typeof(DocumentContainer),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnActiveDocumentChanged));

        /// <summary>
        /// Identifies the <see cref="HeaderContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(DocumentContainer),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HeaderContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentTemplateProperty = DependencyProperty.Register(
            nameof(HeaderContentTemplate),
            typeof(DataTemplate),
            typeof(DocumentContainer),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HeaderContentTemplateSelector"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentTemplateSelectorProperty = DependencyProperty.Register(
            nameof(HeaderContentTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(DocumentContainer),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="DocumentClosingCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocumentClosingCommandProperty = DependencyProperty.Register(
            nameof(DocumentClosingCommand),
            typeof(ICommand),
            typeof(DocumentContainer),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ActiveTabBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveTabBackgroundProperty = DependencyProperty.Register(
            nameof(ActiveTabBackground),
            typeof(Brush),
            typeof(DocumentContainer),
            new FrameworkPropertyMetadata(CreateFrozenBrush(0x00, 0x7A, 0xCC)));

        /// <summary>
        /// Identifies the <see cref="ActiveTabForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveTabForegroundProperty = DependencyProperty.Register(
            nameof(ActiveTabForeground),
            typeof(Brush),
            typeof(DocumentContainer),
            new FrameworkPropertyMetadata(Brushes.White));

        /// <summary>
        /// Identifies the <see cref="TabOverflowMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TabOverflowModeProperty = DependencyProperty.Register(
            nameof(TabOverflowMode),
            typeof(DocumentTabOverflowMode),
            typeof(DocumentContainer),
            new FrameworkPropertyMetadata(DocumentTabOverflowMode.Wrap, OnTabOverflowModeChanged));

        /// <summary>
        /// Identifies the <see cref="DocumentClosing"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DocumentClosingEvent = EventManager.RegisterRoutedEvent(
            nameof(DocumentClosing),
            RoutingStrategy.Bubble,
            typeof(EventHandler<DocumentClosingEventArgs>),
            typeof(DocumentContainer));

        /// <summary>
        /// Identifies the <see cref="DocumentClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DocumentClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(DocumentClosed),
            RoutingStrategy.Bubble,
            typeof(EventHandler<DocumentClosedEventArgs>),
            typeof(DocumentContainer));

        private bool _isSynchronizingSelection;
        private DocumentTabItem? _dragSourceTab;
        private Point _dragStartPoint;
        private ScrollViewer? _tabScrollViewer;

        static DocumentContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DocumentContainer),
                new FrameworkPropertyMetadata(typeof(DocumentContainer)));
            EnableDragAndDropReorderingProperty.OverrideMetadata(
                typeof(DocumentContainer),
                new FrameworkPropertyMetadata(false, OnEnableDragAndDropReorderingChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentContainer"/> class.
        /// </summary>
        public DocumentContainer()
        {
            SetCurrentValue(DocumentsProperty, new ObservableCollection<Document>());
            SetCurrentValue(EnableDragAndDropReorderingProperty, true);
            AllowDrop = true;

            CommandBindings.Add(new CommandBinding(
                CloseDocumentCommand,
                OnCloseDocumentExecuted,
                OnCloseDocumentCanExecute));
            CommandBindings.Add(new CommandBinding(
                ScrollTabsLeftCommand,
                OnScrollTabsLeftExecuted,
                OnScrollTabsLeftCanExecute));
            CommandBindings.Add(new CommandBinding(
                ScrollTabsRightCommand,
                OnScrollTabsRightExecuted,
                OnScrollTabsRightCanExecute));
        }

        /// <summary>
        /// Gets or sets the collection of documents displayed by the control.
        /// </summary>
        /// <value>The observable collection that drives the displayed document tabs.</value>
        [Category("Common")]
        [Description("The observable collection of documents displayed by the control.")]
        public ObservableCollection<Document> Documents
        {
            get => (ObservableCollection<Document>)GetValue(DocumentsProperty);
            set => SetValue(DocumentsProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently selected document.
        /// </summary>
        /// <value>The active document, or <see langword="null"/> when no document is selected.</value>
        [Category("Common")]
        [Description("The currently selected document.")]
        public Document? ActiveDocument
        {
            get => (Document?)GetValue(ActiveDocumentProperty);
            set => SetValue(ActiveDocumentProperty, value);
        }

        /// <summary>
        /// Gets or sets the content displayed at the right side of the tab strip.
        /// </summary>
        /// <value>The custom content displayed in the shared document header area.</value>
        [Category("Content")]
        [Description("Content displayed at the right side of the tab strip.")]
        public object? HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display <see cref="HeaderContent"/>.
        /// </summary>
        /// <value>The template used to render the shared header content.</value>
        [Category("Content")]
        [Description("The data template used to display the shared header content.")]
        public DataTemplate? HeaderContentTemplate
        {
            get => (DataTemplate?)GetValue(HeaderContentTemplateProperty);
            set => SetValue(HeaderContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the template selector used to display <see cref="HeaderContent"/>.
        /// </summary>
        /// <value>The selector used to choose a template for the shared header content.</value>
        [Category("Content")]
        [Description("The template selector used to display the shared header content.")]
        public DataTemplateSelector? HeaderContentTemplateSelector
        {
            get => (DataTemplateSelector?)GetValue(HeaderContentTemplateSelectorProperty);
            set => SetValue(HeaderContentTemplateSelectorProperty, value);
        }

        /// <summary>
        /// Gets or sets the command invoked before a document is removed.
        /// </summary>
        /// <value>The command that receives the document being closed as its parameter.</value>
        [Category("Action")]
        [Description("The command invoked before a document is removed.")]
        public ICommand? DocumentClosingCommand
        {
            get => (ICommand?)GetValue(DocumentClosingCommandProperty);
            set => SetValue(DocumentClosingCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush used by the active document tab.
        /// </summary>
        /// <value>The active tab background brush. The default color is <c>#007ACC</c>.</value>
        [Category("Brushes")]
        [Description("The background brush used by the active document tab.")]
        public Brush ActiveTabBackground
        {
            get => (Brush)GetValue(ActiveTabBackgroundProperty);
            set => SetValue(ActiveTabBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush used by the active document tab.
        /// </summary>
        /// <value>The active tab foreground brush. The default color is white.</value>
        [Category("Brushes")]
        [Description("The foreground brush used by the active document tab.")]
        public Brush ActiveTabForeground
        {
            get => (Brush)GetValue(ActiveTabForegroundProperty);
            set => SetValue(ActiveTabForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets how document tabs are arranged when they exceed the available width.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the overflow behavior.
        /// The default is <see cref="DocumentTabOverflowMode.Wrap"/>.
        /// </value>
        [Category("Layout")]
        [Description("Specifies whether overflowing document tabs wrap or scroll.")]
        public DocumentTabOverflowMode TabOverflowMode
        {
            get => (DocumentTabOverflowMode)GetValue(TabOverflowModeProperty);
            set => SetValue(TabOverflowModeProperty, value);
        }

        /// <summary>
        /// Occurs before a document is closed and allows the operation to be canceled.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised before a document is closed.")]
        public event EventHandler<DocumentClosingEventArgs> DocumentClosing
        {
            add => AddHandler(DocumentClosingEvent, value);
            remove => RemoveHandler(DocumentClosingEvent, value);
        }

        /// <summary>
        /// Occurs after a document has been removed from the collection.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised after a document has been closed.")]
        public event EventHandler<DocumentClosedEventArgs> DocumentClosed
        {
            add => AddHandler(DocumentClosedEvent, value);
            remove => RemoveHandler(DocumentClosedEvent, value);
        }

        /// <summary>
        /// Attempts to close the specified document using the standard closing pipeline.
        /// </summary>
        /// <param name="document">The document to close.</param>
        /// <returns>
        /// <see langword="true"/> if the document was removed; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryCloseDocument(Document document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var documents = Documents;
            var originalIndex = documents.IndexOf(document);
            if (originalIndex < 0 || !document.CanClose)
            {
                return false;
            }

            var wasActive = ReferenceEquals(ActiveDocument, document);
            var closingArgs = new DocumentClosingEventArgs(DocumentClosingEvent, this, document);
            RaiseEvent(closingArgs);
            if (closingArgs.Cancel)
            {
                return false;
            }

            var command = DocumentClosingCommand;
            if (command != null)
            {
                if (!command.CanExecute(document))
                {
                    return false;
                }

                command.Execute(document);
            }

            if (documents.Contains(document))
            {
                documents.Remove(document);
            }

            if (documents.Contains(document))
            {
                return false;
            }

            if (wasActive)
            {
                var nextIndex = Math.Min(originalIndex, documents.Count - 1);
                SetCurrentValue(
                    ActiveDocumentProperty,
                    nextIndex >= 0 ? documents[nextIndex] : null);
            }

            RaiseEvent(new DocumentClosedEventArgs(DocumentClosedEvent, this, document));
            return true;
        }

        /// <inheritdoc/>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DocumentTabItem;
        }

        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DocumentTabItem();
        }

        /// <inheritdoc/>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is not DocumentTabItem tabItem || item is not Document document)
            {
                return;
            }

            tabItem.Document = document;
            tabItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            tabItem.VerticalContentAlignment = VerticalAlignment.Stretch;

            BindingOperations.SetBinding(
                tabItem,
                HeaderedContentControl.HeaderProperty,
                new Binding(nameof(Document.Title)) { Source = document });
            BindingOperations.SetBinding(
                tabItem,
                ContentControl.ContentProperty,
                new Binding(nameof(Document.Content)) { Source = document });
            BindingOperations.SetBinding(
                tabItem,
                FrameworkElement.ToolTipProperty,
                new Binding(nameof(Document.ToolTip)) { Source = document });
            BindingOperations.SetBinding(
                tabItem,
                DocumentTabItem.CanCloseProperty,
                new Binding(nameof(Document.CanClose)) { Source = document });
        }

        /// <inheritdoc/>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is DocumentTabItem tabItem)
            {
                BindingOperations.ClearBinding(tabItem, HeaderedContentControl.HeaderProperty);
                BindingOperations.ClearBinding(tabItem, ContentControl.ContentProperty);
                BindingOperations.ClearBinding(tabItem, FrameworkElement.ToolTipProperty);
                BindingOperations.ClearBinding(tabItem, DocumentTabItem.CanCloseProperty);
                tabItem.ClearValue(DocumentTabItem.DocumentProperty);
            }

            base.ClearContainerForItemOverride(element, item);
        }

        /// <inheritdoc/>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_isSynchronizingSelection)
            {
                return;
            }

            _isSynchronizingSelection = true;
            SetCurrentValue(ActiveDocumentProperty, SelectedItem as Document);
            _isSynchronizingSelection = false;

            if (TabOverflowMode == DocumentTabOverflowMode.Scroll && SelectedItem != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    if (ItemContainerGenerator.ContainerFromItem(SelectedItem) is FrameworkElement container)
                    {
                        container.BringIntoView();
                    }
                });
            }
        }

        /// <inheritdoc/>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            _dragSourceTab = null;
            if (!EnableDragAndDropReordering
                || e.OriginalSource is not DependencyObject source
                || FindVisualAncestor<ButtonBase>(source) != null)
            {
                return;
            }

            _dragSourceTab = FindVisualAncestor<DocumentTabItem>(source);
            if (_dragSourceTab?.Document != null)
            {
                _dragStartPoint = e.GetPosition(this);
            }
        }

        /// <inheritdoc/>
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (!EnableDragAndDropReordering
                || e.LeftButton != MouseButtonState.Pressed
                || _dragSourceTab?.Document is not Document document)
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var data = new DataObject(DocumentDragDataFormat, document);
            var dragSource = _dragSourceTab;
            _dragSourceTab = null;
            DragDrop.DoDragDrop(dragSource, data, DragDropEffects.Move);
            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.Effects = CanReorderDraggedDocument(e.Data)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (!CanReorderDraggedDocument(e.Data)
                || e.Data.GetData(DocumentDragDataFormat) is not Document document)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var oldIndex = Documents.IndexOf(document);
            var insertionIndex = Documents.Count;
            if (e.OriginalSource is DependencyObject source
                && FindVisualAncestor<DocumentTabItem>(source) is { Document: { } targetDocument } targetTab)
            {
                insertionIndex = Documents.IndexOf(targetDocument);
                if (insertionIndex >= 0 && e.GetPosition(targetTab).X >= targetTab.ActualWidth / 2)
                {
                    insertionIndex++;
                }
            }

            if (oldIndex < insertionIndex)
            {
                insertionIndex--;
            }

            var newIndex = Math.Clamp(insertionIndex, 0, Documents.Count - 1);
            if (newIndex != oldIndex)
            {
                Documents.Move(oldIndex, newIndex);
            }

            SetCurrentValue(ActiveDocumentProperty, document);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            if (_tabScrollViewer != null)
            {
                _tabScrollViewer.ScrollChanged -= OnTabScrollViewerScrollChanged;
            }

            base.OnApplyTemplate();

            _tabScrollViewer = GetTemplateChild(PartTabScrollViewer) as ScrollViewer;
            if (_tabScrollViewer != null)
            {
                _tabScrollViewer.ScrollChanged += OnTabScrollViewerScrollChanged;
            }
        }

        /// <inheritdoc/>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TabControlAutomationPeer(this);
        }

        private static void OnDocumentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var container = (DocumentContainer)d;

            if (e.OldValue is ObservableCollection<Document> oldDocuments)
            {
                oldDocuments.CollectionChanged -= container.OnDocumentsCollectionChanged;
            }

            var documents = e.NewValue as ObservableCollection<Document> ?? new ObservableCollection<Document>();
            if (!ReferenceEquals(e.NewValue, documents))
            {
                container.SetCurrentValue(DocumentsProperty, documents);
                return;
            }

            documents.CollectionChanged += container.OnDocumentsCollectionChanged;
            container.SetCurrentValue(ItemsSourceProperty, documents);

            if (container.ActiveDocument != null && !documents.Contains(container.ActiveDocument))
            {
                container.SetCurrentValue(ActiveDocumentProperty, null);
            }
            else if (container.ActiveDocument != null)
            {
                container.SetCurrentValue(SelectedItemProperty, container.ActiveDocument);
            }
        }

        private static void OnActiveDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var container = (DocumentContainer)d;
            if (container._isSynchronizingSelection)
            {
                return;
            }

            var document = e.NewValue as Document;
            if (document != null && !container.Documents.Contains(document))
            {
                container.SetCurrentValue(ActiveDocumentProperty, container.SelectedItem as Document);
                return;
            }

            container._isSynchronizingSelection = true;
            container.SetCurrentValue(SelectedItemProperty, document);
            container._isSynchronizingSelection = false;
        }

        private static void OnTabOverflowModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var container = (DocumentContainer)d;
            container.Dispatcher.BeginInvoke(() =>
            {
                if (container.TabOverflowMode == DocumentTabOverflowMode.Wrap)
                {
                    container._tabScrollViewer?.ScrollToLeftEnd();
                }
                else if (container.SelectedItem != null
                    && container.ItemContainerGenerator.ContainerFromItem(container.SelectedItem) is FrameworkElement selectedContainer)
                {
                    selectedContainer.BringIntoView();
                }

                CommandManager.InvalidateRequerySuggested();
            });
        }

        private static void OnEnableDragAndDropReorderingChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is not DocumentContainer container)
            {
                return;
            }

            // DocumentContainer handles wrapped and scrolling tab layouts itself.
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(container, false);
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(container, false);
            container.AllowDrop = (bool)e.NewValue;
        }

        private bool CanReorderDraggedDocument(IDataObject data)
        {
            return EnableDragAndDropReordering
                && data.GetDataPresent(DocumentDragDataFormat)
                && data.GetData(DocumentDragDataFormat) is Document document
                && Documents.Contains(document);
        }

        private static T? FindVisualAncestor<T>(DependencyObject source)
            where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = current switch
                {
                    Visual => VisualTreeHelper.GetParent(current),
                    ContentElement contentElement => ContentOperations.GetParent(contentElement)
                        ?? (contentElement as FrameworkContentElement)?.Parent,
                    _ => LogicalTreeHelper.GetParent(current)
                };
            }

            return null;
        }

        private void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ActiveDocument != null && !Documents.Contains(ActiveDocument))
            {
                var fallbackIndex = e.OldStartingIndex >= 0
                    ? Math.Min(e.OldStartingIndex, Documents.Count - 1)
                    : 0;
                SetCurrentValue(
                    ActiveDocumentProperty,
                    Documents.Count > 0 ? Documents[fallbackIndex] : null);
            }
        }

        private void OnCloseDocumentCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var document = e.Parameter as Document ?? ActiveDocument;
            e.CanExecute = document?.CanClose == true && Documents.Contains(document);
            e.Handled = true;
        }

        private void OnCloseDocumentExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var document = e.Parameter as Document ?? ActiveDocument;
            if (document != null)
            {
                TryCloseDocument(document);
            }

            e.Handled = true;
        }

        private static Brush CreateFrozenBrush(byte red, byte green, byte blue)
        {
            var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }

        private void OnScrollTabsLeftCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TabOverflowMode == DocumentTabOverflowMode.Scroll
                && _tabScrollViewer?.HorizontalOffset > 0;
            e.Handled = true;
        }

        private void OnScrollTabsLeftExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _tabScrollViewer?.LineLeft();
            e.Handled = true;
        }

        private void OnScrollTabsRightCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TabOverflowMode == DocumentTabOverflowMode.Scroll
                && _tabScrollViewer != null
                && _tabScrollViewer.HorizontalOffset < _tabScrollViewer.ScrollableWidth;
            e.Handled = true;
        }

        private void OnScrollTabsRightExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _tabScrollViewer?.LineRight();
            e.Handled = true;
        }

        private void OnTabScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
