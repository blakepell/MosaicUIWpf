/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Argus.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public partial class PropertyGridTests
    {
        public partial class Inner : ObservableObject
        {
            [ObservableProperty]
            private string? _city;

            [ObservableProperty]
            private int _zip;

            public override string ToString() => $"{City} {Zip}";
        }

        public partial class Outer : ObservableObject
        {
            [ObservableProperty]
            private string? _name;

            [ObservableProperty]
            private Inner? _inner;

            [ObservableProperty]
            private FontFamily? _font;

            [ObservableProperty]
            private ObservableCollection<string> _tags = new();

            [ObservableProperty]
            private Outer? _self;
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

        private static readonly Lock ThemeManagerGate = new();

        /// <summary>
        /// The read-only lock icon is an AdaptiveImage, which resolves the theme through AppServices.  Only a
        /// hosting MosaicApp populates it, so register one (atomically, since AppServices is process wide).
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

        private static void Pump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }

        private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindAll<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private static PropertyItem Item(PropertyGrid grid, string name) => grid.Properties.Single(p => p.Name == name);

        [Theory]
        [InlineData(nameof(Outer.Inner), true)]
        [InlineData(nameof(Outer.Self), true)]
        [InlineData(nameof(Outer.Name), false)]
        [InlineData(nameof(Outer.Font), false)]
        [InlineData(nameof(Outer.Tags), false)]
        public void DetermineExpandable_OnlyMatchesSubObjects(string property, bool expected)
        {
            var pd = TypeDescriptor.GetProperties(typeof(Outer))[property]!;
            Assert.Equal(expected, PropertyItem.DetermineExpandable(pd));
        }

        [Fact]
        public void CanExpand_TracksNullValueAndCollapses()
        {
            var outer = new Outer();
            var pd = TypeDescriptor.GetProperties(outer)[nameof(Outer.Inner)]!;
            var item = new PropertyItem(pd, outer);

            Assert.True(item.IsExpandable);
            Assert.False(item.CanExpand);

            outer.Inner = new Inner { City = "Springfield", Zip = 1 };
            Assert.True(item.CanExpand);
            item.IsExpanded = true;

            outer.Inner = null;
            Assert.False(item.CanExpand);
            Assert.False(item.IsExpanded);
        }

        [Fact]
        public void ValueSummary_RefreshesWhenSubObjectChanges()
        {
            var outer = new Outer { Inner = new Inner { City = "A", Zip = 1 } };
            var pd = TypeDescriptor.GetProperties(outer)[nameof(Outer.Inner)]!;
            var item = new PropertyItem(pd, outer);
            var raised = new List<string?>();
            item.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            outer.Inner.City = "B";

            Assert.Contains(nameof(PropertyItem.ValueSummary), raised);
            Assert.Equal("B 1", item.ValueSummary);

            item.Detach();
            raised.Clear();
            outer.Inner.City = "C";
            Assert.Empty(raised);
        }

        [Fact]
        public void Expanding_ShowsInsetNestedGrid_AndCollapsingReleasesIt()
        {
            RunSta(() =>
            {
                EnsureThemeManagerRegistered();
                var outer = new Outer { Name = "root", Inner = new Inner { City = "Springfield", Zip = 62704 } };
                outer.Self = outer; // a cycle must not be walked until it is expanded
                var grid = new PropertyGrid { Object = outer };
                var window = new Window { Content = grid, Width = 500, Height = 600, ShowActivated = false, WindowStyle = WindowStyle.None };

                try
                {
                    window.Show();
                    window.UpdateLayout();

                    Assert.DoesNotContain(FindAll<PropertyGrid>(grid), g => g.IsNested);

                    var innerItem = Item(grid, nameof(Outer.Inner));
                    Assert.True(innerItem.CanExpand);
                    Assert.False(Item(grid, nameof(Outer.Name)).CanExpand);

                    innerItem.IsExpanded = true;
                    window.UpdateLayout();

                    var nested = Assert.Single(FindAll<PropertyGrid>(grid), g => g.IsNested);
                    Assert.Same(outer.Inner, nested.Object);
                    Assert.False(nested.ShowDescriptionPanel);
                    Assert.Equal(16, nested.Margin.Left);
                    Assert.Equal([nameof(Inner.City), nameof(Inner.Zip)], nested.Properties.Select(p => p.Name).OrderBy(n => n));

                    // Single category in a nested grid: no group header.
                    Assert.Empty(((ICollectionView)CollectionViewSourceDefault(nested)).GroupDescriptions);

                    // Editing in the nested grid writes through to the sub object.
                    Item(nested, nameof(Inner.City)).Value = "Shelbyville";
                    Assert.Equal("Shelbyville", outer.Inner!.City);

                    // Selecting inside the nested grid selects on the outermost grid.
                    var nameCell = FindAll<Border>(nested).First(b => PropertyGrid.GetSelectOnClick(b) && b.DataContext is PropertyItem { Name: nameof(Inner.Zip) });
                    typeof(PropertyGrid).GetMethod("SelectFrom", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [nameCell]);
                    Assert.Same(Item(nested, nameof(Inner.Zip)), grid.SelectedProperty);
                    Assert.Null(nested.SelectedProperty);

                    innerItem.IsExpanded = false;
                    window.UpdateLayout();
                    Pump();

                    Assert.DoesNotContain(FindAll<PropertyGrid>(grid), g => g.IsNested);
                    Assert.Empty(nested.Properties);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static ICollectionView CollectionViewSourceDefault(PropertyGrid grid)
        {
            return System.Windows.Data.CollectionViewSource.GetDefaultView(grid.Properties);
        }
    }
}
