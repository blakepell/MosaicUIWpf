/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
