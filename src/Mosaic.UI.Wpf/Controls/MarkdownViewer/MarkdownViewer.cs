/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Documents;
using DocHyperlink = System.Windows.Documents.Hyperlink;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A lookless, WPF-native Markdown viewer. Markdown supplied via the <see cref="Markdown"/>
    /// property is rendered into a <see cref="FlowDocument"/> and hosted in a read-only
    /// <see cref="RichTextBox"/>, so the formatted content can be selected and copied as rich text
    /// instead of raw Markdown syntax. Rendering is defensive: invalid Markdown never throws, and a
    /// failed render falls back to displaying the original text as plain text.
    /// </summary>
    /// <remarks>
    /// Multi-line code blocks are hosted in a read-only <see cref="SyntaxEditor"/> that stretches to
    /// the full height of its content, so the code is syntax highlighted when the fence names a
    /// language the editor supports (for example <c>```csharp</c> or <c>``` csharp</c>) and shown as
    /// plain text when it does not. Because the embedded editor is sized to its document it never
    /// scrolls on its own and leaves the viewer's scrolling intact.
    /// <para>
    /// <c>Ctrl+F</c> opens a find bar modeled on the <see cref="SyntaxEditor"/> search panel. The
    /// gesture is scoped to the focused control: pressed inside a code block it opens that editor's
    /// own search panel, and pressed anywhere else it opens the viewer's find bar, which searches
    /// the document text and the text of every embedded code block editor.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartRichTextBox, Type = typeof(RichTextBox))]
    [TemplatePart(Name = PartFindPanel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartFindTextBox, Type = typeof(TextBox))]
    public class MarkdownViewer : Control
    {
        /// <summary>
        /// The smallest base font size reachable with Ctrl+mouse-wheel zoom.
        /// </summary>
        private const double MinimumZoomFontSize = 8;

        /// <summary>
        /// The largest base font size reachable with Ctrl+mouse-wheel zoom.
        /// </summary>
        private const double MaximumZoomFontSize = 32;

        /// <summary>
        /// The name of the <see cref="RichTextBox"/> template part that hosts the rendered document.
        /// </summary>
        private const string PartRichTextBox = "PART_RichTextBox";

        /// <summary>
        /// The name of the find bar template part.
        /// </summary>
        private const string PartFindPanel = "PART_FindPanel";

        /// <summary>
        /// The name of the find bar's search text box template part.
        /// </summary>
        private const string PartFindTextBox = "PART_FindTextBox";

        /// <summary>
        /// The wash painted behind every match in the document. A translucent amber reads on both
        /// the light and dark themes without hiding the text beneath it.
        /// </summary>
        private static readonly Brush FindHighlightBrush = CreateFrozenBrush(0x4D, 0xF4, 0xB4, 0x00);

        /// <summary>
        /// The wash painted behind the selected match. A deeper, more opaque orange separates it
        /// from the other matches, which the document selection cannot do on its own: while the
        /// find bar holds focus that selection is drawn with the pale inactive highlight brush and
        /// disappears against the other matches.
        /// </summary>
        private static readonly Brush FindCurrentHighlightBrush = CreateFrozenBrush(0xCC, 0xFF, 0x8C, 0x00);

        /// <summary>
        /// The hosting rich text box, resolved from the template.
        /// </summary>
        private RichTextBox? _richTextBox;

        /// <summary>
        /// The find bar's search text box, resolved from the template.
        /// </summary>
        private TextBox? _findTextBox;

        /// <summary>
        /// The matches for the current find text, in document order.
        /// </summary>
        private List<MarkdownSearchMatch> _matches = new();

        /// <summary>
        /// The ranges currently painted with <see cref="FindHighlightBrush"/>.
        /// </summary>
        private readonly List<TextRange> _highlightedRanges = new();

        /// <summary>
        /// The index of the selected match within <see cref="_matches"/>, or <c>-1</c> when none is
        /// selected.
        /// </summary>
        private int _currentMatchIndex = -1;

        /// <summary>
        /// The range currently painted with <see cref="FindCurrentHighlightBrush"/>.
        /// </summary>
        private TextRange? _currentHighlightRange;

        /// <summary>
        /// The absolute URI of the document currently loaded via <see cref="Source"/>, used to
        /// resolve relative links and images. <c>null</c> when the Markdown was supplied directly.
        /// </summary>
        private Uri? _resolvedSource;

        /// <summary>
        /// The absolute base URI derived from <see cref="StorageFolder"/> (with a trailing separator),
        /// used to resolve relative images and links when no <see cref="Source"/> is set. <c>null</c>
        /// when no storage folder is configured.
        /// </summary>
        private Uri? _storageFolderUri;

        /// <summary>
        /// Previously displayed documents, most recent last, used by <see cref="GoBack"/>.
        /// </summary>
        private readonly Stack<Uri> _backStack = new();

        /// <summary>
        /// Suppresses history bookkeeping while <see cref="GoBack"/> changes <see cref="Source"/>.
        /// </summary>
        private bool _suppressHistory;

        /// <summary>
        /// Distinguishes <see cref="Markdown"/> changes made while loading a <see cref="Source"/>
        /// document from changes made directly by the consumer.
        /// </summary>
        private bool _settingMarkdownFromSource;

        /// <summary>
        /// Initializes static metadata for the <see cref="MarkdownViewer"/> class.
        /// </summary>
        static MarkdownViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer), new FrameworkPropertyMetadata(typeof(MarkdownViewer)));
            FontSizeProperty.OverrideMetadata(
                typeof(MarkdownViewer),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontSize,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnFontSizeChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownViewer"/> class and wires the find
        /// bar's commands and keyboard gestures.
        /// </summary>
        public MarkdownViewer()
        {
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, this.OnFindCommandExecuted));
            this.CommandBindings.Add(new CommandBinding(FindNextCommand, (_, e) => { this.FindNext(true); e.Handled = true; }, this.OnFindNavigationCanExecute));
            this.CommandBindings.Add(new CommandBinding(FindPreviousCommand, (_, e) => { this.FindNext(false); e.Handled = true; }, this.OnFindNavigationCanExecute));
            this.CommandBindings.Add(new CommandBinding(CloseFindPanelCommand, (_, e) => { this.CloseFindPanel(); e.Handled = true; }));

            // These bindings are only reached when the focused element did not already claim the
            // gesture, which is what scopes Ctrl+F to a code block's own search panel when the
            // caret is inside one.
            this.InputBindings.Add(new KeyBinding(ApplicationCommands.Find, new KeyGesture(Key.F, ModifierKeys.Control)));
            this.InputBindings.Add(new KeyBinding(FindNextCommand, new KeyGesture(Key.F3)));
            this.InputBindings.Add(new KeyBinding(FindPreviousCommand, new KeyGesture(Key.F3, ModifierKeys.Shift)));
        }

        /// <summary>
        /// Selects the next match in the find bar.
        /// </summary>
        public static readonly RoutedUICommand FindNextCommand = new("Find Next", nameof(FindNextCommand), typeof(MarkdownViewer));

        /// <summary>
        /// Selects the previous match in the find bar.
        /// </summary>
        public static readonly RoutedUICommand FindPreviousCommand = new("Find Previous", nameof(FindPreviousCommand), typeof(MarkdownViewer));

        /// <summary>
        /// Closes the find bar and clears its highlights.
        /// </summary>
        public static readonly RoutedUICommand CloseFindPanelCommand = new("Close", nameof(CloseFindPanelCommand), typeof(MarkdownViewer));

        /// <summary>
        /// Identifies the <see cref="Markdown"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, OnMarkdownChanged));

        /// <summary>
        /// Gets or sets the Markdown text to render. A <c>null</c> value is treated as an empty string.
        /// </summary>
        [Category("Common")]
        [Description("The Markdown text to render as formatted, copyable rich text.")]
        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(Uri),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnSourceChanged));

        /// <summary>
        /// Gets or sets the URI of a Markdown document to load and display. Supports application
        /// resource URIs for files built with the <c>Resource</c> build action (relative form
        /// <c>/AssemblyName;component/Docs/index.md</c> or the absolute <c>pack://</c> form) as well
        /// as local file URIs. Relative links inside the loaded document that point at other
        /// Markdown resources navigate the viewer in place; use <see cref="GoBack"/> to return.
        /// </summary>
        [Category("Common")]
        [Description("The URI of a Markdown document to load, such as a pack resource URI.")]
        public Uri? Source
        {
            get => (Uri?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StorageFolder"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StorageFolderProperty = DependencyProperty.Register(
            nameof(StorageFolder),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnStorageFolderChanged));

        /// <summary>
        /// Gets or sets a folder used as the base for resolving relative image and link URLs when the
        /// Markdown is supplied directly through <see cref="Markdown"/> rather than loaded from a
        /// <see cref="Source"/>. This mirrors the <c>MarkdownEditor.StorageFolder</c> property: an image
        /// inserted by the editor as a relative <c>{Guid}.{extension}</c> link resolves against this
        /// folder here. When <see cref="Source"/> is set, that document's location takes precedence.
        /// </summary>
        [Category("Common")]
        [Description("A folder used as the base for resolving relative image and link URLs in directly-supplied Markdown.")]
        public string? StorageFolder
        {
            get => (string?)GetValue(StorageFolderProperty);
            set => SetValue(StorageFolderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeadingBottomSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeadingBottomSpacingProperty = DependencyProperty.Register(
            nameof(HeadingBottomSpacing),
            typeof(double),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(MarkdownFlowDocumentRenderer.DefaultHeadingBottomSpacing, OnHeadingBottomSpacingChanged),
            IsValidHeadingBottomSpacing);

        /// <summary>
        /// Gets or sets the space, in device-independent pixels, left below each rendered heading
        /// before the following content. The default is
        /// <see cref="MarkdownFlowDocumentRenderer.DefaultHeadingBottomSpacing"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The space, in device-independent pixels, left below each rendered heading.")]
        public double HeadingBottomSpacing
        {
            get => (double)GetValue(HeadingBottomSpacingProperty);
            set => SetValue(HeadingBottomSpacingProperty, value);
        }

        /// <summary>
        /// Identifies the read-only <see cref="CanGoBack"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey CanGoBackPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CanGoBack),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="CanGoBack"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether there is a previous document to navigate back to.
        /// </summary>
        [Category("Common")]
        [Description("Whether there is a previous document to navigate back to.")]
        public bool CanGoBack => (bool)GetValue(CanGoBackProperty);

        /// <summary>
        /// Identifies the <see cref="LinkClicked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent LinkClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(LinkClicked),
            RoutingStrategy.Bubble,
            typeof(MarkdownLinkClickedEventHandler),
            typeof(MarkdownViewer));

        /// <summary>
        /// Occurs when the user clicks a hyperlink in the rendered document, before the viewer's
        /// default navigation runs. Mark the event as handled to take over navigation, for example
        /// to route a custom scheme such as <c>app:settings</c> to a page within the application.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when a hyperlink is clicked, before the default navigation runs.")]
        public event MarkdownLinkClickedEventHandler LinkClicked
        {
            add => AddHandler(LinkClickedEvent, value);
            remove => RemoveHandler(LinkClickedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="EventRaised"/> routed event.
        /// </summary>
        public static readonly RoutedEvent EventRaisedEvent = EventManager.RegisterRoutedEvent(
            nameof(EventRaised),
            RoutingStrategy.Bubble,
            typeof(MarkdownEventRaisedEventHandler),
            typeof(MarkdownViewer));

        /// <summary>
        /// Occurs when the user clicks an event link: a Markdown link whose destination starts with
        /// <c>@</c>, such as <c>[Blake's Articles](@ShowArticle?keyword=bpell)</c>. The event data
        /// carries the event name (<c>ShowArticle</c>) and the URL-decoded query-string parameters
        /// (<c>keyword</c> = <c>bpell</c>). Event links never navigate and do not raise
        /// <see cref="LinkClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when an @-prefixed event link is clicked, carrying the event name and query-string parameters.")]
        public event MarkdownEventRaisedEventHandler EventRaised
        {
            add => AddHandler(EventRaisedEvent, value);
            remove => RemoveHandler(EventRaisedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsCopyEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCopyEnabledProperty = DependencyProperty.Register(
            nameof(IsCopyEnabled),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(true, OnIsCopyEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the rendered text can be selected and copied.
        /// When <c>false</c>, the document is display-only and cannot be selected. Defaults to <c>true</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the rendered text can be selected and copied.")]
        public bool IsCopyEnabled
        {
            get => (bool)GetValue(IsCopyEnabledProperty);
            set => SetValue(IsCopyEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDocumentReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDocumentReadOnlyProperty = DependencyProperty.Register(
            nameof(IsDocumentReadOnly),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(true, OnIsDocumentReadOnlyChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the hosted document is read-only. Defaults to
        /// <c>true</c>; the viewer is intended for display rather than editing.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the hosted document is read-only.")]
        public bool IsDocumentReadOnly
        {
            get => (bool)GetValue(IsDocumentReadOnlyProperty);
            set => SetValue(IsDocumentReadOnlyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsFindPanelOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFindPanelOpenProperty = DependencyProperty.Register(
            nameof(IsFindPanelOpen),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, OnIsFindPanelOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the find bar is shown. Setting this to
        /// <c>true</c> is equivalent to pressing <c>Ctrl+F</c> over the document.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the find bar is shown.")]
        public bool IsFindPanelOpen
        {
            get => (bool)GetValue(IsFindPanelOpenProperty);
            set => SetValue(IsFindPanelOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FindText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FindTextProperty = DependencyProperty.Register(
            nameof(FindText),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFindOptionChanged));

        /// <summary>
        /// Gets or sets the text the find bar searches for.
        /// </summary>
        [Category("Behavior")]
        [Description("The text the find bar searches for.")]
        public string FindText
        {
            get => (string)GetValue(FindTextProperty);
            set => SetValue(FindTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FindMatchCase"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FindMatchCaseProperty = DependencyProperty.Register(
            nameof(FindMatchCase),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFindOptionChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the find bar's search is case sensitive.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the find bar's search is case sensitive.")]
        public bool FindMatchCase
        {
            get => (bool)GetValue(FindMatchCaseProperty);
            set => SetValue(FindMatchCaseProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FindWholeWords"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FindWholeWordsProperty = DependencyProperty.Register(
            nameof(FindWholeWords),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFindOptionChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the find bar matches whole words only.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the find bar matches whole words only.")]
        public bool FindWholeWords
        {
            get => (bool)GetValue(FindWholeWordsProperty);
            set => SetValue(FindWholeWordsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FindUseRegex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FindUseRegexProperty = DependencyProperty.Register(
            nameof(FindUseRegex),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFindOptionChanged));

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="FindText"/> is a regular expression.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the find text is treated as a regular expression.")]
        public bool FindUseRegex
        {
            get => (bool)GetValue(FindUseRegexProperty);
            set => SetValue(FindUseRegexProperty, value);
        }

        /// <summary>
        /// Identifies the read-only <see cref="FindStatusText"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey FindStatusTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FindStatusText),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="FindStatusText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FindStatusTextProperty = FindStatusTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the find bar's result summary, such as <c>3 of 12</c> or <c>No results</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("The find bar's result summary.")]
        public string FindStatusText => (string)GetValue(FindStatusTextProperty);

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _richTextBox = GetTemplateChild(PartRichTextBox) as RichTextBox;

            if (_richTextBox != null)
            {
                _richTextBox.IsReadOnly = IsDocumentReadOnly;
                _richTextBox.IsHitTestVisible = IsCopyEnabled;
                _richTextBox.Focusable = IsCopyEnabled;

                // Elements embedded in a rich text box are disabled unless the document is enabled,
                // which would leave the code blocks' editors unselectable, without their context menu,
                // and painted with the theme's disabled foreground.
                _richTextBox.IsDocumentEnabled = true;

                // The find bar takes focus while it is open, so the selected match has to stay
                // visible even though the document itself is no longer focused.
                _richTextBox.IsInactiveSelectionHighlightEnabled = true;

                // A RichTextBox's text editor intercepts mouse clicks for selection before a
                // Hyperlink can raise RequestNavigate (even when read-only), so links are opened by
                // hit-testing the click position for a hyperlink instead.
                _richTextBox.PreviewMouseLeftButtonDown -= OnRichTextBoxPreviewMouseLeftButtonDown;
                _richTextBox.PreviewMouseLeftButtonDown += OnRichTextBoxPreviewMouseLeftButtonDown;

                // The text editor also wins the cursor negotiation and forces an I-beam over the
                // whole document, so the hand cursor over a link is applied here for the same reason.
                _richTextBox.QueryCursor -= OnRichTextBoxQueryCursor;
                _richTextBox.QueryCursor += OnRichTextBoxQueryCursor;
            }

            _findTextBox = GetTemplateChild(PartFindTextBox) as TextBox;

            if (_findTextBox != null)
            {
                _findTextBox.PreviewKeyDown -= OnFindTextBoxPreviewKeyDown;
                _findTextBox.PreviewKeyDown += OnFindTextBoxPreviewKeyDown;
            }

            RenderMarkdown(Markdown);
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control || e.Delta == 0)
            {
                return;
            }

            double oldFontSize = FontSize;
            double newFontSize = Math.Clamp(
                oldFontSize + Math.Sign(e.Delta),
                MinimumZoomFontSize,
                MaximumZoomFontSize);

            if (!newFontSize.Equals(oldFontSize))
            {
                SetCurrentValue(FontSizeProperty, newFontSize);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Scales the rendered document when the viewer's base font size changes through zoom,
        /// binding, a style, or direct property assignment.
        /// </summary>
        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if (viewer._richTextBox?.Document is not { } document ||
                e.OldValue is not double oldFontSize ||
                e.NewValue is not double newFontSize ||
                oldFontSize <= 0 ||
                newFontSize <= 0 ||
                oldFontSize.Equals(newFontSize))
            {
                return;
            }

            ScaleDocumentFontSizes(document, newFontSize / oldFontSize);
        }

        /// <summary>
        /// Scales every locally assigned font size in a rendered document. This includes the
        /// document's base size and explicit Markdown heading sizes, preserving their proportions
        /// as the user zooms.
        /// </summary>
        private static void ScaleDocumentFontSizes(DependencyObject element, double scale)
        {
            object localFontSize = element.ReadLocalValue(TextElement.FontSizeProperty);

            if (localFontSize is double fontSize)
            {
                element.SetValue(TextElement.FontSizeProperty, fontSize * scale);
            }

            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject dependencyObject)
                {
                    ScaleDocumentFontSizes(dependencyObject, scale);
                }
            }
        }

        /// <summary>
        /// Navigates a hyperlink when the user clicks it, hit-testing the click position so a
        /// single click works inside the read-only <see cref="RichTextBox"/>.
        /// </summary>
        private void OnRichTextBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_richTextBox == null)
            {
                return;
            }

            var position = _richTextBox.GetPositionFromPoint(e.GetPosition(_richTextBox), false);

            if (position == null)
            {
                return;
            }

            var hyperlink = FindHyperlink(position.Parent);

            if (hyperlink?.NavigateUri == null)
            {
                return;
            }

            e.Handled = true;
            NavigateTo(hyperlink.NavigateUri);
        }

        /// <summary>
        /// Shows the hand cursor while the pointer is over a hyperlink, overriding the I-beam the
        /// <see cref="RichTextBox"/> text editor would otherwise show across the whole document.
        /// </summary>
        private void OnRichTextBoxQueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (_richTextBox == null)
            {
                return;
            }

            var position = _richTextBox.GetPositionFromPoint(e.GetPosition(_richTextBox), false);

            if (position == null || FindHyperlink(position.Parent) == null)
            {
                return;
            }

            e.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        /// <summary>
        /// Handles a clicked link: raises <see cref="LinkClicked"/> first so the application can
        /// intercept it, then either navigates the viewer to another Markdown document (pack
        /// resource or local <c>.md</c> file) or opens the link with the system default handler.
        /// </summary>
        /// <param name="uri">The link target; may be relative.</param>
        private void NavigateTo(Uri uri)
        {
            // Event links (@Name?key=value) are application callbacks rather than navigation
            // targets: raise EventRaised with the parsed name and parameters and stop there.
            if (!uri.IsAbsoluteUri &&
                MarkdownEventRaisedEventArgs.TryParse(uri.OriginalString, out var eventName, out var parameters))
            {
                RaiseEvent(new MarkdownEventRaisedEventArgs(EventRaisedEvent, this, eventName, parameters, uri.OriginalString));
                return;
            }

            var args = new MarkdownLinkClickedEventArgs(LinkClickedEvent, this, uri);
            RaiseEvent(args);

            if (args.Handled)
            {
                return;
            }

            var resolved = uri;

            if (!resolved.IsAbsoluteUri)
            {
                // In-page anchors are not supported; ignore them rather than failing.
                if (resolved.OriginalString.StartsWith("#", StringComparison.Ordinal))
                {
                    return;
                }

                if (_resolvedSource == null || !Uri.TryCreate(_resolvedSource, resolved.OriginalString, out resolved!))
                {
                    return;
                }
            }

            // Markdown documents inside the application (or on disk) are shown in place; everything
            // else is delegated to the shell so http/https/mailto links open normally.
            bool isMarkdownDocument =
                string.Equals(resolved.Scheme, System.IO.Packaging.PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase) ||
                (resolved.IsFile && string.Equals(Path.GetExtension(resolved.LocalPath), ".md", StringComparison.OrdinalIgnoreCase));

            if (isMarkdownDocument)
            {
                SetCurrentValue(SourceProperty, resolved);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(resolved.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Navigates back to the previously displayed document, if any.
        /// </summary>
        public void GoBack()
        {
            if (_backStack.Count == 0)
            {
                return;
            }

            var target = _backStack.Pop();
            _suppressHistory = true;

            try
            {
                SetCurrentValue(SourceProperty, target);
            }
            finally
            {
                _suppressHistory = false;
            }

            SetValue(CanGoBackPropertyKey, _backStack.Count > 0);
        }

        /// <summary>
        /// Walks the logical tree from the supplied element to locate an enclosing
        /// <see cref="Hyperlink"/>, if any.
        /// </summary>
        private static DocHyperlink? FindHyperlink(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is DocHyperlink hyperlink)
                {
                    return hyperlink;
                }

                element = LogicalTreeHelper.GetParent(element);
            }

            return null;
        }

        /// <summary>
        /// Re-renders the document when the <see cref="Markdown"/> property changes. Markdown set
        /// directly by the consumer has no backing document, so relative links stop resolving until
        /// <see cref="Source"/> is set again.
        /// </summary>
        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if (!viewer._settingMarkdownFromSource)
            {
                viewer._resolvedSource = null;
            }

            viewer.RenderMarkdown(e.NewValue as string ?? string.Empty);
        }

        /// <summary>
        /// Loads the document when the <see cref="Source"/> property changes.
        /// </summary>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MarkdownViewer)d).LoadSource(e.NewValue as Uri);
        }

        /// <summary>
        /// Rebuilds the storage-folder base URI and re-renders when <see cref="StorageFolder"/> changes.
        /// </summary>
        private static void OnStorageFolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;
            viewer._storageFolderUri = BuildStorageFolderUri(e.NewValue as string);
            viewer.RenderMarkdown(viewer.Markdown);
        }

        /// <summary>
        /// Re-renders the document when <see cref="HeadingBottomSpacing"/> changes so the new
        /// spacing applies to headings already on screen.
        /// </summary>
        private static void OnHeadingBottomSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;
            viewer.RenderMarkdown(viewer.Markdown);
        }

        /// <summary>
        /// Validates <see cref="HeadingBottomSpacing"/>: the value must be a finite, non-negative number.
        /// </summary>
        private static bool IsValidHeadingBottomSpacing(object value)
        {
            return value is double d && !double.IsNaN(d) && !double.IsInfinity(d) && d >= 0;
        }

        /// <summary>
        /// Builds an absolute base URI for a storage folder, ensuring a trailing directory separator so
        /// relative file names resolve as children of the folder rather than replacing its last segment.
        /// </summary>
        /// <param name="folder">The storage folder path, or <c>null</c>/empty for none.</param>
        /// <returns>The absolute folder URI, or <c>null</c> when no valid folder was supplied.</returns>
        private static Uri? BuildStorageFolderUri(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            try
            {
                string fullPath = Path.GetFullPath(folder);

                if (!fullPath.EndsWith(Path.DirectorySeparatorChar) &&
                    !fullPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    fullPath += Path.DirectorySeparatorChar;
                }

                return new Uri(fullPath, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Loads and displays the Markdown document identified by <paramref name="uri"/>, recording
        /// the previously displayed document in the back history.
        /// </summary>
        /// <param name="uri">The document to load, or <c>null</c> to clear the viewer.</param>
        private void LoadSource(Uri? uri)
        {
            var previous = _resolvedSource;

            if (uri == null)
            {
                _resolvedSource = null;
                _backStack.Clear();
                SetValue(CanGoBackPropertyKey, false);
                SetMarkdownFromSource(string.Empty);
                return;
            }

            var absolute = ToAbsoluteUri(uri);
            string? text = absolute == null ? null : ReadSourceText(absolute);

            if (!_suppressHistory && previous != null && absolute != null && absolute != previous)
            {
                _backStack.Push(previous);
                SetValue(CanGoBackPropertyKey, true);
            }

            _resolvedSource = absolute;
            SetMarkdownFromSource(text ?? $"# Document not found\n\nThe document `{uri.OriginalString}` could not be loaded.");
        }

        /// <summary>
        /// Sets the <see cref="Markdown"/> property on behalf of <see cref="Source"/> loading,
        /// without clearing the resolved source used for relative link resolution.
        /// </summary>
        /// <param name="text">The Markdown text to display.</param>
        private void SetMarkdownFromSource(string text)
        {
            _settingMarkdownFromSource = true;

            try
            {
                SetCurrentValue(MarkdownProperty, text);
            }
            finally
            {
                _settingMarkdownFromSource = false;
            }
        }

        /// <summary>
        /// Converts a possibly relative source URI to an absolute URI. Relative URIs of the form
        /// <c>/AssemblyName;component/path.md</c> are treated as application pack resources.
        /// </summary>
        /// <param name="uri">The URI to normalize.</param>
        /// <returns>The absolute URI, or <c>null</c> when the URI cannot be resolved.</returns>
        private static Uri? ToAbsoluteUri(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }

            string path = uri.OriginalString;

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                return null;
            }

            // Reading UriSchemePack ensures the pack:// scheme is registered before use.
            string scheme = System.IO.Packaging.PackUriHelper.UriSchemePack;
            return new Uri($"{scheme}://application:,,,{path}", UriKind.Absolute);
        }

        /// <summary>
        /// Reads the Markdown text behind an absolute source URI, supporting local files and
        /// application pack resources (files built with the <c>Resource</c> build action).
        /// </summary>
        /// <param name="uri">The absolute URI to read.</param>
        /// <returns>The document text, or <c>null</c> when it could not be read.</returns>
        private static string? ReadSourceText(Uri uri)
        {
            try
            {
                if (uri.IsFile)
                {
                    return File.Exists(uri.LocalPath) ? File.ReadAllText(uri.LocalPath) : null;
                }

                var resource = Application.GetResourceStream(uri);

                if (resource == null)
                {
                    return null;
                }

                using var reader = new StreamReader(resource.Stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Applies the <see cref="IsCopyEnabled"/> value to the hosting rich text box.
        /// </summary>
        private static void OnIsCopyEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if (viewer._richTextBox != null)
            {
                bool enabled = (bool)e.NewValue;
                viewer._richTextBox.IsHitTestVisible = enabled;
                viewer._richTextBox.Focusable = enabled;
            }
        }

        /// <summary>
        /// Applies the <see cref="IsDocumentReadOnly"/> value to the hosting rich text box.
        /// </summary>
        private static void OnIsDocumentReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if (viewer._richTextBox != null)
            {
                viewer._richTextBox.IsReadOnly = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Renders the supplied Markdown into the hosting rich text box, falling back to plain text
        /// when rendering fails so that invalid Markdown never crashes the application.
        /// </summary>
        /// <param name="markdown">The Markdown text to render.</param>
        private void RenderMarkdown(string markdown)
        {
            if (_richTextBox == null)
            {
                return;
            }

            // The previous document's matches point into content that is about to be replaced.
            _highlightedRanges.Clear();
            _currentHighlightRange = null;
            _matches = new List<MarkdownSearchMatch>();
            _currentMatchIndex = -1;

            FlowDocument document;

            try
            {
                // A loaded Source document's location takes precedence; otherwise fall back to the
                // configured storage folder so relative images/links in directly-supplied Markdown
                // resolve. Images additionally fall back to the storage folder when they are not
                // found next to the source document.
                document = MarkdownFlowDocumentRenderer.Render(
                    markdown,
                    _resolvedSource ?? _storageFolderUri,
                    _storageFolderUri,
                    HeadingBottomSpacing);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                document = CreateFallbackDocument(markdown);
            }

            // Inherit the control's typography and colors so the document honors the active theme.
            document.PagePadding = new Thickness(0);
            document.FontFamily = _richTextBox.FontFamily;
            document.FontSize = _richTextBox.FontSize;
            document.Foreground = _richTextBox.Foreground;

            _richTextBox.Document = document;

            if (IsFindPanelOpen)
            {
                UpdateMatches(true);
            }
        }

        /// <summary>
        /// Opens the find bar in response to <see cref="ApplicationCommands.Find"/>. This binding is
        /// only reached when the focused element did not handle the gesture first, so <c>Ctrl+F</c>
        /// inside a code block opens that editor's own search panel instead.
        /// </summary>
        private void OnFindCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ShowFindPanel();
            e.Handled = true;
        }

        /// <summary>
        /// Reports whether the find bar has matches to step through.
        /// </summary>
        private void OnFindNavigationCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsFindPanelOpen || !string.IsNullOrEmpty(FindText);
        }

        /// <summary>
        /// Opens the find bar, seeds it with the current selection, and focuses its text box.
        /// </summary>
        public void ShowFindPanel()
        {
            // A single-line selection is what the user most likely wants to look for, matching the
            // behavior of the syntax editor's search panel.
            if (_richTextBox is { Selection.IsEmpty: false })
            {
                string selection = _richTextBox.Selection.Text.Trim();

                if (!string.IsNullOrWhiteSpace(selection) && !selection.Contains('\n'))
                {
                    SetCurrentValue(FindTextProperty, selection);
                }
            }

            SetCurrentValue(IsFindPanelOpenProperty, true);

            // The panel is only realized once its visibility flips, so focus is deferred until the
            // layout pass that shows it has run.
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (_findTextBox == null)
                    {
                        return;
                    }

                    _findTextBox.Focus();
                    _findTextBox.SelectAll();
                }),
                DispatcherPriority.Input);
        }

        /// <summary>
        /// Closes the find bar, clears its highlights, and returns focus to the document.
        /// </summary>
        public void CloseFindPanel()
        {
            SetCurrentValue(IsFindPanelOpenProperty, false);
            _richTextBox?.Focus();
        }

        /// <summary>
        /// Clears the find state when the panel closes, and re-runs the search when it opens.
        /// </summary>
        private static void OnIsFindPanelOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if ((bool)e.NewValue)
            {
                viewer.UpdateMatches(true);
                return;
            }

            // The editors are cleared before the match list is dropped, since that list is what
            // names the editors holding a selection.
            viewer.ClearHighlights();
            viewer.ClearEditorSelections();
            viewer._matches = new List<MarkdownSearchMatch>();
            viewer._currentMatchIndex = -1;
            viewer.UpdateFindStatus();
        }

        /// <summary>
        /// Re-runs the search when the find text or one of the find options changes.
        /// </summary>
        private static void OnFindOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;

            if (viewer.IsFindPanelOpen)
            {
                viewer.UpdateMatches(true);
            }
        }

        /// <summary>
        /// Handles the keyboard gestures the find bar owns: <c>Enter</c> and <c>Shift+Enter</c> step
        /// through the matches and <c>Escape</c> closes the bar.
        /// </summary>
        private void OnFindTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    FindNext((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CloseFindPanel();
                    e.Handled = true;
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Bubbling rather than tunneling, so a code block's own search panel gets first refusal
            // on Escape while it is open.
            if (!e.Handled && e.Key == Key.Escape && IsFindPanelOpen)
            {
                CloseFindPanel();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Recomputes the matches for the current find text and options.
        /// </summary>
        /// <param name="selectFirst">
        /// Whether to move the selection to the first match at or after the caret.
        /// </param>
        private void UpdateMatches(bool selectFirst)
        {
            ClearHighlights();
            ClearEditorSelections();

            var regex = MarkdownDocumentSearch.BuildRegex(FindText, FindMatchCase, FindWholeWords, FindUseRegex);
            _matches = MarkdownDocumentSearch.FindAll(_richTextBox?.Document, regex);
            _currentMatchIndex = -1;

            ApplyHighlights();

            if (selectFirst && _matches.Count > 0)
            {
                SelectMatch(FirstMatchAtOrAfterCaret());
            }

            UpdateFindStatus();
        }

        /// <summary>
        /// Returns the index of the first match at or after the current caret position so that
        /// typing in the find bar moves forward through the document rather than jumping back to
        /// the top.
        /// </summary>
        private int FirstMatchAtOrAfterCaret()
        {
            var caret = _richTextBox?.Selection.Start;

            if (caret == null)
            {
                return 0;
            }

            for (int i = 0; i < _matches.Count; i++)
            {
                if (_matches[i].Range is { } range && caret.CompareTo(range.Start) <= 0)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Moves the selection to the next or previous match, wrapping at either end of the document.
        /// </summary>
        /// <param name="forward">Whether to move forward through the document.</param>
        public void FindNext(bool forward)
        {
            if (!IsFindPanelOpen)
            {
                ShowFindPanel();
                return;
            }

            if (_matches.Count == 0)
            {
                return;
            }

            int next = _currentMatchIndex < 0
                ? (forward ? 0 : _matches.Count - 1)
                : (_currentMatchIndex + (forward ? 1 : -1) + _matches.Count) % _matches.Count;

            SelectMatch(next);
        }

        /// <summary>
        /// Selects the match at the supplied index and scrolls it into view.
        /// </summary>
        /// <param name="index">The index of the match within the current result set.</param>
        private void SelectMatch(int index)
        {
            if (_richTextBox == null || index < 0 || index >= _matches.Count)
            {
                return;
            }

            _currentMatchIndex = index;
            var match = _matches[index];
            ClearEditorSelections();
            PaintCurrentMatch(match.Range);

            if (match.Range != null)
            {
                _richTextBox.Selection.Select(match.Range.Start, match.Range.End);
                BringRangeIntoView(match.Range);
            }
            else if (match.Editor != null)
            {
                // The document selection would otherwise keep showing the previous match while the
                // current one is highlighted inside a code block.
                _richTextBox.Selection.Select(_richTextBox.Selection.Start, _richTextBox.Selection.Start);
                match.Editor.Select(match.EditorOffset, match.Length);
                BringEditorMatchIntoView(match.Editor, match.EditorOffset);
            }

            UpdateFindStatus();
        }

        /// <summary>
        /// Scrolls a matched range to the middle of the viewer.
        /// </summary>
        /// <param name="range">The range to reveal.</param>
        private void BringRangeIntoView(TextRange range)
        {
            if (_richTextBox == null)
            {
                return;
            }

            try
            {
                var rect = range.Start.GetCharacterRect(LogicalDirection.Forward);

                if (!rect.IsEmpty)
                {
                    ScrollIntoView(rect.Top, rect.Height);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Scrolls the viewer so that a band of the document, measured in the hosting rich text
        /// box own coordinate space, is centered in the viewport.
        /// </summary>
        /// <param name="top">The top of the band relative to the rich text box.</param>
        /// <param name="height">The height of the band.</param>
        private void ScrollIntoView(double top, double height)
        {
            if (_richTextBox == null || _richTextBox.ViewportHeight <= 0)
            {
                return;
            }

            if (top >= 0 && top + height <= _richTextBox.ViewportHeight)
            {
                return;
            }

            _richTextBox.ScrollToVerticalOffset(
                _richTextBox.VerticalOffset + top - ((_richTextBox.ViewportHeight - height) / 2));
        }

        /// <summary>
        /// Moves the current-match highlight to the supplied range, restoring the previous one to
        /// the ordinary match highlight. A <c>null</c> range clears the current highlight, which is
        /// what a match inside a code block editor needs.
        /// </summary>
        /// <param name="range">The range to paint as the current match.</param>
        private void PaintCurrentMatch(TextRange? range)
        {
            try
            {
                _currentHighlightRange?.ApplyPropertyValue(TextElement.BackgroundProperty, FindHighlightBrush);
                range?.ApplyPropertyValue(TextElement.BackgroundProperty, FindCurrentHighlightBrush);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            _currentHighlightRange = range;
        }

        /// <summary>
        /// Scrolls a match inside a code block editor into view. The editor is sized to its whole
        /// document, so the scrolling happens in the viewer rather than in the editor.
        /// </summary>
        /// <param name="editor">The editor holding the match.</param>
        /// <param name="offset">The offset of the match in the editor's document.</param>
        private void BringEditorMatchIntoView(SyntaxEditor editor, int offset)
        {
            try
            {
                if (editor.Document == null)
                {
                    return;
                }

                var textView = editor.TextArea.TextView;
                var line = editor.Document.GetLineByOffset(offset);
                double top = textView.GetVisualTopByDocumentLine(line.LineNumber) - textView.VerticalOffset;
                double height = textView.DefaultLineHeight;

                // The position has to be expressed in the rich text box coordinate space, since
                // that is the element doing the scrolling; the editor shows its whole document and
                // has nothing to scroll itself.
                if (_richTextBox != null && editor.IsDescendantOf(_richTextBox))
                {
                    var origin = editor.TransformToAncestor(_richTextBox).Transform(new Point(0, top));
                    ScrollIntoView(origin.Y, height);
                    return;
                }

                editor.BringIntoView(new Rect(0, top, 1, height));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                editor.BringIntoView();
            }
        }

        /// <summary>
        /// Paints every match in the document with the find highlight.
        /// </summary>
        private void ApplyHighlights()
        {
            foreach (var match in _matches)
            {
                if (match.Range == null)
                {
                    continue;
                }

                try
                {
                    match.Range.ApplyPropertyValue(TextElement.BackgroundProperty, FindHighlightBrush);
                    _highlightedRanges.Add(match.Range);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Removes the find highlight from the ranges it was applied to.
        /// </summary>
        private void ClearHighlights()
        {
            foreach (var range in _highlightedRanges)
            {
                try
                {
                    range.ApplyPropertyValue(TextElement.BackgroundProperty, null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            _highlightedRanges.Clear();
            _currentHighlightRange = null;
        }

        /// <summary>
        /// Clears the selection in every code block editor that holds a match.
        /// </summary>
        private void ClearEditorSelections()
        {
            foreach (var editor in _matches.Select(m => m.Editor).Where(e => e != null).Distinct())
            {
                editor!.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Refreshes the find bar's result summary.
        /// </summary>
        private void UpdateFindStatus()
        {
            string status;

            if (!IsFindPanelOpen || string.IsNullOrEmpty(FindText))
            {
                status = string.Empty;
            }
            else if (_matches.Count == 0)
            {
                status = "No results";
            }
            else
            {
                status = $"{Math.Max(_currentMatchIndex, 0) + 1} of {_matches.Count}";
            }

            SetValue(FindStatusTextPropertyKey, status);
        }

        /// <summary>
        /// Creates a frozen brush from the supplied channels.
        /// </summary>
        private static Brush CreateFrozenBrush(byte a, byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            brush.Freeze();

            return brush;
        }

        /// <summary>
        /// Creates a minimal <see cref="FlowDocument"/> that displays the supplied text as plain text.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <returns>A <see cref="FlowDocument"/> containing the text in a single paragraph.</returns>
        private static FlowDocument CreateFallbackDocument(string? text)
        {
            return new FlowDocument(new Paragraph(new Run(text ?? string.Empty)))
            {
                PagePadding = new Thickness(0)
            };
        }
    }
}
