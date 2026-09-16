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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class MarkdownEditorTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF controls require.
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

        private static readonly Lock ThemeManagerGate = new();

        /// <summary>
        /// AdaptiveImage (used by the editor's toolbar buttons) resolves the current theme through
        /// AppServices, which only a hosting MosaicApp populates. Register one so the toolbar can be
        /// constructed. AppServices is process wide, so the check and the registration have to be
        /// atomic across the parallel test threads.
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

        /// <summary>
        /// Creates an editor with its theme dependency satisfied. Constructing it also proves the
        /// control's XAML parses and that every resource it references resolves.
        /// </summary>
        private static MarkdownEditor CreateEditor()
        {
            EnsureThemeManagerRegistered();
            return new MarkdownEditor();
        }

        /// <summary>
        /// Resolves the toolbar's custom menu split button, whose visibility tracks the caller's items.
        /// </summary>
        private static SplitButton CustomMenuButton(MarkdownEditor editor) => (SplitButton)editor.FindName("CustomMenuButton")!;

        [Fact]
        public void CustomMenuIsCollapsedWhenNoItemsAreSupplied()
        {
            RunSta(() =>
            {
                var editor = CreateEditor();

                Assert.Empty(editor.CustomMenuItems);
                Assert.Equal(Visibility.Collapsed, CustomMenuButton(editor).Visibility);
            });
        }

        [Fact]
        public void CustomMenuBecomesVisibleWithASingleItem()
        {
            RunSta(() =>
            {
                var editor = CreateEditor();
                editor.CustomMenuItems.Add(new MenuItem { Header = "Character Count..." });

                Assert.Equal(Visibility.Visible, CustomMenuButton(editor).Visibility);
            });
        }

        [Fact]
        public void CustomMenuCollapsesAgainWhenTheLastItemIsRemoved()
        {
            RunSta(() =>
            {
                var editor = CreateEditor();
                var item = new MenuItem { Header = "Character Count..." };

                editor.CustomMenuItems.Add(item);
                editor.CustomMenuItems.Remove(item);

                Assert.Equal(Visibility.Collapsed, CustomMenuButton(editor).Visibility);
            });
        }

        /// <summary>
        /// A supplied item has to be a logical child of the drop-down menu as soon as it is added,
        /// otherwise the container's alignment bindings have no ancestor ItemsControl to resolve against
        /// and WPF reports a binding error against the caller's XAML.
        /// </summary>
        [Fact]
        public void SuppliedItemIsParentedToTheDropDownMenuImmediately()
        {
            RunSta(() =>
            {
                var editor = CreateEditor();
                var item = new MenuItem { Header = "Character Count..." };

                editor.CustomMenuItems.Add(item);

                var menu = CustomMenuButton(editor).ContextMenu!;

                Assert.Same(menu, LogicalTreeHelper.GetParent(item));
            });
        }

        /// <summary>
        /// Mirrors how a host supplies items from markup, which is the shape the demo example uses.
        /// </summary>
        [Fact]
        public void SuppliedItemsCanBeDeclaredAsAPropertyElementInMarkup()
        {
            RunSta(() =>
            {
                EnsureThemeManagerRegistered();

                const string markup = """
                <mosaic:MarkdownEditor xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                       xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui">
                    <mosaic:MarkdownEditor.CustomMenuItems>
                        <MenuItem Header="Character Count..." />
                        <Separator />
                    </mosaic:MarkdownEditor.CustomMenuItems>
                </mosaic:MarkdownEditor>
                """;

                var editor = (MarkdownEditor)XamlReader.Parse(markup);

                Assert.Equal(2, editor.CustomMenuItems.Count);
                Assert.Equal(Visibility.Visible, CustomMenuButton(editor).Visibility);
                Assert.Same(CustomMenuButton(editor).ContextMenu, LogicalTreeHelper.GetParent((MenuItem)editor.CustomMenuItems[0]!));
            });
        }
    }
}
