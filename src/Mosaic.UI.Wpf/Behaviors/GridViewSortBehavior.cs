/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Provides attached properties and behaviors to enable sorting of a ListView's GridView columns
    /// when the column headers are clicked.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// <ListView ItemsSource="{Binding YourCollection}" 
    ///           behaviors:GridViewSortBehavior.IsEnabled="True">
    ///     <ListView.View>
    ///         <GridView>
    ///             <GridViewColumn Header="Name" 
    ///                             DisplayMemberBinding="{Binding Name}" 
    ///                             behaviors:GridViewSortBehavior.SortPropertyName="Name" />
    ///             <GridViewColumn Header="Age" 
    ///                             DisplayMemberBinding="{Binding Age}" 
    ///                             behaviors:GridViewSortBehavior.SortPropertyName="Age" />
    ///             <GridViewColumn Header="Country" 
    ///                             DisplayMemberBinding="{Binding Country}" 
    ///                             behaviors:GridViewSortBehavior.SortPropertyName="Country" />
    ///         </GridView>
    ///     </ListView.View>
    /// </ListView>
    /// ]]>
    /// </remarks>
    public static class GridViewSortBehavior
    {
        /// <summary>
        /// Enables the sort behaviors on the grid view.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(GridViewSortBehavior), new UIPropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// The name of the property on the bound class that should be used to sort.
        /// </summary>
        public static readonly DependencyProperty SortPropertyNameProperty = DependencyProperty.RegisterAttached(
                "SortPropertyName", typeof(string), typeof(GridViewSortBehavior), new UIPropertyMetadata(null));

        public static string GetSortPropertyName(DependencyObject obj)
        {
            return (string)obj.GetValue(SortPropertyNameProperty);
        }

        public static void SetSortPropertyName(DependencyObject obj, string value)
        {
            obj.SetValue(SortPropertyNameProperty, value);
        }

        /// <summary>
        /// Last header that was clicked to sort.
        /// </summary>
        private static readonly DependencyProperty LastHeaderClickedProperty = DependencyProperty.RegisterAttached(
                "LastHeaderClicked", typeof(GridViewColumnHeader), typeof(GridViewSortBehavior), new UIPropertyMetadata(null));

        private static GridViewColumnHeader GetLastHeaderClicked(DependencyObject obj)
        {
            return (GridViewColumnHeader)obj.GetValue(LastHeaderClickedProperty);
        }

        private static void SetLastHeaderClicked(DependencyObject obj, GridViewColumnHeader value)
        {
            obj.SetValue(LastHeaderClickedProperty, value);
        }

        /// <summary>
        /// The last sort direction.
        /// </summary>
        private static readonly DependencyProperty LastSortDirectionProperty = DependencyProperty.RegisterAttached(
            "LastSortDirection", typeof(ListSortDirection), typeof(GridViewSortBehavior), new UIPropertyMetadata(ListSortDirection.Ascending));

        private static ListSortDirection GetLastSortDirection(DependencyObject obj)
        {
            return (ListSortDirection)obj.GetValue(LastSortDirectionProperty);
        }

        private static void SetLastSortDirection(DependencyObject obj, ListSortDirection value)
        {
            obj.SetValue(LastSortDirectionProperty, value);
        }

        /// <summary>
        /// When the grid sorting behavior has been toggled on or off.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                bool isEnabled = (bool)e.NewValue;

                if (isEnabled)
                {
                    listView.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                }
                else
                {
                    listView.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                }
            }
        }

        /// <summary>
        /// Column click event that implements the sorting logic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Column != null)
            {
                string sortBy = GetSortPropertyName(headerClicked.Column);

                if (string.IsNullOrEmpty(sortBy))
                {
                    return;
                }

                var listView = sender as ListView;

                if (listView == null)
                {
                    return;
                }

                var collectionView = CollectionViewSource.GetDefaultView(listView.ItemsSource);

                if (collectionView == null || !collectionView.CanSort)
                {
                    return;
                }

                ListSortDirection direction;

                var lastHeaderClicked = GetLastHeaderClicked(listView);
                var lastDirection = GetLastSortDirection(listView);

                if (headerClicked == lastHeaderClicked)
                {
                    // Toggle direction
                    direction = lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }

                // Apply the sort
                collectionView.SortDescriptions.Clear();
                collectionView.SortDescriptions.Add(new SortDescription(sortBy, direction));
                collectionView.Refresh();

                // Save the current header and direction
                SetLastHeaderClicked(listView, headerClicked);
                SetLastSortDirection(listView, direction);
            }
        }
    }
}