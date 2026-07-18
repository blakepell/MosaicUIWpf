/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class EnumComboBoxTests
    {
        private enum TestStatus
        {
            PlainValue,

            [EnumDisplayName("Friendly Display Name")]
            DisplayNamed,

            [Description("Descriptive Name")]
            Described,

            [EnumDisplayName("Display Wins")]
            [Description("Description Loses")]
            BothAttributes
        }

        private enum OtherStatus
        {
            Alpha,
            Beta
        }

        private enum AliasedStatus
        {
            Original = 1,

            [EnumDisplayName("Original (Alias)")]
            Alias = 1
        }

        [Flags]
        private enum FlagStatus
        {
            None = 0,
            One = 1,
            Two = 2,

            [EnumDisplayName("One and Two")]
            Both = One | Two
        }

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

        private static IReadOnlyList<EnumComboBoxItem> Items(EnumComboBox control)
        {
            return (IReadOnlyList<EnumComboBoxItem>)control.ItemsSource!;
        }

        [Fact]
        public void EnumType_PopulatesOneItemPerDeclaredField()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                Assert.Equal(4, Items(control).Count);
            });
        }

        [Fact]
        public void PlainMember_UsesMemberName()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                Assert.Equal(nameof(TestStatus.PlainValue), Items(control)[0].DisplayName);
            });
        }

        [Fact]
        public void DisplayNameAttribute_IsUsedWhenPresent()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                Assert.Equal("Friendly Display Name", Items(control)[1].DisplayName);
            });
        }

        [Fact]
        public void DescriptionAttribute_IsUsedWhenNoDisplayName()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                Assert.Equal("Descriptive Name", Items(control)[2].DisplayName);
            });
        }

        [Fact]
        public void DisplayNameAttribute_WinsOverDescription()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                Assert.Equal("Display Wins", Items(control)[3].DisplayName);
            });
        }

        [Fact]
        public void ItemValues_AreActualEnumValues()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                var items = Items(control);

                Assert.Equal(TestStatus.PlainValue, items[0].Value);
                Assert.Equal(TestStatus.DisplayNamed, items[1].Value);
                Assert.Equal(TestStatus.Described, items[2].Value);
                Assert.Equal(TestStatus.BothAttributes, items[3].Value);
            });
        }

        [Fact]
        public void NullEnumType_ClearsItems()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                control.EnumType = null;
                Assert.Null(control.ItemsSource);
            });
        }

        [Fact]
        public void NonEnumType_ThrowsArgumentException()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox();
                Assert.Throws<ArgumentException>(() => control.EnumType = typeof(string));
            });
        }

        [Fact]
        public void NullableEnumType_ResolvesUnderlyingType()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus?) };
                var items = Items(control);

                Assert.Equal(4, items.Count);
                Assert.Equal(TestStatus.PlainValue, items[0].Value);
            });
        }

        [Fact]
        public void ChangingEnumType_RefreshesItems()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                control.EnumType = typeof(OtherStatus);

                var items = Items(control);
                Assert.Equal(2, items.Count);
                Assert.Equal(OtherStatus.Alpha, items[0].Value);
                Assert.Equal(OtherStatus.Beta, items[1].Value);
            });
        }

        [Fact]
        public void DuplicateNumericValues_RemainSeparateDeclaredItems()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(AliasedStatus) };
                var items = Items(control);

                Assert.Equal(2, items.Count);
                Assert.Equal(nameof(AliasedStatus.Original), items[0].DisplayName);
                Assert.Equal("Original (Alias)", items[1].DisplayName);
            });
        }

        [Fact]
        public void FlagsEnum_ShowsDeclaredMembersOnly()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(FlagStatus) };
                var items = Items(control);

                Assert.Equal(4, items.Count);
                Assert.Equal(FlagStatus.Both, items[3].Value);
                Assert.Equal("One and Two", items[3].DisplayName);
            });
        }

        [Fact]
        public void SameEnumType_UsesCachedReadOnlyItems()
        {
            RunSta(() =>
            {
                var first = new EnumComboBox { EnumType = typeof(TestStatus) };
                var second = new EnumComboBox { EnumType = typeof(TestStatus) };

                Assert.Same(first.ItemsSource, second.ItemsSource);
                Assert.IsNotAssignableFrom<IList<object>>(first.ItemsSource);
            });
        }

        [Fact]
        public void SelectedValue_SelectsMatchingItem()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                control.SelectedValue = TestStatus.Described;

                var selected = Assert.IsType<EnumComboBoxItem>(control.SelectedItem);
                Assert.Equal(TestStatus.Described, selected.Value);
                Assert.Equal(2, control.SelectedIndex);
            });
        }

        [Fact]
        public void SelectedValue_NullClearsSelection()
        {
            RunSta(() =>
            {
                var control = new EnumComboBox { EnumType = typeof(TestStatus) };
                control.SelectedValue = TestStatus.PlainValue;
                control.SelectedValue = null;

                Assert.Null(control.SelectedItem);
                Assert.Equal(-1, control.SelectedIndex);
            });
        }
    }
}
