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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Xps.Packaging;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies the <see cref="MarkdownViewer"/> export methods, which write the rendered document
    /// to Markdown, RTF, and XPS files.
    /// </summary>
    public class MarkdownViewerExportTests
    {
        private const string SampleMarkdown = "# Title\n\nSome **bold** text.\n\n```csharp\nvar x = 1;\nvar y = 2;\n```\n";

        [Fact]
        public void SaveAsMarkdownWritesTheSourceText()
        {
            RunSta(() =>
            {
                string path = TempFile(".md");

                try
                {
                    new MarkdownViewer { Markdown = SampleMarkdown }.SaveAsMarkdown(path);

                    Assert.Equal(SampleMarkdown, File.ReadAllText(path));
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void SaveAsRtfWritesTheTextAndCodeBlocks()
        {
            RunSta(() =>
            {
                string path = TempFile(".rtf");

                try
                {
                    new MarkdownViewer { Markdown = SampleMarkdown }.SaveAsRtf(path);
                    string rtf = File.ReadAllText(path);

                    Assert.StartsWith(@"{\rtf", rtf);
                    Assert.Contains("Title", rtf);
                    Assert.Contains("bold", rtf);

                    // Code blocks are embedded editors on screen; the export writes them as text so
                    // they survive in the RTF.
                    Assert.Contains("var x = 1;", rtf);
                    Assert.Contains("var y = 2;", rtf);

                    // The text uses the light theme's foreground (#333333) regardless of the active theme.
                    Assert.Contains(@"\red51\green51\blue51", rtf);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void SaveAsXpsWritesAPaginatedDocument()
        {
            RunSta(() =>
            {
                string path = TempFile(".xps");

                try
                {
                    new MarkdownViewer { Markdown = SampleMarkdown }.SaveAsXps(path);

                    using var xps = new XpsDocument(path, FileAccess.Read);
                    Assert.True(xps.GetFixedDocumentSequence().DocumentPaginator.PageCount > 0);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void SaveAsXpsReplacesAnExistingFile()
        {
            RunSta(() =>
            {
                string path = TempFile(".xps");

                try
                {
                    File.WriteAllText(path, "not an xps package");
                    new MarkdownViewer { Markdown = SampleMarkdown }.SaveAsXps(path);

                    using var xps = new XpsDocument(path, FileAccess.Read);
                    Assert.True(xps.GetFixedDocumentSequence().DocumentPaginator.PageCount > 0);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void SaveAsAndPrintMenusAreEnabledByDefault()
        {
            RunSta(() =>
            {
                var viewer = new MarkdownViewer();

                Assert.True(viewer.IsSaveAsMenuEnabled);
                Assert.True(viewer.IsPrintMenuEnabled);
                Assert.True(MarkdownViewer.SaveAsRtfCommand.CanExecute(null, viewer));
                Assert.True(MarkdownViewer.SaveAsXpsCommand.CanExecute(null, viewer));
                Assert.True(System.Windows.Input.ApplicationCommands.Print.CanExecute(null, viewer));
            });
        }

        [Fact]
        public void DisablingTheMenusDisablesTheirCommands()
        {
            RunSta(() =>
            {
                var viewer = new MarkdownViewer { IsSaveAsMenuEnabled = false, IsPrintMenuEnabled = false };

                Assert.False(MarkdownViewer.SaveAsRtfCommand.CanExecute(null, viewer));
                Assert.False(MarkdownViewer.SaveAsXpsCommand.CanExecute(null, viewer));
                Assert.False(System.Windows.Input.ApplicationCommands.Print.CanExecute(null, viewer));
            });
        }

        private static string TempFile(string extension)
        {
            return Path.Combine(Path.GetTempPath(), $"MarkdownViewerExport-{Guid.NewGuid():N}{extension}");
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
    }
}
