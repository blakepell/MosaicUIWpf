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
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mosaic.UI.Wpf.Themes;
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
        private readonly record struct RenderContext(Uri? BaseUri, Uri? ImageBaseUri);

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
            return Render(markdown, new RenderContext(baseUri, imageBaseUri));
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

            var parsed = Markdown.Parse(markdown ?? string.Empty, Pipeline);

            foreach (var block in parsed)
            {
                var rendered = RenderBlock((MarkdigBlock)block, context);

                if (rendered != null)
                {
                    document.Blocks.Add(rendered);
                }
            }

            return document;
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
                    return RenderCodeBlock(code);
                case ThematicBreakBlock:
                    return RenderThematicBreak();
                case HtmlBlock html:
                    return RenderHtmlBlock(html);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Renders a heading block, scaling the font size by heading level.
        /// </summary>
        private static WpfBlock RenderHeading(HeadingBlock heading, RenderContext context)
        {
            var paragraph = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                FontSize = HeadingFontSize(heading.Level),
                Margin = new Thickness(0, heading.Level <= 2 ? 12 : 8, 0, 4)
            };

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
            if (section.Blocks.FirstBlock is { } firstBlock)
            {
                var margin = firstBlock.Margin;
                firstBlock.Margin = new Thickness(margin.Left, 0, margin.Right, margin.Bottom);
            }

            if (section.Blocks.LastBlock is { } lastBlock)
            {
                var margin = lastBlock.Margin;
                lastBlock.Margin = new Thickness(margin.Left, margin.Top, margin.Right, 0);
            }

            return section;
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
        private static WpfBlock RenderCodeBlock(CodeBlock code)
        {
            string text = GetCodeBlockText(code);

            if (text.Contains('\n'))
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
        /// Renders a raw HTML block as plain, monospace text rather than interpreting the markup.
        /// </summary>
        private static WpfBlock RenderHtmlBlock(HtmlBlock html)
        {
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

            foreach (var inline in container)
            {
                AddInline(target, inline, context);
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
                    var codeRun = new Run(code.Content);
                    codeRun.SetResourceReference(TextElement.FontFamilyProperty, MosaicTheme.MonospaceFontFamily);
                    codeRun.SetResourceReference(TextElement.BackgroundProperty, MosaicTheme.ControlBackgroundLightBrush);
                    target.Add(codeRun);
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
        /// hosting control can decide how to resolve them.
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

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
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
    }
}
