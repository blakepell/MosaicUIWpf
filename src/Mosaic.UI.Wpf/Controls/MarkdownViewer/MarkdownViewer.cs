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
        /// The name of the <see cref="RichTextBox"/> template part that hosts the rendered document.
        /// </summary>
        private const string PartRichTextBox = "PART_RichTextBox";

        /// <summary>
        /// The hosting rich text box, resolved from the template.
        /// </summary>
        private RichTextBox? _richTextBox;

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

        /// <summary>
        /// Opens a hyperlink in the system default browser when the user clicks it, hit-testing the
        /// click position so a single click works inside the read-only <see cref="RichTextBox"/>.
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

            try
            {
                Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            e.Handled = true;
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
        /// Re-renders the document when the <see cref="Markdown"/> property changes.
        /// </summary>
        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MarkdownViewer)d).RenderMarkdown(e.NewValue as string ?? string.Empty);
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
                document = MarkdownFlowDocumentRenderer.Render(markdown);
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
