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
        /// Renders the supplied Markdown text into a <see cref="FlowDocument"/>.
        /// </summary>
        /// <param name="markdown">The Markdown source. A <c>null</c> value is treated as an empty string.</param>
        /// <returns>A <see cref="FlowDocument"/> representing the parsed Markdown.</returns>
        public static FlowDocument Render(string? markdown)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(0)
            };

            var parsed = Markdown.Parse(markdown ?? string.Empty, Pipeline);

            foreach (var block in parsed)
            {
                var rendered = RenderBlock((MarkdigBlock)block);

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
        /// <returns>The rendered block, or <c>null</c> when the block type is unsupported.</returns>
        private static WpfBlock? RenderBlock(MarkdigBlock block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    return RenderHeading(heading);
                case ParagraphBlock paragraph:
                    return RenderParagraph(paragraph);
                case ListBlock list:
                    return RenderList(list);
                case QuoteBlock quote:
                    return RenderQuote(quote);
                case MarkdigTable table:
                    return RenderTable(table);
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
        private static WpfBlock RenderHeading(HeadingBlock heading)
        {
            var paragraph = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                FontSize = HeadingFontSize(heading.Level),
                Margin = new Thickness(0, heading.Level <= 2 ? 12 : 8, 0, 4)
            };

            AddInlines(paragraph.Inlines, heading.Inline);
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
        private static WpfBlock RenderParagraph(ParagraphBlock paragraph)
        {
            var result = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            AddInlines(result.Inlines, paragraph.Inline);
            return result;
        }

        /// <summary>
        /// Renders a bulleted or numbered list, including nested lists.
        /// </summary>
        private static WpfBlock RenderList(ListBlock listBlock)
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
                    var rendered = RenderBlock(child);

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
        private static WpfBlock RenderQuote(QuoteBlock quote)
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
                var rendered = RenderBlock(child);

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
        private static WpfBlock RenderTable(MarkdigTable table)
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
                            AddInlines(paragraph.Inlines, cellParagraph.Inline);
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
        /// Renders a fenced or indented code block as a monospace, shaded paragraph.
        /// </summary>
        private static WpfBlock RenderCodeBlock(CodeBlock code)
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

            var lines = code.Lines.Lines;
            int count = code.Lines.Count;
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
        private static void AddInlines(InlineCollection target, ContainerInline? container)
        {
            if (container == null)
            {
                return;
            }

            foreach (var inline in container)
            {
                AddInline(target, inline);
            }
        }

        /// <summary>
        /// Renders a single Markdig <see cref="Inline"/> into the supplied WPF inline collection.
        /// </summary>
        private static void AddInline(InlineCollection target, Markdig.Syntax.Inlines.Inline inline)
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

                    AddInlines(span.Inlines, emphasis);
                    target.Add(span);
                    break;

                case CodeInline code:
                    var codeRun = new Run(code.Content);
                    codeRun.SetResourceReference(TextElement.FontFamilyProperty, MosaicTheme.MonospaceFontFamily);
                    codeRun.SetResourceReference(TextElement.BackgroundProperty, MosaicTheme.ControlBackgroundLightBrush);
                    target.Add(codeRun);
                    break;

                case LinkInline link:
                    AddLink(target, link);
                    break;

                case AutolinkInline autolink:
                    AddHyperlink(target, autolink.Url, new[] { (Markdig.Syntax.Inlines.Inline)new LiteralInline(autolink.Url) });
                    break;

                case LineBreakInline lineBreak:
                    // A hard line break forces a new line; a soft break renders as a space.
                    target.Add(lineBreak.IsHard ? new LineBreak() : new Run(" "));
                    break;

                case ContainerInline containerInline:
                    AddInlines(target, containerInline);
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
        private static void AddLink(InlineCollection target, LinkInline link)
        {
            if (link.IsImage)
            {
                AddImage(target, link);
                return;
            }

            AddHyperlink(target, link.Url, link);
        }

        /// <summary>
        /// Renders an image link from either a remote/local URL or an inline <c>data:</c> base64 URI.
        /// On failure the image is rendered as its alt text in italics so a broken or unsupported
        /// source never crashes the viewer.
        /// </summary>
        private static void AddImage(InlineCollection target, LinkInline link)
        {
            string altText = GetAltText(link);

            try
            {
                var source = LoadImage(link.Url);

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
        /// <returns>A frozen <see cref="BitmapImage"/>, or <c>null</c> when the source is unsupported.</returns>
        private static BitmapImage? LoadImage(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return LoadDataUriImage(url);
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

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
        /// <see cref="Hyperlink.RequestNavigateEvent"/>.
        /// </summary>
        private static void AddHyperlink(InlineCollection target, string? url, IEnumerable<Markdig.Syntax.Inlines.Inline> labelInlines)
        {
            var hyperlink = new WpfHyperlink();
            hyperlink.SetResourceReference(TextElement.ForegroundProperty, MosaicTheme.HyperLinkBrush);

            foreach (var labelInline in labelInlines)
            {
                AddInline(hyperlink.Inlines, labelInline);
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

            target.Add(hyperlink);
        }
    }
}
