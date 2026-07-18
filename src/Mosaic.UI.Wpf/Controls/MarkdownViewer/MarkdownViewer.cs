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
    [TemplatePart(Name = PartRichTextBox, Type = typeof(RichTextBox))]
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
        /// The hosting rich text box, resolved from the template.
        /// </summary>
        private RichTextBox? _richTextBox;

        /// <summary>
        /// The absolute URI of the document currently loaded via <see cref="Source"/>, used to
        /// resolve relative links and images. <c>null</c> when the Markdown was supplied directly.
        /// </summary>
        private Uri? _resolvedSource;

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
        }

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

                // A RichTextBox's text editor intercepts mouse clicks for selection before a
                // Hyperlink can raise RequestNavigate (even when read-only), so links are opened by
                // hit-testing the click position for a hyperlink instead.
                _richTextBox.PreviewMouseLeftButtonDown -= OnRichTextBoxPreviewMouseLeftButtonDown;
                _richTextBox.PreviewMouseLeftButtonDown += OnRichTextBoxPreviewMouseLeftButtonDown;
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

                if (_richTextBox?.Document is { } document)
                {
                    ScaleDocumentFontSizes(document, newFontSize / oldFontSize);
                }
            }

            e.Handled = true;
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
        /// Handles a clicked link: raises <see cref="LinkClicked"/> first so the application can
        /// intercept it, then either navigates the viewer to another Markdown document (pack
        /// resource or local <c>.md</c> file) or opens the link with the system default handler.
        /// </summary>
        /// <param name="uri">The link target; may be relative.</param>
        private void NavigateTo(Uri uri)
        {
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

            FlowDocument document;

            try
            {
                document = MarkdownFlowDocumentRenderer.Render(markdown, _resolvedSource);
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
