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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies the <see cref="MarkdownViewer"/> find bar, which searches the rendered flow document
    /// as well as the code block editors embedded in it.
    /// </summary>
    public class MarkdownViewerFindTests
    {
        [Fact]
        public void FindReportsEveryMatchInTheDocument()
        {
            RunSta(() =>
            {
                var viewer = Open("Hello world\n\nhello again", "hello");

                Assert.Equal("1 of 2", viewer.FindStatusText);
            });
        }

        [Fact]
        public void MatchCaseNarrowsTheResults()
        {
            RunSta(() =>
            {
                var viewer = Open("Hello world\n\nhello again", "hello");
                viewer.FindMatchCase = true;

                Assert.Equal("1 of 1", viewer.FindStatusText);
            });
        }

        [Fact]
        public void WholeWordsNarrowsTheResults()
        {
            RunSta(() =>
            {
                var viewer = Open("catalog\n\nthe cat", "cat");
                Assert.Equal("1 of 2", viewer.FindStatusText);

                viewer.FindWholeWords = true;
                Assert.Equal("1 of 1", viewer.FindStatusText);
            });
        }

        [Fact]
        public void RegularExpressionsAreUsedOnlyWhenRequested()
        {
            RunSta(() =>
            {
                var viewer = Open("one 42 two 7", @"\d+");
                Assert.Equal("No results", viewer.FindStatusText);

                viewer.FindUseRegex = true;
                Assert.Equal("1 of 2", viewer.FindStatusText);
            });
        }

        [Fact]
        public void AnInvalidRegularExpressionReportsNoResultsRatherThanThrowing()
        {
            RunSta(() =>
            {
                var viewer = Open("one two", "(unclosed");
                viewer.FindUseRegex = true;

                Assert.Equal("No results", viewer.FindStatusText);
            });
        }

        [Fact]
        public void APhraseIsFoundAcrossInlineFormatting()
        {
            RunSta(() =>
            {
                // The renderer splits this paragraph into three inlines; the search flattens them so
                // the phrase still matches.
                var viewer = Open("This is **bold** text.", "is bold text");

                Assert.Equal("1 of 1", viewer.FindStatusText);
            });
        }

        [Fact]
        public void APhraseDoesNotMatchAcrossTwoBlocks()
        {
            RunSta(() =>
            {
                var viewer = Open("First paragraph\n\nSecond paragraph", "paragraph Second");

                Assert.Equal("No results", viewer.FindStatusText);
            });
        }

        [Fact]
        public void CodeBlockEditorsAreSearchedAndTheMatchIsSelectedInTheEditor()
        {
            RunSta(() =>
            {
                var viewer = Open(
                    "Some prose.\n\n```csharp\npublic static void Main()\n{\n    Console.WriteLine(\"hi\");\n}\n```",
                    "Console");

                Assert.Equal("1 of 1", viewer.FindStatusText);

                var editor = Assert.Single(FindSyntaxEditors(RichTextBoxOf(viewer).Document));

                Assert.Equal("Console", editor.SelectedText);

                // Closing the bar has to clear the highlight it left inside the code block too.
                viewer.CloseFindPanel();

                Assert.Equal(0, editor.SelectionLength);
            });
        }

        [Fact]
        public void FindNextWrapsAroundTheDocument()
        {
            RunSta(() =>
            {
                var viewer = Open("alpha beta alpha", "alpha");
                Assert.Equal("1 of 2", viewer.FindStatusText);

                viewer.FindNext(true);
                Assert.Equal("2 of 2", viewer.FindStatusText);

                viewer.FindNext(true);
                Assert.Equal("1 of 2", viewer.FindStatusText);

                viewer.FindNext(false);
                Assert.Equal("2 of 2", viewer.FindStatusText);
            });
        }

        [Fact]
        public void ClosingTheFindPanelClearsTheSearchState()
        {
            RunSta(() =>
            {
                var viewer = Open("alpha beta alpha", "alpha");
                Assert.Equal("1 of 2", viewer.FindStatusText);

                viewer.CloseFindPanel();

                Assert.False(viewer.IsFindPanelOpen);
                Assert.Equal(string.Empty, viewer.FindStatusText);
            });
        }

        [Fact]
        public void ShowFindPanelSeedsTheFindTextWithTheCurrentSelection()
        {
            RunSta(() =>
            {
                var viewer = new MarkdownViewer { Markdown = "alpha beta alpha" };
                var richTextBox = Realize(viewer);
                var start = richTextBox.Document.ContentStart.GetPositionAtOffset(1, LogicalDirection.Forward)!;
                richTextBox.Selection.Select(start, start.GetPositionAtOffset(9, LogicalDirection.Forward)!);

                viewer.ShowFindPanel();

                Assert.True(viewer.IsFindPanelOpen);
                Assert.False(string.IsNullOrWhiteSpace(viewer.FindText));
                Assert.Contains(viewer.FindText, "alpha beta alpha");
            });
        }

        [Fact]
        public void ChangingTheDocumentDoesNotCarryStaleMatchesForward()
        {
            RunSta(() =>
            {
                var viewer = Open("alpha beta alpha", "alpha");
                Assert.Equal("1 of 2", viewer.FindStatusText);

                viewer.Markdown = "nothing to see here";

                Assert.Equal("No results", viewer.FindStatusText);
            });
        }

        [Fact]
        public void TheSelectedMatchIsPaintedDifferentlyFromTheOtherMatches()
        {
            RunSta(() =>
            {
                var viewer = Open("alpha beta alpha gamma alpha", "alpha");
                var document = RichTextBoxOf(viewer).Document;

                // The document's own selection marks the current match too, but it is drawn with
                // the pale inactive highlight while the find bar holds focus, so the current match
                // has to carry a background of its own.
                var highlights = HighlightedRuns(document);
                Assert.Equal(3, highlights.Count);

                var current = Assert.Single(OddOneOut(highlights));

                viewer.FindNext(true);

                highlights = HighlightedRuns(document);
                Assert.Equal(3, highlights.Count);

                var moved = Assert.Single(OddOneOut(highlights));
                Assert.NotSame(current, moved);
            });
        }

        [Fact]
        public void CodeBlockEditorsAreNotLeftReadOnlyByTheHostingDocument()
        {
            RunSta(() =>
            {
                var viewer = new MarkdownViewer { Markdown = "```csharp\nvar a = 1;\nvar b = 2;\n```" };
                var richTextBox = Realize(viewer);
                var editor = Assert.Single(FindSyntaxEditors(richTextBox.Document));

                // TextBoxBase.IsReadOnly inherits, so the read-only rich text box would otherwise
                // push its read-only state onto every text box inside the code block, including the
                // one in the editor's search panel, which then silently refuses input.
                Assert.False((bool)editor.GetValue(TextBoxBase.IsReadOnlyProperty));

                // The editor's own read-only state is AvalonEdit's separate property.
                Assert.True(editor.IsReadOnly);
            });
        }

        /// <summary>
        /// Returns every run in the document that carries a find highlight.
        /// </summary>
        private static List<Run> HighlightedRuns(DependencyObject node, List<Run>? found = null)
        {
            found ??= new List<Run>();

            if (node is Run { Background: SolidColorBrush } run)
            {
                found.Add(run);
            }

            foreach (object child in LogicalTreeHelper.GetChildren(node))
            {
                if (child is DependencyObject dependencyObject)
                {
                    HighlightedRuns(dependencyObject, found);
                }
            }

            return found;
        }

        /// <summary>
        /// Returns the runs whose highlight color is unique among the supplied runs, which is the
        /// selected match.
        /// </summary>
        private static List<Run> OddOneOut(List<Run> runs)
        {
            return runs
                .GroupBy(r => ((SolidColorBrush)r.Background).Color)
                .Where(g => g.Count() == 1)
                .SelectMany(g => g)
                .ToList();
        }

        /// <summary>
        /// Creates a realized viewer showing the supplied Markdown with the find bar open and the
        /// supplied text searched for.
        /// </summary>
        private static MarkdownViewer Open(string markdown, string findText)
        {
            var viewer = new MarkdownViewer { Markdown = markdown };
            Realize(viewer);

            viewer.IsFindPanelOpen = true;
            viewer.FindText = findText;

            return viewer;
        }

        private static RichTextBox RichTextBoxOf(MarkdownViewer viewer)
        {
            return (RichTextBox)viewer.Template.FindName("PART_RichTextBox", viewer);
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
    }
}
