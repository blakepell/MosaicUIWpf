/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies that <see cref="MarkdownFlowDocumentRenderer"/> resolves relative image paths against the
    /// supplied base URI (the viewer supplies its <c>StorageFolder</c> here).
    /// </summary>
    public class MarkdownFlowDocumentRendererTests
    {
        [Theory]
        [InlineData("sample.png", "")]
        [InlineData("attachment/sample.png", "attachment")]
        public void RelativeImagePathResolvesAgainstBaseUri(string markdownPath, string subFolder)
        {
            RunSta(() => AssertRelativeImageResolves(markdownPath, subFolder));
        }

        private static void AssertRelativeImageResolves(string markdownPath, string subFolder)
        {
            string root = Path.Combine(Path.GetTempPath(), "mosaic-md-" + Guid.NewGuid().ToString("N"));
            string folder = string.IsNullOrEmpty(subFolder) ? root : Path.Combine(root, subFolder);
            Directory.CreateDirectory(folder);

            try
            {
                File.WriteAllBytes(Path.Combine(folder, "sample.png"), OnePixelPng());

                var baseUri = new Uri(root + Path.DirectorySeparatorChar, UriKind.Absolute);
                var document = MarkdownFlowDocumentRenderer.Render($"![Screenshot]({markdownPath})", baseUri);

                Assert.Single(FindImages(document));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Theory]
        [InlineData("sample.png", "")]
        [InlineData("attachment/sample.png", "attachment")]
        public void ViewerResolvesRelativeImagesAgainstStorageFolder(string markdownPath, string subFolder)
        {
            RunSta(() =>
            {
                string root = Path.Combine(Path.GetTempPath(), "mosaic-md-" + Guid.NewGuid().ToString("N"));
                string folder = string.IsNullOrEmpty(subFolder) ? root : Path.Combine(root, subFolder);
                Directory.CreateDirectory(folder);

                try
                {
                    File.WriteAllBytes(Path.Combine(folder, "sample.png"), OnePixelPng());

                    var viewer = new MarkdownViewer
                    {
                        StorageFolder = root,
                        Markdown = $"![Screenshot]({markdownPath})"
                    };

                    Assert.Single(FindImages(Realize(viewer).Document));
                }
                finally
                {
                    Directory.Delete(root, true);
                }
            });
        }

        [Fact]
        public void ViewerFallsBackToStorageFolderForImagesMissingNextToTheSourceDocument()
        {
            RunSta(() =>
            {
                string root = Path.Combine(Path.GetTempPath(), "mosaic-md-" + Guid.NewGuid().ToString("N"));
                string documentFolder = Path.Combine(root, "documents");
                string storageFolder = Path.Combine(root, "storage", "attachment");
                Directory.CreateDirectory(documentFolder);
                Directory.CreateDirectory(storageFolder);

                try
                {
                    File.WriteAllBytes(Path.Combine(storageFolder, "sample.png"), OnePixelPng());

                    string documentPath = Path.Combine(documentFolder, "note.md");
                    File.WriteAllText(documentPath, "![Screenshot](attachment/sample.png)");

                    var viewer = new MarkdownViewer
                    {
                        StorageFolder = Path.Combine(root, "storage"),
                        Source = new Uri(documentPath, UriKind.Absolute)
                    };

                    Assert.Single(FindImages(Realize(viewer).Document));
                }
                finally
                {
                    Directory.Delete(root, true);
                }
            });
        }

        [Theory]
        [InlineData("```csharp")]
        [InlineData("``` csharp")]
        [InlineData("```cs")]
        [InlineData("```C#")]
        public void FencedCodeBlockWithNamedLanguageRendersInAHighlightedSyntaxEditor(string fence)
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render(
                    $"{fence}\npublic static void Main()\n{{\n}}\n```");

                var editor = Assert.Single(FindSyntaxEditors(document));

                Assert.Equal(SyntaxLanguage.CSharp, editor.Language);
                Assert.True(editor.IsReadOnly);
                Assert.Contains("public static void Main()", editor.Text);
            });
        }

        [Fact]
        public void FencedCodeBlockWithoutALanguageStillRendersInASyntaxEditor()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("```\nline one\nline two\n```");

                var editor = Assert.Single(FindSyntaxEditors(document));

                Assert.Equal(SyntaxLanguage.None, editor.Language);
                Assert.Equal("line one\nline two", editor.Text);
            });
        }

        [Fact]
        public void HeadingUsesDefaultBottomSpacingWhenNoneIsSupplied()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("# Title\n\nBody");
                var heading = document.Blocks.OfType<Paragraph>().First();

                Assert.Equal(MarkdownFlowDocumentRenderer.DefaultHeadingBottomSpacing, heading.Margin.Bottom);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(24.5)]
        public void HeadingHonorsSuppliedBottomSpacing(double spacing)
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("## Title\n\nBody", null, null, spacing);
                var heading = document.Blocks.OfType<Paragraph>().First();

                Assert.Equal(spacing, heading.Margin.Bottom);
            });
        }

        [Fact]
        public void EventLinkKeepsItsOriginalTextEvenWhenABaseUriIsSupplied()
        {
            RunSta(() =>
            {
                var baseUri = new Uri("https://example.com/docs/index.md");
                var document = MarkdownFlowDocumentRenderer.Render("[Articles](@ShowArticle?keyword=bpell)", baseUri);
                var hyperlink = document.Blocks.OfType<Paragraph>().Single().Inlines.OfType<System.Windows.Documents.Hyperlink>().Single();

                Assert.NotNull(hyperlink.NavigateUri);
                Assert.False(hyperlink.NavigateUri!.IsAbsoluteUri);
                Assert.Equal("@ShowArticle?keyword=bpell", hyperlink.NavigateUri.OriginalString);
            });
        }

        [Fact]
        public void HeadingsCarryGeneratedExplicitAndDeduplicatedIds()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("## Event Links\n\n## Getting Started {#start}\n\n## Notes\n\n## Notes\n\nBody");
                var ids = document.Blocks.OfType<Paragraph>().Select(MarkdownFlowDocumentRenderer.GetHeadingId).ToArray();

                Assert.Equal(new[] { "event-links", "start", "notes", "notes-1", null }, ids);
            });
        }

        [Fact]
        public void AnchorLinkStaysRelativeEvenWhenABaseUriIsSupplied()
        {
            RunSta(() =>
            {
                var baseUri = new Uri("https://example.com/docs/index.md");
                var document = MarkdownFlowDocumentRenderer.Render("[Jump](#event-links)", baseUri);
                var hyperlink = document.Blocks.OfType<Paragraph>().Single().Inlines.OfType<System.Windows.Documents.Hyperlink>().Single();

                Assert.NotNull(hyperlink.NavigateUri);
                Assert.False(hyperlink.NavigateUri!.IsAbsoluteUri);
                Assert.Equal("#event-links", hyperlink.NavigateUri.OriginalString);
            });
        }

        [Fact]
        public void ScrollToAnchorScrollsAFarHeadingIntoView()
        {
            RunSta(() =>
            {
                string filler = string.Concat(Enumerable.Repeat("Filler paragraph.\n\n", 80));
                var viewer = new MarkdownViewer { Markdown = $"# Top\n\n{filler}## Far Away\n\nEnd" };
                var richTextBox = Realize(viewer);
                richTextBox.UpdateLayout();

                Assert.False(viewer.ScrollToAnchor("#missing"));
                Assert.True(viewer.ScrollToAnchor("#FAR-AWAY"));

                richTextBox.UpdateLayout();
                Assert.True(richTextBox.VerticalOffset > 0);
            });
        }

        [Fact]
        public void EventLinkParsesNameAndDecodedParameters()
        {
            Assert.True(MarkdownEventRaisedEventArgs.TryParse("@ShowArticle?keyword=bpell&title=Hello%20World&tag=a+b", out var name, out var parameters));

            Assert.Equal("ShowArticle", name);
            Assert.Equal(3, parameters.Count);
            Assert.Equal("bpell", parameters["keyword"]);
            Assert.Equal("Hello World", parameters["title"]);
            Assert.Equal("a b", parameters["tag"]);
            Assert.Equal("bpell", parameters["KEYWORD"]);
        }

        [Theory]
        [InlineData("@Refresh")]
        [InlineData("@Refresh?")]
        public void EventLinkWithoutParametersYieldsAnEmptyDictionary(string link)
        {
            Assert.True(MarkdownEventRaisedEventArgs.TryParse(link, out var name, out var parameters));

            Assert.Equal("Refresh", name);
            Assert.Empty(parameters);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("@")]
        [InlineData("@?keyword=bpell")]
        [InlineData("https://example.com/@user")]
        [InlineData("docs/@readme.md")]
        public void NonEventLinksAreRejected(string? link)
        {
            Assert.False(MarkdownEventRaisedEventArgs.TryParse(link, out _, out _));
        }

        [Fact]
        public void SingleLineCodeBlockStaysAPlainParagraph()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("```csharp\nvar x = 1;\n```");

                Assert.Empty(FindSyntaxEditors(document));
                Assert.Single(document.Blocks.OfType<Paragraph>());
            });
        }

        [Fact]
        public void EmbeddedSyntaxEditorIsInteractiveInsideTheViewer()
        {
            RunSta(() =>
            {
                var viewer = new MarkdownViewer { Markdown = "```csharp\nvar a = 1;\nvar b = 2;\n```" };
                var richTextBox = Realize(viewer);
                richTextBox.UpdateLayout();

                var editor = Assert.Single(FindSyntaxEditors(richTextBox.Document));

                // Elements embedded in a rich text box are disabled unless the document is enabled, and a
                // disabled editor cannot be selected, has no context menu, and paints its text with the
                // theme's disabled foreground.
                Assert.True(richTextBox.IsDocumentEnabled);
                Assert.True(editor.IsEnabled);
                Assert.True(editor.ShowLineNumbers);
                Assert.NotNull(editor.ContextMenu);
            });
        }

        [Fact]
        public void EmbeddedSyntaxEditorStretchesToTheHeightOfItsDocument()
        {
            RunSta(() =>
            {
                double shortHeight = MeasureCodeBlockHeight(4);
                double tallHeight = MeasureCodeBlockHeight(40);

                Assert.True(shortHeight > 0, "The embedded editor measured to zero height.");

                // The editor is sized by its content rather than clipped to a scrolling viewport, so ten
                // times the lines is roughly ten times the height.
                Assert.True(
                    tallHeight > shortHeight * 5,
                    $"Expected the taller block to grow with its content; measured {shortHeight} and {tallHeight}.");
            });
        }

        [Fact]
        public void EmbeddedSyntaxEditorRendersTallerThanTheViewerRatherThanScrollingItself()
        {
            RunSta(() =>
            {
                string code = string.Join("\n", Enumerable.Range(1, 40).Select(i => $"var x{i} = {i};"));
                var viewer = new MarkdownViewer { Markdown = $"```csharp\n{code}\n```" };
                var richTextBox = Realize(viewer);
                richTextBox.UpdateLayout();

                var editor = Assert.Single(FindSyntaxEditors(richTextBox.Document));

                // The viewer is laid out 400 device-independent pixels tall; the code block is longer
                // than that, so an editor that scrolled internally would stop at the viewport height.
                Assert.True(
                    editor.ActualHeight > richTextBox.ActualHeight,
                    $"Expected the editor to render its whole document; it was {editor.ActualHeight} tall inside a {richTextBox.ActualHeight} tall viewer.");
            });
        }

        [Fact]
        public void ImplicitHostStylesDoNotRestyleTheViewerSurface()
        {
            RunSta(() =>
            {
                // Another UI framework's implicit styles: a RichTextBox template that paints its own
                // background, and a FlowDocument style that recolors the text.
                var foreignTemplate = new ControlTemplate(typeof(RichTextBox))
                {
                    VisualTree = new FrameworkElementFactory(typeof(Border), "ForeignBorder")
                };
                foreignTemplate.VisualTree.SetValue(Border.BackgroundProperty, Brushes.Black);

                var richTextBoxStyle = new Style(typeof(RichTextBox));
                richTextBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, foreignTemplate));
                richTextBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Black));

                var documentStyle = new Style(typeof(FlowDocument));
                documentStyle.Setters.Add(new Setter(FlowDocument.BackgroundProperty, Brushes.Black));
                documentStyle.Setters.Add(new Setter(FlowDocument.ForegroundProperty, Brushes.Red));

                var viewer = new MarkdownViewer { Markdown = "# Title\n\nBody text.", Foreground = Brushes.White };
                var host = new Border { Child = viewer };
                host.Resources.Add(typeof(RichTextBox), richTextBoxStyle);
                host.Resources.Add(typeof(FlowDocument), documentStyle);

                var richTextBox = Realize(viewer);
                richTextBox.ApplyTemplate();

                Assert.NotSame(richTextBoxStyle, richTextBox.Style);
                Assert.Null(richTextBox.Template.FindName("ForeignBorder", richTextBox));
                Assert.Equal(Colors.Transparent, ((SolidColorBrush)richTextBox.Background).Color);
                Assert.Equal(Colors.Transparent, ((SolidColorBrush)richTextBox.Document.Background).Color);
                Assert.Same(Brushes.White, richTextBox.Document.Foreground);

                // An override by the application after rendering still reaches the document.
                viewer.Foreground = Brushes.Yellow;
                Assert.Same(Brushes.Yellow, richTextBox.Document.Foreground);
            });
        }

        [Fact]
        public void ShieldElementRendersShieldInsteadOfFallbackText()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("Release <shield label=\"Version\" value=\"1.0\">v1.0</shield> is out.");

                var shield = Assert.Single(FindShields(document));
                Assert.Equal("Version", shield.Label);
                Assert.Equal("1.0", shield.Value);
                Assert.DoesNotContain("v1.0", GetText(document));
                Assert.Contains("Release", GetText(document));
                Assert.Contains("is out.", GetText(document));
            });
        }

        [Fact]
        public void ShieldElementAtStartOfLineIsRenderedInline()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("<shield label=\"Version\" value=\"1.0\">v1.0</shield>");

                Assert.Single(FindShields(document));
                Assert.DoesNotContain("<shield", GetText(document));
            });
        }

        [Fact]
        public void ShieldElementAppliesHexAndNamedColors()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render(
                    "<shield label=\"Version\" value=\"1.0\" background=\"#6395EE\" foreground=\"white\">v1.0</shield>");

                var shield = Assert.Single(FindShields(document));
                Assert.Equal(Color.FromRgb(0x63, 0x95, 0xEE), ((SolidColorBrush)shield.ValueBackgroundBrush).Color);
                Assert.Equal(Colors.White, ((SolidColorBrush)shield.ValueForegroundBrush).Color);
                Assert.True(shield.ValueBackgroundBrush.IsFrozen);
            });
        }

        [Fact]
        public void ShieldElementWithInvalidColorKeepsDefaults()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render(
                    "<shield label=\"Build\" value=\"ok\" background=\"not-a-color\">ok</shield>");

                var shield = Assert.Single(FindShields(document));
                Assert.Same(new Shield().ValueBackgroundBrush, shield.ValueBackgroundBrush);
            });
        }

        [Fact]
        public void ShieldElementWithoutValueUsesFallbackText()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("<shield label=\"Version\">v2.0</shield>");

                Assert.Equal("v2.0", Assert.Single(FindShields(document)).Value);
            });
        }

        [Fact]
        public void SelfClosingShieldOnItsOwnLineRendersShield()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("<shield label=\"License\" value=\"MIT\" />");

                var shield = Assert.Single(FindShields(document));
                Assert.Equal("MIT", shield.Value);
                Assert.DoesNotContain("<shield", GetText(document));
            });
        }

        [Fact]
        public void UnclosedShieldLeavesFollowingTextIntact()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.Render("Before <shield label=\"Version\" value=\"1.0\"> after");

                Assert.Single(FindShields(document));
                Assert.Contains("after", GetText(document));
            });
        }

        [Fact]
        public void ExportRendersShieldAsFallbackText()
        {
            RunSta(() =>
            {
                var document = MarkdownFlowDocumentRenderer.RenderForExport(
                    "<shield label=\"Version\" value=\"1.0\">v1.0</shield>", null, null, MarkdownFlowDocumentRenderer.DefaultHeadingBottomSpacing);

                Assert.Empty(FindShields(document));
                Assert.Contains("v1.0", GetText(document));
            });
        }

        private static Shield[] FindShields(FlowDocument document)
        {
            return document.Blocks
                .OfType<Paragraph>()
                .SelectMany(p => p.Inlines)
                .OfType<InlineUIContainer>()
                .Select(c => c.Child)
                .OfType<Shield>()
                .ToArray();
        }

        private static string GetText(FlowDocument document)
        {
            return new TextRange(document.ContentStart, document.ContentEnd).Text;
        }

        /// <summary>
        /// Renders a fenced code block of the requested line count and measures the embedded editor with
        /// unbounded height, mirroring how the hosting rich text box lays the document out.
        /// </summary>
        private static double MeasureCodeBlockHeight(int lineCount)
        {
            string code = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"var x{i} = {i};"));
            var document = MarkdownFlowDocumentRenderer.Render($"```csharp\n{code}\n```");
            var editor = Assert.Single(FindSyntaxEditors(document));

            editor.Measure(new Size(600, double.PositiveInfinity));

            return editor.DesiredSize.Height;
        }

        private static SyntaxEditor[] FindSyntaxEditors(FlowDocument document)
        {
            return document.Blocks
                .OfType<BlockUIContainer>()
                .Select(c => c.Child)
                .Select(child => child is Border border ? border.Child : child)
                .OfType<SyntaxEditor>()
                .ToArray();
        }

        /// <summary>
        /// Applies the viewer's shipped style, realizes its template, and returns the hosted rich text box.
        /// The default style is not resolved automatically outside of a hosting application, so the
        /// control's dictionary is loaded explicitly here.
        /// </summary>
        private static RichTextBox Realize(MarkdownViewer viewer)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/MarkdownViewer/MarkdownViewer.xaml")
            };

            viewer.Style = (Style)dictionary[typeof(MarkdownViewer)];
            viewer.Measure(new Size(600, 400));
            viewer.Arrange(new Rect(0, 0, 600, 400));
            viewer.ApplyTemplate();

            return (RichTextBox)viewer.Template.FindName("PART_RichTextBox", viewer);
        }

        /// <summary>
        /// Runs the test body on an STA thread, which WPF visuals require.
        /// </summary>
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static Image[] FindImages(FlowDocument document)
        {
            return document.Blocks
                .OfType<Paragraph>()
                .SelectMany(p => p.Inlines)
                .OfType<InlineUIContainer>()
                .Select(c => c.Child)
                .OfType<Image>()
                .ToArray();
        }

        /// <summary>
        /// The bytes of a 1x1 transparent PNG.
        /// </summary>
        private static byte[] OnePixelPng()
        {
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        }
    }
}
