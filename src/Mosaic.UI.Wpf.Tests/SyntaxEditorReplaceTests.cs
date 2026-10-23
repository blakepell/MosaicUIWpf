/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using System.Runtime.ExceptionServices;
using ICSharpCode.AvalonEdit.Search;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies the find-and-replace behavior that <see cref="SyntaxEditor"/> layers on top of the
    /// AvalonEdit <see cref="SearchPanel"/>.
    /// </summary>
    public class SyntaxEditorReplaceTests
    {
        [Fact]
        public void ReplaceNextWithoutAMatchSelectedOnlySelectsTheNextMatch()
        {
            RunSta(() =>
            {
                var editor = Create("one two one", "one", "1");

                bool replaced = editor.ReplaceNext();

                Assert.False(replaced);
                Assert.Equal("one two one", editor.Text);
                Assert.Equal("one", editor.SelectedText);
                Assert.Equal(0, editor.SelectionStart);
            });
        }

        [Fact]
        public void ReplaceNextReplacesTheSelectedMatchAndMovesToTheNext()
        {
            RunSta(() =>
            {
                var editor = Create("one two one", "one", "1");
                editor.ReplaceNext();

                bool replaced = editor.ReplaceNext();

                Assert.True(replaced);
                Assert.Equal("1 two one", editor.Text);
                Assert.Equal("one", editor.SelectedText);
                Assert.Equal(6, editor.SelectionStart);
            });
        }

        [Fact]
        public void ReplaceNextDoesNotSkipAnAdjacentMatch()
        {
            RunSta(() =>
            {
                var editor = Create("aaa", "a", "b");
                editor.ReplaceNext();

                editor.ReplaceNext();
                Assert.Equal("baa", editor.Text);
                Assert.Equal(1, editor.SelectionStart);

                editor.ReplaceNext();
                Assert.Equal("bba", editor.Text);
                Assert.Equal(2, editor.SelectionStart);
            });
        }

        [Fact]
        public void ReplaceNextTreatsTheReplacementLiterallyWithoutRegex()
        {
            RunSta(() =>
            {
                var editor = Create("price", "price", "$1.00");
                editor.ReplaceNext();

                editor.ReplaceNext();

                Assert.Equal("$1.00", editor.Text);
            });
        }

        [Fact]
        public void ReplaceAllHonorsRegexGroupSubstitutions()
        {
            RunSta(() =>
            {
                var editor = Create("a=1;b=2", @"(\w)=(\d)", "$2=$1");
                editor.SearchPanel!.UseRegex = true;

                int count = editor.ReplaceAll();

                Assert.Equal(2, count);
                Assert.Equal("1=a;2=b", editor.Text);
            });
        }

        [Fact]
        public void ReplaceAllReplacesEveryMatchInASingleUndoStep()
        {
            RunSta(() =>
            {
                var editor = Create("x x x", "x", "yy");

                int count = editor.ReplaceAll();

                Assert.Equal(3, count);
                Assert.Equal("yy yy yy", editor.Text);

                editor.Undo();
                Assert.Equal("x x x", editor.Text);
            });
        }

        [Fact]
        public void ReplaceAllRespectsMatchCase()
        {
            RunSta(() =>
            {
                var editor = Create("Cat cat", "cat", "dog");
                editor.SearchPanel!.MatchCase = true;

                int count = editor.ReplaceAll();

                Assert.Equal(1, count);
                Assert.Equal("Cat dog", editor.Text);
            });
        }

        [Fact]
        public void ReplaceIsInertWhenTheEditorIsReadOnly()
        {
            RunSta(() =>
            {
                var editor = Create("one one", "one", "1");
                editor.IsReadOnly = true;

                Assert.Equal(0, editor.ReplaceAll());
                Assert.False(editor.ReplaceNext());
                Assert.Equal("one one", editor.Text);
            });
        }

        [Fact]
        public void ReplaceAllWithAnEmptyPatternDoesNothing()
        {
            RunSta(() =>
            {
                var editor = Create("one one", string.Empty, "1");

                Assert.Equal(0, editor.ReplaceAll());
                Assert.Equal("one one", editor.Text);
            });
        }

        [Fact]
        public void OpenReplacePanelShowsTheReplaceRowAndSeedsTheSelection()
        {
            RunSta(() =>
            {
                EnsureThemeManagerRegistered();

                var editor = new SyntaxEditor { Text = "alpha beta" };
                editor.Select(6, 4);

                editor.OpenReplacePanel();

                var panel = editor.SearchPanel;
                Assert.NotNull(panel);
                Assert.False(panel.IsClosed);
                Assert.True(SyntaxEditor.GetIsReplaceVisible(panel));
                Assert.Equal("beta", panel.SearchPattern);
            });
        }

        [Fact]
        public void ThemeChangeRecreatesThePanelWithTheReplaceStateIntact()
        {
            RunSta(() =>
            {
                EnsureThemeManagerRegistered();

                var editor = Create("alpha", "alpha", "beta");
                editor.OpenReplacePanel();
                var original = editor.SearchPanel;

                editor.Theme = editor.Theme == MosaicThemeMode.Dark ? MosaicThemeMode.Light : MosaicThemeMode.Dark;

                var recreated = editor.SearchPanel;
                Assert.NotNull(recreated);
                Assert.NotSame(original, recreated);
                Assert.Equal("alpha", recreated.SearchPattern);
                Assert.True(SyntaxEditor.GetIsReplaceVisible(recreated));
                Assert.Equal("beta", SyntaxEditor.GetReplacePattern(recreated));
            });
        }

        /// <summary>
        /// Creates an editor with the given text, search pattern, and replacement text.
        /// </summary>
        private static SyntaxEditor Create(string text, string searchPattern, string replacePattern)
        {
            var editor = new SyntaxEditor { Text = text };
            var panel = editor.SearchPanel;
            Assert.NotNull(panel);

            panel.SearchPattern = searchPattern;
            SyntaxEditor.SetReplacePattern(panel, replacePattern);

            return editor;
        }

        private static readonly Lock ThemeManagerGate = new();

        /// <summary>
        /// The panel template contains AdaptiveImage icons, which resolve the current theme through
        /// AppServices. Only a hosting MosaicApp populates it, so register a ThemeManager here. AppServices
        /// is process wide, so the registration has to be atomic across the parallel test threads.
        /// </summary>
        private static void EnsureThemeManagerRegistered()
        {
            lock (ThemeManagerGate)
            {
                try
                {
                    AppServices.AddSingleton(new ThemeManager());
                }
                catch (InvalidOperationException)
                {
                    // Another test in this process registered it first, which is all we needed.
                }
            }
        }

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
