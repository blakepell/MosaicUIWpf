/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mosaic.UI.Wpf.Cache;
using Mosaic.UI.Wpf.Themes;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigTableCell = Markdig.Extensions.Tables.TableCell;
using MarkdigTableRow = Markdig.Extensions.Tables.TableRow;
using WpfBlock = System.Windows.Documents.Block;
using WpfHyperlink = System.Windows.Documents.Hyperlink;
using WpfTableCell = System.Windows.Documents.TableCell;
using WpfTableRow = System.Windows.Documents.TableRow;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a Markdown string into a WPF <see cref="FlowDocument"/> by walking the Markdig
    /// abstract syntax tree. The produced document references Mosaic theme tokens via
    /// <see cref="FrameworkContentElement.SetResourceReference"/> so colors update live when the
    /// theme changes, and renders to standard <see cref="FlowDocument"/> elements so the hosting
    /// <see cref="System.Windows.Controls.RichTextBox"/> supports text selection and rich copy.
    /// </summary>
    public static class MarkdownFlowDocumentRenderer
    {
        /// <summary>
        /// The shared Markdig pipeline used to parse Markdown. Advanced extensions enable pipe
        /// tables, auto-links, and other common GitHub-flavored constructs.
        /// </summary>
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        /// <summary>
        /// The base URIs a render pass resolves relative link and image targets against.
        /// </summary>
        /// <param name="BaseUri">
        /// The primary base URI, typically the location of the document being displayed.
        /// </param>
        /// <param name="ImageBaseUri">
        /// An additional base URI tried when a relative image does not resolve against
        /// <paramref name="BaseUri"/>, typically the viewer's storage folder.
        /// </param>
        /// <param name="HeadingBottomSpacing">
        /// The space, in device-independent pixels, left below each rendered heading.
        /// </param>
        /// <param name="PlainCodeBlocks">
        /// Whether multi-line code blocks are rendered as monospace paragraphs rather than hosted
        /// in an embedded <see cref="SyntaxEditor"/>. Export formats such as RTF cannot carry
        /// embedded controls, so exported documents use plain paragraphs instead.
        /// </param>
        /// <param name="PlainCustomElements">
        /// Whether custom elements (<c>&lt;shield&gt;</c>, <c>&lt;pill&gt;</c>) are rendered as their
        /// text rather than as embedded <see cref="Shield"/> and <see cref="Badge"/> controls, for
        /// the same reason as <paramref name="PlainCodeBlocks"/>.
        /// </param>
        private readonly record struct RenderContext(Uri? BaseUri, Uri? ImageBaseUri, double HeadingBottomSpacing, bool PlainCodeBlocks = false, bool PlainCustomElements = false);

        /// <summary>
        /// The default space, in device-independent pixels, left below each rendered heading.
        /// </summary>
        public const double DefaultHeadingBottomSpacing = 6;

        /// <summary>
        /// The prefix that marks a link as an application event rather than a navigable target,
        /// for example <c>[Show](@ShowArticle?keyword=bpell)</c>. Such links are rendered with a
        /// relative <see cref="Hyperlink.NavigateUri"/> that preserves the original text so the
        /// hosting <see cref="MarkdownViewer"/> can raise <see cref="MarkdownViewer.EventRaised"/>.
        /// </summary>
        public const string EventLinkPrefix = "@";

        /// <summary>
        /// Identifies the <c>HeadingId</c> attached property, which carries the anchor identifier of a
        /// rendered heading paragraph. Identifiers come from Markdig's auto-identifier extension (for
        /// example <c>## Event Links</c> becomes <c>event-links</c>) or from an explicit
        /// <c>{#custom-id}</c> attribute, so in-page links such as <c>[Jump](#event-links)</c> can be
        /// resolved to the heading they target.
        /// </summary>
        public static readonly DependencyProperty HeadingIdProperty = DependencyProperty.RegisterAttached(
            "HeadingId",
            typeof(string),
            typeof(MarkdownFlowDocumentRenderer),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the anchor identifier of a rendered heading.
        /// </summary>
        /// <param name="element">The rendered heading element.</param>
        /// <returns>The heading's anchor identifier, or <c>null</c> when the element is not a heading.</returns>
        public static string? GetHeadingId(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (string?)element.GetValue(HeadingIdProperty);
        }

        /// <summary>
        /// Sets the anchor identifier of a rendered heading.
        /// </summary>
        /// <param name="element">The rendered heading element.</param>
        /// <param name="value">The anchor identifier.</param>
        public static void SetHeadingId(DependencyObject element, string? value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(HeadingIdProperty, value);
        }

        /// <summary>
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/>.
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        public static FlowDocument Render(string? markdown)
        {
            return Render(markdown, null, null);
        }

        /// <summary>
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/>, resolving relative
        /// link and image URLs against the supplied base URI (for example the pack URI of the
        /// document being displayed).
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <param name="baseUri">
        /// The absolute URI relative links and images are resolved against, or <c>null</c> to leave
        /// relative links unresolved.
        /// </param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        public static FlowDocument Render(string? markdown, Uri? baseUri)
        {
            return Render(markdown, baseUri, null);
        }

        /// <summary>
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/>, resolving relative
        /// link and image URLs against <paramref name="baseUri"/> and falling back to
        /// <paramref name="imageBaseUri"/> for images that do not resolve there.
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <param name="baseUri">
        /// The absolute URI relative links and images are resolved against, or <c>null</c> to leave
        /// relative links unresolved.
        /// </param>
        /// <param name="imageBaseUri">
        /// An additional absolute base URI (typically a storage folder) tried when a relative image
        /// does not resolve against <paramref name="baseUri"/>, or <c>null</c> for none.
        /// </param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        public static FlowDocument Render(string? markdown, Uri? baseUri, Uri? imageBaseUri)
        {
            return Render(markdown, baseUri, imageBaseUri, DefaultHeadingBottomSpacing);
        }

        /// <summary>
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/>, resolving relative
        /// link and image URLs against <paramref name="baseUri"/> and falling back to
        /// <paramref name="imageBaseUri"/> for images that do not resolve there, and leaving
        /// <paramref name="headingBottomSpacing"/> pixels of space below each heading.
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <param name="baseUri">
        /// The absolute URI relative links and images are resolved against, or <c>null</c> to leave
        /// relative links unresolved.
        /// </param>
        /// <param name="imageBaseUri">
        /// An additional absolute base URI (typically a storage folder) tried when a relative image
        /// does not resolve against <paramref name="baseUri"/>, or <c>null</c> for none.
        /// </param>
        /// <param name="headingBottomSpacing">
        /// The space, in device-independent pixels, left below each rendered heading. Negative or
        /// non-finite values are treated as zero.
        /// </param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        public static FlowDocument Render(string? markdown, Uri? baseUri, Uri? imageBaseUri, double headingBottomSpacing)
        {
            if (double.IsNaN(headingBottomSpacing) || double.IsInfinity(headingBottomSpacing) || headingBottomSpacing < 0)
            {
                headingBottomSpacing = 0;
            }

            return Render(markdown, new RenderContext(baseUri, imageBaseUri, headingBottomSpacing));
        }

        /// <summary>
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/> suitable for export
        /// (RTF, XPS, or printing). The output matches <see cref="Render(string?, Uri?, Uri?, double)"/>
        /// except that code blocks are always monospace paragraphs and shields and pills are plain text, since
        /// embedded controls cannot be serialized to RTF and do not paginate reliably.
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <param name="baseUri">The absolute URI relative links and images are resolved against, if any.</param>
        /// <param name="imageBaseUri">An additional base URI tried for relative images, if any.</param>
        /// <param name="headingBottomSpacing">The space, in device-independent pixels, left below each heading.</param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        internal static FlowDocument RenderForExport(string? markdown, Uri? baseUri, Uri? imageBaseUri, double headingBottomSpacing)
        {
            if (double.IsNaN(headingBottomSpacing) || double.IsInfinity(headingBottomSpacing) || headingBottomSpacing < 0)
            {
                headingBottomSpacing = 0;
            }

            return Render(markdown, new RenderContext(baseUri, imageBaseUri, headingBottomSpacing, PlainCodeBlocks: true, PlainCustomElements: true));
        }

        /// <summary>
        /// Renders the supplied Markdown text using an already-built resolution context.
        /// </summary>
        private static FlowDocument Render(string? markdown, RenderContext context)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(0)
            };

            RenderBlocks(markdown ?? string.Empty, context, document.Blocks);
            return document;
        }

        /// <summary>
        /// Parses <paramref name="markdown"/> and appends its rendered top-level blocks to
        /// <paramref name="target"/>. A <c>&lt;panel&gt;</c> element is located in the source text
        /// rather than in the syntax tree because a CommonMark HTML block ends at the first blank
        /// line, so a panel with several paragraphs is split across many sibling blocks; every block
        /// that starts inside the panel is consumed by it.
        /// </summary>
        /// <param name="markdown">The Markdown source.</param>
        /// <param name="context">The active render context.</param>
        /// <param name="target">The collection the rendered blocks are added to.</param>
        private static void RenderBlocks(string markdown, RenderContext context, BlockCollection target)
        {
            var parsed = Markdown.Parse(markdown, Pipeline);

            for (int i = 0; i < parsed.Count; i++)
            {
                var block = parsed[i];

                if (block is HtmlBlock && TryFindPanel(markdown, block.Span.Start, out var panel))
                {
                    target.Add(RenderPanel(panel, context));

                    // Skip the blocks the panel consumed. An HTML block runs to the next blank line,
                    // so the last of them can carry content after the closing tag, which is rendered
                    // on its own.
                    int consumedEnd = block.Span.End + 1;

                    while (i + 1 < parsed.Count && parsed[i + 1].Span.Start < panel.End)
                    {
                        i++;
                        consumedEnd = parsed[i].Span.End + 1;
                    }

                    if (consumedEnd > panel.End && consumedEnd <= markdown.Length)
                    {
                        string remainder = markdown[panel.End..consumedEnd];

                        if (!string.IsNullOrWhiteSpace(remainder))
                        {
                            RenderBlocks(remainder, context, target);
                        }
                    }

                    continue;
                }

                var rendered = RenderBlock(block, context);

                if (rendered != null)
                {
                    target.Add(rendered);
                }
            }
        }

        /// <summary>
        /// Renders a single Markdig <see cref="Block"/> into the equivalent <see cref="Block"/> element.
        /// </summary>
        /// <param name="block">The block to render.</param>
        /// <param name="context">The base URIs relative links and images are resolved against, if any.</param>
        /// <returns>The rendered block, or <c>null</c> when the block type is unsupported.</returns>
        private static WpfBlock? RenderBlock(MarkdigBlock block, RenderContext context)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    return RenderHeading(heading, context);
                case ParagraphBlock paragraph:
                    return RenderParagraph(paragraph, context);
                case ListBlock list:
                    return RenderList(list, context);
                case QuoteBlock quote:
                    return RenderQuote(quote, context);
                case MarkdigTable table:
                    return RenderTable(table, context);
                case CodeBlock code:
                    return RenderCodeBlock(code, context);
                case ThematicBreakBlock:
                    return RenderThematicBreak();
                case HtmlBlock html:
                    return RenderHtmlBlock(html, context);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Renders a heading block, scaling the font size by heading level and leaving the
        /// context's configured spacing below it.
        /// </summary>
        private static WpfBlock RenderHeading(HeadingBlock heading, RenderContext context)
        {
            var paragraph = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                FontSize = HeadingFontSize(heading.Level),
                Margin = new Thickness(0, heading.Level <= 2 ? 12 : 8, 0, context.HeadingBottomSpacing)
            };

            string? id = heading.GetAttributes().Id;

            if (!string.IsNullOrEmpty(id))
            {
                SetHeadingId(paragraph, id);
            }

            AddInlines(paragraph.Inlines, heading.Inline, context);
            return paragraph;
        }

        /// <summary>
        /// Maps a heading level (1-6) to a relative font size in device-independent pixels.
        /// </summary>
        private static double HeadingFontSize(int level)
        {
            return level switch
            {
                1 => 24,
                2 => 20,
                3 => 17,
                4 => 15,
                5 => 13,
                _ => 12
            };
        }

        /// <summary>
        /// Renders a paragraph block.
        /// </summary>
        private static WpfBlock RenderParagraph(ParagraphBlock paragraph, RenderContext context)
        {
            var result = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            AddInlines(result.Inlines, paragraph.Inline, context);
            return result;
        }

        /// <summary>
        /// Renders a bulleted or numbered list, including nested lists.
        /// </summary>
        private static WpfBlock RenderList(ListBlock listBlock, RenderContext context)
        {
            var list = new List
            {
                Margin = new Thickness(0, 0, 0, 8),
                MarkerStyle = listBlock.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc
            };

            if (listBlock.IsOrdered && int.TryParse(listBlock.OrderedStart, out int start))
            {
                list.StartIndex = start;
            }

            foreach (var item in listBlock)
            {
                if (item is not ListItemBlock itemBlock)
                {
                    continue;
                }

                var listItem = new ListItem();

                foreach (var child in itemBlock)
                {
                    var rendered = RenderBlock(child, context);

                    if (rendered != null)
                    {
                        listItem.Blocks.Add(rendered);
                    }
                }

                // Ensure the list item is never empty, which WPF does not allow.
                if (listItem.Blocks.Count == 0)
                {
                    listItem.Blocks.Add(new Paragraph());
                }

                list.ListItems.Add(listItem);
            }

            return list;
        }

        /// <summary>
        /// Renders a block quote as a bordered, indented <see cref="Section"/>.
        /// </summary>
        private static WpfBlock RenderQuote(QuoteBlock quote, RenderContext context)
        {
            var section = new Section
            {
                Margin = new Thickness(0, 12, 0, 12),
                Padding = new Thickness(10, 4, 0, 4),
                BorderThickness = new Thickness(3, 0, 0, 0),
            };

            section.SetResourceReference(WpfBlock.BorderBrushProperty, MosaicTheme.AccentBrush);
            section.SetResourceReference(TextElement.ForegroundProperty, MosaicTheme.ControlTextSecondaryForegroundBrush);

            foreach (var child in quote)
            {
                var rendered = RenderBlock(child, context);

                if (rendered != null)
                {
                    section.Blocks.Add(rendered);
                }
            }

            if (section.Blocks.Count == 0)
            {
                section.Blocks.Add(new Paragraph());
            }

            // Child paragraphs carry their own top/bottom margins; strip the leading top and trailing
            // bottom margins so the Section's padding alone controls the spacing and renders evenly
            // above and below the quoted text.
            TrimOuterMargins(section.Blocks);
            return section;
        }

        /// <summary>
        /// Removes the top margin of the first block and the bottom margin of the last block so the
        /// container's own padding alone controls the spacing around its content.
        /// </summary>
        private static void TrimOuterMargins(BlockCollection blocks)
        {
            if (blocks.FirstBlock is { } firstBlock)
            {
                var margin = firstBlock.Margin;
                firstBlock.Margin = new Thickness(margin.Left, 0, margin.Right, margin.Bottom);
            }

            if (blocks.LastBlock is { } lastBlock)
            {
                var margin = lastBlock.Margin;
                lastBlock.Margin = new Thickness(margin.Left, margin.Top, margin.Right, 0);
            }
        }

        /// <summary>
        /// Renders a pipe table into a WPF <see cref="System.Windows.Documents.Table"/>.
        /// </summary>
        private static WpfBlock RenderTable(MarkdigTable table, RenderContext context)
        {
            var wpfTable = new System.Windows.Documents.Table
            {
                Margin = new Thickness(0, 0, 0, 8),
                CellSpacing = 0
            };

            var rowGroup = new TableRowGroup();
            wpfTable.RowGroups.Add(rowGroup);

            foreach (var rowObj in table)
            {
                if (rowObj is not MarkdigTableRow tableRow)
                {
                    continue;
                }

                var wpfRow = new WpfTableRow();

                foreach (var cellObj in tableRow)
                {
                    if (cellObj is not MarkdigTableCell tableCell)
                    {
                        continue;
                    }

                    var paragraph = new Paragraph { Margin = new Thickness(0) };

                    foreach (var cellBlock in tableCell)
                    {
                        if (cellBlock is ParagraphBlock cellParagraph)
                        {
                            AddInlines(paragraph.Inlines, cellParagraph.Inline, context);
                        }
                    }

                    var wpfCell = new WpfTableCell(paragraph)
                    {
                        Padding = new Thickness(6, 3, 6, 3),
                        BorderThickness = new Thickness(1)
                    };

                    wpfCell.SetResourceReference(WpfTableCell.BorderBrushProperty, MosaicTheme.ControlSeparatorBrush);

                    if (tableRow.IsHeader)
                    {
                        paragraph.FontWeight = FontWeights.Bold;
                    }

                    wpfRow.Cells.Add(wpfCell);
                }

                rowGroup.Rows.Add(wpfRow);
            }

            return wpfTable;
        }

        /// <summary>
        /// Renders a fenced or indented code block. Multi-line blocks are hosted in a read-only
        /// <see cref="SyntaxEditor"/> so the code is syntax highlighted when the fence names a
        /// supported language (<c>```csharp</c>) and displayed as plain text when it does not.
        /// Single-line blocks stay a lightweight monospace, shaded paragraph.
        /// </summary>
        private static WpfBlock RenderCodeBlock(CodeBlock code, RenderContext context)
        {
            string text = GetCodeBlockText(code);

            if (!context.PlainCodeBlocks && text.Contains('\n'))
            {
                var editorBlock = TryRenderCodeBlockEditor(text, (code as FencedCodeBlock)?.Info);

                if (editorBlock != null)
                {
                    return editorBlock;
                }
            }

            return RenderCodeBlockParagraph(text);
        }

        /// <summary>
        /// Joins a code block's lines into a single string, trimming trailing blank lines so an
        /// embedded editor does not size itself around empty space at the end of the fence.
        /// </summary>
        private static string GetCodeBlockText(CodeBlock code)
        {
            var lines = code.Lines.Lines;
            int count = code.Lines.Count;

            while (count > 0 && lines[count - 1].Slice.ToString().Trim().Length == 0)
            {
                count--;
            }

            var builder = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i].Slice.ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Hosts a multi-line code block in a read-only <see cref="SyntaxEditor"/> sized to its full
        /// content, so the editor never scrolls and the mouse wheel keeps scrolling the document that
        /// contains it.
        /// </summary>
        /// <param name="text">The code to display.</param>
        /// <param name="info">The fence's language identifier, if the block was fenced.</param>
        /// <returns>
        /// The rendered block, or <c>null</c> when the editor could not be created (for example when no
        /// WPF application is available to resolve its resources), so the caller can fall back to text.
        /// </returns>
        private static WpfBlock? TryRenderCodeBlockEditor(string text, string? info)
        {
            try
            {
                var editor = new SyntaxEditor
                {
                    Text = text,
                    Language = SyntaxLanguageMap.FromMarkdownLanguage(info),
                    IsReadOnly = true,
                    ShowLineNumbers = true,
                    StatusBarVisible = false,
                    ClearVisible = false,
                    WordWrap = false,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6, 4, 6, 4),
                    // The editor stretches to the height of its document, so it has nothing to scroll
                    // vertically; a horizontal scroll bar only appears for long lines and leaves the
                    // containing document's vertical scrolling alone.
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                editor.Options.HighlightCurrentLine = false;
                editor.Options.AllowScrollBelowDocument = false;

                // AvalonEdit's scroll viewer marks the wheel as handled even when it cannot scroll, so
                // the event is re-raised on the parent to keep the outer scroll viewer responsive.
                editor.PreviewMouseWheel += OnCodeBlockEditorPreviewMouseWheel;

                var border = new Border
                {
                    Child = editor,
                    BorderThickness = new Thickness(1)
                };

                border.SetResourceReference(Border.BorderBrushProperty, MosaicTheme.ControlSeparatorBrush);

                // TextBoxBase.IsReadOnly is an inheriting property, so the read-only rich text box
                // hosting this document pushes its own read-only state down into every text box
                // inside the code block - including the one in the editor's Ctrl+F search panel,
                // which then silently refuses input. The editor's own read-only state is
                // AvalonEdit's separate property and is unaffected by this.
                border.SetValue(System.Windows.Controls.Primitives.TextBoxBase.IsReadOnlyProperty, false);

                return new BlockUIContainer(border)
                {
                    Margin = new Thickness(0, 0, 0, 8)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Re-raises a mouse wheel event on an embedded code editor's parent so the wheel scrolls the
        /// document rather than being swallowed by the editor's own scroll viewer.
        /// </summary>
        private static void OnCodeBlockEditorPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || sender is not UIElement element)
            {
                return;
            }

            if (VisualTreeHelper.GetParent(element) is not UIElement parent)
            {
                return;
            }

            e.Handled = true;

            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = parent
            });
        }

        /// <summary>
        /// Renders code as a monospace, shaded paragraph.
        /// </summary>
        private static WpfBlock RenderCodeBlockParagraph(string text)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 8, 10, 8)
            };

            paragraph.SetResourceReference(TextElement.FontFamilyProperty, MosaicTheme.MonospaceFontFamily);
            paragraph.SetResourceReference(WpfBlock.BackgroundProperty, MosaicTheme.ControlBackgroundLightBrush);
            paragraph.SetResourceReference(WpfBlock.BorderBrushProperty, MosaicTheme.ControlSeparatorBrush);
            paragraph.BorderThickness = new Thickness(1);

            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }

                paragraph.Inlines.Add(new Run(lines[i]));
            }

            return paragraph;
        }

        /// <summary>
        /// Renders a thematic break (horizontal rule) as a thin separator line.
        /// </summary>
        private static WpfBlock RenderThematicBreak()
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 4, 0, 12),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            paragraph.SetResourceReference(WpfBlock.BorderBrushProperty, MosaicTheme.ControlSeparatorBrush);
            return paragraph;
        }

        /// <summary>
        /// Renders a raw HTML block as plain, monospace text rather than interpreting the markup. A
        /// block made up only of custom elements such as <c>&lt;shield&gt;</c> and <c>&lt;pill&gt;</c>
        /// (for example a self-closing shield alone on a line, which CommonMark classifies as an HTML
        /// block) is rendered as a paragraph of those elements instead.
        /// </summary>
        private static WpfBlock RenderHtmlBlock(HtmlBlock html, RenderContext context)
        {
            var customParagraph = TryRenderCustomElementBlock(html, context);

            if (customParagraph != null)
            {
                return customParagraph;
            }

            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            paragraph.SetResourceReference(TextElement.FontFamilyProperty, MosaicTheme.MonospaceFontFamily);

            int count = html.Lines.Count;
            var lines = html.Lines.Lines;
            bool first = true;

            for (int i = 0; i < count; i++)
            {
                if (!first)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }

                paragraph.Inlines.Add(new Run(lines[i].Slice.ToString()));
                first = false;
            }

            return paragraph;
        }

        /// <summary>
        /// The source of a <c>&lt;panel&gt;</c> element located in Markdown text.
        /// </summary>
        /// <param name="Attributes">The raw attribute text of the opening tag.</param>
        /// <param name="Inner">The text between the opening and closing tags.</param>
        /// <param name="End">The source index just past the closing tag.</param>
        private readonly record struct PanelSource(string Attributes, string Inner, int End);

        /// <summary>
        /// Matches a <c>&lt;panel&gt;</c> opening tag at the start of an HTML block.
        /// </summary>
        private static readonly Regex PanelOpenTagRegex = new(@"\G[ \t]*<panel(?<attrs>\s[^>]*?)?\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Matches any opening or closing <c>&lt;panel&gt;</c> tag, used to pair nested panels.
        /// </summary>
        private static readonly Regex PanelTagRegex = new(@"<(?<close>/)?panel(?:\s[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Matches a panel's <c>&lt;header&gt;</c> or <c>&lt;footer&gt;</c> slot.
        /// </summary>
        private static readonly Regex PanelSlotRegex = new(@"<(?<name>header|footer)(?:\s[^>]*)?>(?<inner>.*?)</\k<name>\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        /// <summary>
        /// Locates a <c>&lt;panel&gt;</c> element whose opening tag starts at <paramref name="start"/>,
        /// pairing nested panels so the matching closing tag is found.
        /// </summary>
        /// <param name="markdown">The Markdown source.</param>
        /// <param name="start">The index of the HTML block that may open a panel.</param>
        /// <param name="panel">The located panel.</param>
        /// <returns><c>false</c> when no panel opens there or it is never closed.</returns>
        private static bool TryFindPanel(string markdown, int start, out PanelSource panel)
        {
            panel = default;

            if (start < 0 || start >= markdown.Length)
            {
                return false;
            }

            var open = PanelOpenTagRegex.Match(markdown, start);

            if (!open.Success)
            {
                return false;
            }

            int innerStart = open.Index + open.Length;
            int depth = 1;

            for (var tag = PanelTagRegex.Match(markdown, innerStart); tag.Success; tag = tag.NextMatch())
            {
                depth += tag.Groups["close"].Success ? -1 : 1;

                if (depth == 0)
                {
                    panel = new PanelSource(open.Groups["attrs"].Value, markdown[innerStart..tag.Index], tag.Index + tag.Length);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Renders a <c>&lt;panel title="..."&gt;</c> element as a <see cref="ContentPanel"/>. The
        /// text between the tags is Markdown and becomes the panel's content. An optional
        /// <c>&lt;header&gt;</c> holds Markdown shown right-aligned in the title bar, and an optional
        /// <c>&lt;footer&gt;</c> holds Markdown shown in the footer. Without a <c>title</c> the
        /// header (and any <c>&lt;header&gt;</c>) is collapsed; without a <c>&lt;footer&gt;</c> the
        /// footer is collapsed.
        /// </summary>
        private static WpfBlock RenderPanel(PanelSource source, RenderContext context)
        {
            var attributes = ParseHtmlAttributes(source.Attributes);
            attributes.TryGetValue("title", out var title);

            var nested = GetNestedPanelRanges(source.Inner);
            string? header = null;
            string? footer = null;

            // Pull out the panel's own header and footer, leaving those of nested panels in place.
            string body = PanelSlotRegex.Replace(source.Inner, match =>
            {
                if (nested.Any(range => match.Index >= range.Start && match.Index < range.End))
                {
                    return match.Value;
                }

                if (string.Equals(match.Groups["name"].Value, "header", StringComparison.OrdinalIgnoreCase))
                {
                    header ??= match.Groups["inner"].Value;
                }
                else
                {
                    footer ??= match.Groups["inner"].Value;
                }

                return string.Empty;
            });

            body = Dedent(body);
            header = string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(header) ? null : Dedent(header).Trim();
            footer = string.IsNullOrWhiteSpace(footer) ? null : Dedent(footer).Trim();

            if (!context.PlainCustomElements)
            {
                try
                {
                    var contentPanel = new ContentPanel
                    {
                        Title = title ?? string.Empty,
                        Content = CreatePanelSlot(body, context, TextAlignment.Left, preferTextBlock: false)
                    };

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        contentPanel.HeaderVisibility = Visibility.Collapsed;
                    }
                    else if (header != null)
                    {
                        var headerContent = CreatePanelSlot(header, context, TextAlignment.Right, preferTextBlock: true);
                        headerContent.HorizontalAlignment = HorizontalAlignment.Right;
                        headerContent.VerticalAlignment = VerticalAlignment.Center;
                        contentPanel.HeaderContent = headerContent;
                    }

                    if (footer != null)
                    {
                        contentPanel.FooterContent = CreatePanelSlot(footer, context, TextAlignment.Left, preferTextBlock: true);
                    }
                    else
                    {
                        contentPanel.FooterVisibility = Visibility.Collapsed;
                    }

                    return new BlockUIContainer(contentPanel)
                    {
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            return RenderPlainPanel(title, header, body, footer, context);
        }

        /// <summary>
        /// Renders a panel as a bordered <see cref="Section"/> of ordinary text, used for export (where
        /// embedded controls cannot be serialized) and when the control cannot be created.
        /// </summary>
        private static WpfBlock RenderPlainPanel(string? title, string? header, string body, string? footer, RenderContext context)
        {
            var section = new Section
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 6, 10, 6),
                BorderThickness = new Thickness(1)
            };

            section.SetResourceReference(WpfBlock.BorderBrushProperty, MosaicTheme.ControlBorderBrush);

            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                titleParagraph.Inlines.Add(new Bold(new Run(title)));

                if (header != null)
                {
                    titleParagraph.Inlines.Add(new Run("  "));
                    AddSlotInlines(titleParagraph.Inlines, header, context);
                }

                section.Blocks.Add(titleParagraph);
            }

            RenderBlocks(body, context, section.Blocks);

            if (footer != null)
            {
                var footerParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                footerParagraph.SetResourceReference(TextElement.ForegroundProperty, MosaicTheme.ControlTextSecondaryForegroundBrush);
                AddSlotInlines(footerParagraph.Inlines, footer, context);
                section.Blocks.Add(footerParagraph);
            }

            if (section.Blocks.Count == 0)
            {
                section.Blocks.Add(new Paragraph());
            }

            TrimOuterMargins(section.Blocks);
            return section;
        }

        /// <summary>
        /// Renders slot Markdown and appends the inlines of its paragraphs to <paramref name="target"/>,
        /// separated by spaces.
        /// </summary>
        private static void AddSlotInlines(InlineCollection target, string markdown, RenderContext context)
        {
            var section = new Section();
            RenderBlocks(markdown, context, section.Blocks);
            bool first = true;

            foreach (var paragraph in section.Blocks.OfType<Paragraph>().ToList())
            {
                if (!first)
                {
                    target.Add(new Run(" "));
                }

                var inlines = paragraph.Inlines.ToList();
                paragraph.Inlines.Clear();
                target.AddRange(inlines);
                first = false;
            }
        }

        /// <summary>
        /// Builds the element shown in one of a panel's slots. Markdown that renders to a single
        /// paragraph becomes a <see cref="TextBlock"/> sized to its text when
        /// <paramref name="preferTextBlock"/> is set; anything else is hosted in a nested, read-only
        /// <see cref="RichTextBox"/> so lists, code blocks, and selection keep working.
        /// </summary>
        /// <param name="markdown">The slot's Markdown.</param>
        /// <param name="context">The active render context.</param>
        /// <param name="alignment">The text alignment of the slot.</param>
        /// <param name="preferTextBlock">Whether a single paragraph is shown in a <see cref="TextBlock"/>.</param>
        private static FrameworkElement CreatePanelSlot(string markdown, RenderContext context, TextAlignment alignment, bool preferTextBlock)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent,
                TextAlignment = alignment
            };

            RenderBlocks(markdown, context, document.Blocks);

            if (preferTextBlock && document.Blocks.Count == 1 && document.Blocks.FirstBlock is Paragraph paragraph)
            {
                var textBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = alignment
                };

                var inlines = paragraph.Inlines.ToList();
                paragraph.Inlines.Clear();
                textBlock.Inlines.AddRange(inlines);
                return textBlock;
            }

            TrimOuterMargins(document.Blocks);

            var richTextBox = new RichTextBox
            {
                Document = document,
                IsReadOnly = true,
                IsDocumentEnabled = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // A rich text box applies its default page padding (5,0,5,0) to the document when it loads,
            // which would indent the body relative to the panel's title and footer.
            richTextBox.Loaded += static (sender, _) => ((RichTextBox)sender).Document.PagePadding = new Thickness(0);

            // The typography and color come from the panel slot hosting the box, overriding a native
            // RichTextBox style that would otherwise apply its own.
            richTextBox.SetBinding(Control.FontFamilyProperty, new Binding(nameof(Control.FontFamily)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContentPanel), 1) });
            richTextBox.SetBinding(Control.FontSizeProperty, new Binding(nameof(Control.FontSize)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContentPanel), 1) });
            richTextBox.SetBinding(Control.ForegroundProperty, new Binding { Path = new PropertyPath(TextElement.ForegroundProperty), RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContentPresenter), 1) });

            // The box has nothing to scroll, but its scroll viewer would still swallow the wheel.
            richTextBox.PreviewMouseWheel += OnCodeBlockEditorPreviewMouseWheel;

            return richTextBox;
        }

        /// <summary>
        /// Finds the ranges of <paramref name="text"/> covered by nested <c>&lt;panel&gt;</c> elements.
        /// </summary>
        private static List<(int Start, int End)> GetNestedPanelRanges(string text)
        {
            var ranges = new List<(int Start, int End)>();
            int depth = 0;
            int start = 0;

            foreach (Match tag in PanelTagRegex.Matches(text))
            {
                if (tag.Groups["close"].Success)
                {
                    if (depth == 0)
                    {
                        continue;
                    }

                    if (--depth == 0)
                    {
                        ranges.Add((start, tag.Index + tag.Length));
                    }
                }
                else if (depth++ == 0)
                {
                    start = tag.Index;
                }
            }

            if (depth > 0)
            {
                ranges.Add((start, text.Length));
            }

            return ranges;
        }

        /// <summary>
        /// Removes the indentation shared by every non-blank line so Markdown indented inside an
        /// element (for readability) is not parsed as an indented code block.
        /// </summary>
        private static string Dedent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            int indent = int.MaxValue;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int count = 0;

                while (count < line.Length && (line[count] == ' ' || line[count] == '\t'))
                {
                    count++;
                }

                indent = Math.Min(indent, count);
            }

            if (indent is 0 or int.MaxValue)
            {
                return string.Join("\n", lines);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Length >= indent ? lines[i][indent..] : lines[i].TrimStart(' ', '\t');
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Renders the children of a Markdig <see cref="ContainerInline"/> into a WPF inline collection.
        /// </summary>
        /// <param name="target">The destination inline collection.</param>
        /// <param name="container">The source inline container; may be <c>null</c>.</param>
        /// <param name="context">The base URIs relative links and images are resolved against, if any.</param>
        private static void AddInlines(InlineCollection target, ContainerInline? container, RenderContext context)
        {
            if (container == null)
            {
                return;
            }

            var inline = container.FirstChild;

            while (inline != null)
            {
                // Custom HTML-style elements span several sibling inlines (opening tag, fallback
                // text, closing tag), so they are consumed here rather than in AddInline.
                if (inline is HtmlInline html && TryAddCustomElement(target, html, context, out var next))
                {
                    inline = next;
                    continue;
                }

                AddInline(target, inline, context);
                inline = inline.NextSibling;
            }
        }

        /// <summary>
        /// The custom element names (<c>shield</c>, <c>pill</c>) as a regex alternation.
        /// </summary>
        private const string CustomElementNames = "shield|pill";

        /// <summary>
        /// Matches an opening or self-closing custom element tag as captured by a Markdig
        /// <see cref="HtmlInline"/>.
        /// </summary>
        private static readonly Regex CustomOpenTagRegex = new($@"^<(?<name>{CustomElementNames})(?<attrs>\s[^>]*?)?\s*(?<self>/)?>$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Matches a closing custom element tag such as <c>&lt;/shield&gt;</c>.
        /// </summary>
        private static readonly Regex CustomCloseTagRegex = new($@"^</(?<name>{CustomElementNames})\s*>$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Matches a complete custom element (self-closing or with content) within raw HTML block text.
        /// </summary>
        private static readonly Regex CustomElementRegex = new($@"<(?<name>{CustomElementNames})(?<attrs>\s[^>]*?)?\s*(?:/>|>(?<inner>.*?)</\k<name>\s*>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        /// <summary>
        /// Matches a single <c>name="value"</c>, <c>name='value'</c>, or unquoted attribute.
        /// </summary>
        private static readonly Regex HtmlAttributeRegex = new(@"(?<name>[A-Za-z_:][-A-Za-z0-9_:.]*)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s""'=<>`]+))", RegexOptions.CultureInvariant);

        /// <summary>
        /// Renders a custom element (<c>&lt;shield&gt;</c> or <c>&lt;pill&gt;</c>) that starts at
        /// <paramref name="html"/>, consuming the sibling inlines up to its closing tag.
        /// </summary>
        /// <param name="target">The destination inline collection.</param>
        /// <param name="html">The candidate opening tag.</param>
        /// <param name="context">The active render context.</param>
        /// <param name="next">The inline to resume rendering from when an element was rendered.</param>
        /// <returns><c>true</c> when <paramref name="html"/> was a custom element tag and has been rendered.</returns>
        private static bool TryAddCustomElement(InlineCollection target, HtmlInline html, RenderContext context, out Markdig.Syntax.Inlines.Inline? next)
        {
            next = null;
            var match = CustomOpenTagRegex.Match(html.Tag ?? string.Empty);

            if (!match.Success)
            {
                return false;
            }

            string name = match.Groups["name"].Value;
            next = html.NextSibling;
            string inner = string.Empty;

            if (!match.Groups["self"].Success)
            {
                // Find the matching closing tag among the following siblings. When it is missing the
                // following content is left to render normally rather than swallowing the paragraph.
                var sb = new StringBuilder();

                for (var sibling = html.NextSibling; sibling != null; sibling = sibling.NextSibling)
                {
                    if (sibling is HtmlInline closing
                        && CustomCloseTagRegex.Match(closing.Tag ?? string.Empty) is { Success: true } closeMatch
                        && string.Equals(closeMatch.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
                    {
                        inner = sb.ToString();
                        next = closing.NextSibling;
                        break;
                    }

                    AppendPlainText(sb, sibling);
                }
            }

            target.Add(CreateCustomElementInline(name, ParseHtmlAttributes(match.Groups["attrs"].Value), inner, context));
            return true;
        }

        /// <summary>
        /// Renders an HTML block whose content is nothing but custom elements and whitespace as a
        /// paragraph of those elements.
        /// </summary>
        /// <returns>The rendered paragraph, or <c>null</c> when the block contains other markup.</returns>
        private static Paragraph? TryRenderCustomElementBlock(HtmlBlock html, RenderContext context)
        {
            string text = html.Lines.ToString();
            var matches = CustomElementRegex.Matches(text);

            if (matches.Count == 0 || !string.IsNullOrWhiteSpace(CustomElementRegex.Replace(text, string.Empty)))
            {
                return null;
            }

            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };

            for (int i = 0; i < matches.Count; i++)
            {
                if (i > 0)
                {
                    paragraph.Inlines.Add(new Run(" "));
                }

                string inner = System.Net.WebUtility.HtmlDecode(matches[i].Groups["inner"].Value).Trim();
                paragraph.Inlines.Add(CreateCustomElementInline(matches[i].Groups["name"].Value, ParseHtmlAttributes(matches[i].Groups["attrs"].Value), inner, context));
            }

            return paragraph;
        }

        /// <summary>
        /// Builds the inline for a custom element by name.
        /// </summary>
        /// <param name="name">The element name, matched case-insensitively.</param>
        /// <param name="attributes">The element's attributes, keyed case-insensitively.</param>
        /// <param name="inner">The plain text between the element's tags.</param>
        /// <param name="context">The active render context.</param>
        private static System.Windows.Documents.Inline CreateCustomElementInline(string name, Dictionary<string, string> attributes, string inner, RenderContext context)
        {
            return string.Equals(name, "pill", StringComparison.OrdinalIgnoreCase)
                ? CreatePillInline(attributes, inner, context)
                : CreateShieldInline(attributes, inner, context);
        }

        /// <summary>
        /// Builds the inline for a <c>&lt;pill background="..." foreground="..."&gt;text&lt;/pill&gt;</c>
        /// element: an embedded <see cref="Badge"/> showing the text between the tags, or that text
        /// alone when the render context cannot carry embedded controls. Optional <c>background</c>
        /// and <c>foreground</c> attributes accept hex (<c>#FEB806</c>) or named (<c>white</c>) colors.
        /// </summary>
        /// <param name="attributes">The element's attributes, keyed case-insensitively.</param>
        /// <param name="text">The text between the element's tags.</param>
        /// <param name="context">The active render context.</param>
        private static System.Windows.Documents.Inline CreatePillInline(Dictionary<string, string> attributes, string text, RenderContext context)
        {
            if (context.PlainCustomElements)
            {
                return new Run(text);
            }

            var badge = new Badge { Text = text };

            if (TryGetBrush(attributes, "background", out var background))
            {
                badge.Background = background;
            }

            if (TryGetBrush(attributes, "foreground", out var foreground))
            {
                badge.Foreground = foreground;
            }

            return new InlineUIContainer(badge)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
        }

        /// <summary>
        /// Builds the inline for a <c>&lt;shield label="..." value="..."&gt;fallback&lt;/shield&gt;</c>
        /// element: an embedded <see cref="Shield"/> control, or plain text when the render context
        /// cannot carry embedded controls. The fallback content between the tags exists only for
        /// Markdown renderers that do not understand the element; it is used as the shield's value
        /// when no <c>value</c> attribute is supplied. Optional <c>background</c> and
        /// <c>foreground</c> attributes accept hex (<c>#6395EE</c>) or named (<c>white</c>) colors.
        /// </summary>
        /// <param name="attributes">The element's attributes, keyed case-insensitively.</param>
        /// <param name="fallback">The plain text between the element's tags.</param>
        /// <param name="context">The active render context.</param>
        private static System.Windows.Documents.Inline CreateShieldInline(Dictionary<string, string> attributes, string fallback, RenderContext context)
        {
            attributes.TryGetValue("label", out var label);

            if (!attributes.TryGetValue("value", out var value))
            {
                value = fallback;
            }

            if (context.PlainCustomElements)
            {
                if (!string.IsNullOrEmpty(fallback))
                {
                    return new Run(fallback);
                }

                return new Run(string.IsNullOrEmpty(label) ? value : $"{label}: {value}");
            }

            var shield = new Shield
            {
                Label = label ?? string.Empty,
                Value = value
            };

            if (TryGetBrush(attributes, "background", out var background))
            {
                shield.ValueBackgroundBrush = background;
            }

            if (TryGetBrush(attributes, "foreground", out var foreground))
            {
                shield.ValueForegroundBrush = foreground;
            }

            return new InlineUIContainer(shield)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
        }

        /// <summary>
        /// Resolves a hex or named color attribute to a cached, frozen brush.
        /// </summary>
        /// <returns><c>false</c> when the attribute is missing or is not a valid color.</returns>
        private static bool TryGetBrush(Dictionary<string, string> attributes, string name, [NotNullWhen(true)] out SolidColorBrush? brush)
        {
            brush = null;

            if (!attributes.TryGetValue(name, out var text) || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            try
            {
                if (ColorConverter.ConvertFromString(text.Trim()) is Color color)
                {
                    brush = ColorPaletteCache.GetBrush(color);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Parses HTML attribute text into a case-insensitive name/value dictionary. Values are
        /// HTML-decoded so entities such as <c>&amp;amp;</c> render as their characters.
        /// </summary>
        private static Dictionary<string, string> ParseHtmlAttributes(string text)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in HtmlAttributeRegex.Matches(text))
            {
                attributes[match.Groups["name"].Value] = System.Net.WebUtility.HtmlDecode(match.Groups["value"].Value);
            }

            return attributes;
        }

        /// <summary>
        /// Appends the plain text of a Markdig inline (and its children) to <paramref name="sb"/>.
        /// </summary>
        private static void AppendPlainText(StringBuilder sb, Markdig.Syntax.Inlines.Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;

                case CodeInline code:
                    sb.Append(code.Content);
                    break;

                case HtmlEntityInline entity:
                    sb.Append(entity.Transcoded.ToString());
                    break;

                case LineBreakInline:
                    sb.Append(' ');
                    break;

                case ContainerInline container:
                    foreach (var child in container)
                    {
                        AppendPlainText(sb, child);
                    }

                    break;
            }
        }

        /// <summary>
        /// Renders a single Markdig <see cref="Inline"/> into the supplied WPF inline collection.
        /// </summary>
        private static void AddInline(InlineCollection target, Markdig.Syntax.Inlines.Inline inline, RenderContext context)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    target.Add(new Run(literal.Content.ToString()));
                    break;

                case EmphasisInline emphasis:
                    var span = new Span();

                    // Two delimiters (e.g. **text** / __text__) denote strong emphasis (bold);
                    // a single delimiter denotes emphasis (italic). Tilde denotes strikethrough.
                    if (emphasis.DelimiterChar == '~')
                    {
                        span.TextDecorations = TextDecorations.Strikethrough;
                    }
                    else if (emphasis.DelimiterCount >= 2)
                    {
                        span.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        span.FontStyle = FontStyles.Italic;
                    }

                    AddInlines(span.Inlines, emphasis, context);
                    target.Add(span);
                    break;

                case CodeInline code:
                    // Keep resource references on the containing Span rather than the text Run.
                    // TextRange.ApplyPropertyValue splits Runs when find highlights cover only part
                    // of an inline-code fragment. WPF cannot reliably detach a DynamicResource from
                    // a Run while performing that split and can throw from
                    // ResourceReferenceExpression.OnDetach. The child Run inherits both values and
                    // remains safe for transient search formatting.
                    var codeSpan = new Span(new Run(code.Content));
                    codeSpan.SetResourceReference(TextElement.FontFamilyProperty, MosaicTheme.MonospaceFontFamily);
                    codeSpan.SetResourceReference(TextElement.BackgroundProperty, MosaicTheme.ControlBackgroundLightBrush);
                    target.Add(codeSpan);
                    break;

                case LinkInline link:
                    AddLink(target, link, context);
                    break;

                case AutolinkInline autolink:
                    AddHyperlink(target, autolink.Url, new[] { (Markdig.Syntax.Inlines.Inline)new LiteralInline(autolink.Url) }, context);
                    break;

                case LineBreakInline lineBreak:
                    // A hard line break forces a new line; a soft break renders as a space.
                    target.Add(lineBreak.IsHard ? new LineBreak() : new Run(" "));
                    break;

                case ContainerInline containerInline:
                    AddInlines(target, containerInline, context);
                    break;
            }
        }

        /// <summary>
        /// The maximum rendered width, in device-independent pixels, for an image. Larger images are
        /// scaled down uniformly to fit while preserving their aspect ratio.
        /// </summary>
        private const double MaxImageWidth = 800;

        /// <summary>
        /// Renders a link inline, dispatching image links to <see cref="AddImage"/> and ordinary
        /// links to <see cref="AddHyperlink"/>.
        /// </summary>
        private static void AddLink(InlineCollection target, LinkInline link, RenderContext context)
        {
            if (link.IsImage)
            {
                AddImage(target, link, context);
                return;
            }

            AddHyperlink(target, link.Url, link, context);
        }

        /// <summary>
        /// Renders an image link from either a remote/local URL or an inline <c>data:</c> base64 URI.
        /// On failure the image is rendered as its alt text in italics so a broken or unsupported
        /// source never crashes the viewer.
        /// </summary>
        private static void AddImage(InlineCollection target, LinkInline link, RenderContext context)
        {
            string altText = GetAltText(link);

            try
            {
                var source = LoadImage(link.Url, context);

                if (source != null)
                {
                    var image = new Image
                    {
                        Source = source,
                        Stretch = Stretch.None,
                        ToolTip = string.IsNullOrEmpty(altText) ? link.Url : altText
                    };

                    // Constrain very large images while preserving their aspect ratio.
                    if (source.Width > MaxImageWidth)
                    {
                        image.Stretch = Stretch.Uniform;
                        image.MaxWidth = MaxImageWidth;
                    }

                    target.Add(new InlineUIContainer(image));
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            var span = new Span { FontStyle = FontStyles.Italic };
            span.Inlines.Add(new Run(string.IsNullOrEmpty(altText) ? (link.Url ?? "image") : altText));
            target.Add(span);
        }

        /// <summary>
        /// Loads an image from a <c>data:</c> base64 URI or an absolute URL (remote or local).
        /// </summary>
        /// <param name="url">The image source.</param>
        /// <param name="context">The base URIs a relative image source is resolved against, if any.</param>
        /// <returns>A frozen <see cref="BitmapImage"/>, or <c>null</c> when the source is unsupported.</returns>
        private static BitmapImage? LoadImage(string? url, RenderContext context)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return LoadDataUriImage(url);
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                return LoadImage(absolute);
            }

            // A relative source resolves against the document's own location first and then against
            // the configured storage folder, so `attachment/foo.png` works in both arrangements.
            foreach (var candidateBase in new[] { context.BaseUri, context.ImageBaseUri })
            {
                if (candidateBase == null || !Uri.TryCreate(candidateBase, url, out var resolved))
                {
                    continue;
                }

                // Only a missing local file is worth falling through for; anything else (a corrupt
                // image, a failed download) is a genuine error the caller should surface.
                if (resolved.IsFile && !File.Exists(resolved.LocalPath))
                {
                    continue;
                }

                return LoadImage(resolved);
            }

            return null;
        }

        /// <summary>
        /// Loads and freezes a bitmap from an absolute URI.
        /// </summary>
        private static BitmapImage LoadImage(Uri uri)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Decodes a base64 <c>data:</c> image URI (e.g. <c>data:image/png;base64,...</c>) into a bitmap.
        /// </summary>
        private static BitmapImage? LoadDataUriImage(string url)
        {
            int comma = url.IndexOf(',');

            if (comma < 0)
            {
                return null;
            }

            // The metadata segment sits between "data:" and the comma; only base64 payloads are supported.
            string meta = url.Substring(5, comma - 5);

            if (!meta.Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            byte[] bytes = Convert.FromBase64String(url.Substring(comma + 1));

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Extracts the plain-text alt label from an image link's child inlines.
        /// </summary>
        private static string GetAltText(LinkInline link)
        {
            var sb = new StringBuilder();

            foreach (var child in link)
            {
                if (child is LiteralInline literal)
                {
                    sb.Append(literal.Content.ToString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a navigable <see cref="Hyperlink"/> from the supplied label inlines and URL. The
        /// actual navigation is handled by the hosting control via the bubbling
        /// <see cref="Hyperlink.RequestNavigateEvent"/>. Relative URLs are resolved against the
        /// context's base URI when one is available; otherwise they are kept as relative URIs so the
        /// hosting control can decide how to resolve them. Event links (see
        /// <see cref="EventLinkPrefix"/>) are never resolved against the base URI so the hosting
        /// control receives the original <c>@Name?key=value</c> text.
        /// </summary>
        private static void AddHyperlink(InlineCollection target, string? url, IEnumerable<Markdig.Syntax.Inlines.Inline> labelInlines, RenderContext context)
        {
            var hyperlink = new WpfHyperlink
            {
                Cursor = Cursors.Hand
            };
            hyperlink.SetResourceReference(TextElement.ForegroundProperty, MosaicTheme.HyperLinkBrush);

            foreach (var labelInline in labelInlines)
            {
                AddInline(hyperlink.Inlines, labelInline, context);
            }

            if (hyperlink.Inlines.Count == 0 && !string.IsNullOrEmpty(url))
            {
                hyperlink.Inlines.Add(new Run(url));
            }

            if (IsEventLink(url))
            {
                hyperlink.NavigateUri = new Uri(url!, UriKind.Relative);
                hyperlink.ToolTip = url;
            }
            else if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                hyperlink.NavigateUri = uri;
                hyperlink.ToolTip = url;
            }
            else if (!string.IsNullOrEmpty(url) && !url.StartsWith("#", StringComparison.Ordinal) &&
                     context.BaseUri != null && Uri.TryCreate(context.BaseUri, url, out var resolved))
            {
                hyperlink.NavigateUri = resolved;
                hyperlink.ToolTip = url;
            }
            else if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Relative, out var relative))
            {
                hyperlink.NavigateUri = relative;
                hyperlink.ToolTip = url;
            }

            target.Add(hyperlink);
        }

        /// <summary>
        /// Determines whether a link URL is an application event link, that is one that starts with
        /// <see cref="EventLinkPrefix"/> followed by an event name.
        /// </summary>
        /// <param name="url">The raw link destination from the Markdown source.</param>
        /// <returns><c>true</c> when the URL is an event link; otherwise <c>false</c>.</returns>
        public static bool IsEventLink(string? url)
        {
            return url != null
                   && url.Length > EventLinkPrefix.Length
                   && url.StartsWith(EventLinkPrefix, StringComparison.Ordinal);
        }
    }
}
