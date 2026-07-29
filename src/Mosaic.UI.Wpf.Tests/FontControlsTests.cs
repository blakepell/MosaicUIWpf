/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class FontControlsTests
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

        /// <summary>
        /// Returns an installed font from the catalog so the tests do not depend on any specific
        /// font being present on the machine running them.
        /// </summary>
        private static FontFamily KnownFont(int index = 0)
        {
            return FontFamilyCatalog.Families[index];
        }

        [Fact]
        public void Catalog_IsNotEmptyAndSortedByName()
        {
            RunSta(() =>
            {
                var families = FontFamilyCatalog.Families;

                Assert.NotEmpty(families);
                Assert.Equal(
                    families.Select(f => f.Source).OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase),
                    families.Select(f => f.Source));
            });
        }

        [Fact]
        public void Catalog_IsCachedAcrossCalls()
        {
            RunSta(() => Assert.Same(FontFamilyCatalog.Families, FontFamilyCatalog.Families));
        }

        [Fact]
        public void Catalog_FindIsCaseInsensitiveAndReturnsCachedInstance()
        {
            RunSta(() =>
            {
                var expected = KnownFont();

                Assert.Same(expected, FontFamilyCatalog.Find(expected.Source.ToUpperInvariant()));
                Assert.Null(FontFamilyCatalog.Find("No Such Font 12345"));
                Assert.Null(FontFamilyCatalog.Find(null));
            });
        }

        [Fact]
        public void Catalog_ResolveMatchesAnEquivalentFamilyByName()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                Assert.Same(expected, FontFamilyCatalog.Resolve(new FontFamily(expected.Source)));
            });
        }

        [Fact]
        public void FontComboBox_PopulatesFromCatalog()
        {
            RunSta(() =>
            {
                var control = new FontComboBox();
                Assert.Same(FontFamilyCatalog.Families, control.ItemsSource);
            });
        }

        [Fact]
        public void FontComboBox_SelectedFontNameDrivesSelectionAndFamily()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                var control = new FontComboBox { SelectedFontName = expected.Source };

                Assert.Same(expected, control.SelectedItem);
                Assert.Same(expected, control.SelectedFontFamily);
            });
        }

        [Fact]
        public void FontComboBox_SelectedFontFamilyDrivesSelectionAndName()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                var control = new FontComboBox { SelectedFontFamily = new FontFamily(expected.Source) };

                Assert.Same(expected, control.SelectedItem);
                Assert.Equal(expected.Source, control.SelectedFontName);
            });
        }

        [Fact]
        public void FontComboBox_SelectedItemDrivesBothProperties()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                var control = new FontComboBox { SelectedItem = expected };

                Assert.Same(expected, control.SelectedFontFamily);
                Assert.Equal(expected.Source, control.SelectedFontName);
            });
        }

        [Fact]
        public void FontComboBox_UninstalledFontLeavesSelectionEmpty()
        {
            RunSta(() =>
            {
                var control = new FontComboBox { SelectedFontName = "No Such Font 12345" };

                Assert.Null(control.SelectedItem);
                Assert.Null(control.SelectedFontFamily);
            });
        }

        [Fact]
        public void FontComboBox_ClearingSelectionClearsBothProperties()
        {
            RunSta(() =>
            {
                var control = new FontComboBox { SelectedFontName = KnownFont().Source };
                control.SelectedItem = null;

                Assert.Null(control.SelectedFontFamily);
                Assert.Null(control.SelectedFontName);
            });
        }

        [Fact]
        public void FontComboBox_PreviewAndPlainDisplayAreMutuallyExclusive()
        {
            RunSta(() =>
            {
                var control = new FontComboBox();

                Assert.False(control.ShowFontPreview);
                Assert.Null(control.ItemTemplate);
                Assert.Equal(nameof(FontFamily.Source), control.DisplayMemberPath);

                control.ShowFontPreview = true;
                Assert.NotNull(control.ItemTemplate);
                Assert.True(string.IsNullOrEmpty(control.DisplayMemberPath));

                control.ShowFontPreview = false;
                Assert.Null(control.ItemTemplate);
                Assert.Equal(nameof(FontFamily.Source), control.DisplayMemberPath);
            });
        }

        [Fact]
        public void FontComboBox_PreviewFontSizeRebuildsTemplateWhilePreviewing()
        {
            RunSta(() =>
            {
                var control = new FontComboBox { ShowFontPreview = true };
                var original = control.ItemTemplate;

                control.PreviewFontSize = 24;

                Assert.NotNull(control.ItemTemplate);
                Assert.NotSame(original, control.ItemTemplate);
            });
        }

        [Fact]
        public void FontAutoCompleteBox_PopulatesFromCatalogAndSuggestsWithoutText()
        {
            RunSta(() =>
            {
                var control = new FontAutoCompleteBox();

                Assert.Same(FontFamilyCatalog.Families, control.ItemsSource);
                Assert.False(control.IsTextRequiredForSuggestions);
                Assert.Equal(0, control.MinimumPrefixLength);
                Assert.True(control.MaxSuggestionCount >= FontFamilyCatalog.Families.Count);
            });
        }

        /// <summary>
        /// FontAutoCompleteBox intentionally does not register its own default style; it reuses the
        /// AutoCompleteBox template (and therefore its template parts) from Generic.xaml.
        /// </summary>
        [Fact]
        public void FontAutoCompleteBox_ReusesTheAutoCompleteBoxDefaultStyle()
        {
            RunSta(() => Assert.Equal(typeof(AutoCompleteBox), new StyleKeyProbe().StyleKey));
        }

        /// <summary>
        /// Exposes the protected default style key so the inherited value can be asserted.
        /// </summary>
        private sealed class StyleKeyProbe : FontAutoCompleteBox
        {
            public object? StyleKey => DefaultStyleKey;
        }

        [Fact]
        public void FontAutoCompleteBox_PreviewsByDefault()
        {
            RunSta(() =>
            {
                var control = new FontAutoCompleteBox();

                Assert.True(control.ShowFontPreview);
                Assert.NotNull(control.ItemTemplate);
                Assert.True(string.IsNullOrEmpty(control.DisplayMemberPath));
            });
        }

        [Fact]
        public void FontAutoCompleteBox_SelectedFontNameDrivesSelectionFamilyAndText()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                var control = new FontAutoCompleteBox { SelectedFontName = expected.Source };

                Assert.Same(expected, control.SelectedItem);
                Assert.Same(expected, control.SelectedFontFamily);
                Assert.Equal(expected.Source, control.Text);
            });
        }

        [Fact]
        public void FontAutoCompleteBox_SelectedFontFamilyDrivesSelectionAndName()
        {
            RunSta(() =>
            {
                var expected = KnownFont();
                var control = new FontAutoCompleteBox { SelectedFontFamily = new FontFamily(expected.Source) };

                Assert.Same(expected, control.SelectedItem);
                Assert.Equal(expected.Source, control.SelectedFontName);
            });
        }

        [Fact]
        public void FontAutoCompleteBox_UninstalledFontLeavesSelectionEmpty()
        {
            RunSta(() =>
            {
                var control = new FontAutoCompleteBox { SelectedFontName = "No Such Font 12345" };

                Assert.Null(control.SelectedItem);
                Assert.Null(control.SelectedFontFamily);
            });
        }

        [Fact]
        public void FontAutoCompleteBox_DisablingPreviewFallsBackToPlainNames()
        {
            RunSta(() =>
            {
                var control = new FontAutoCompleteBox { ShowFontPreview = false };

                Assert.Null(control.ItemTemplate);
                Assert.Equal(nameof(FontFamily.Source), control.DisplayMemberPath);
            });
        }
    }
}
