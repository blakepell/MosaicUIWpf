/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Behavior that will allow a <see cref="TextBox"/> to filter a <see cref="DataGrid"/>.
    /// </summary>
    /// <remarks>
    /// This behavior allows a <see cref="TextBox"/> to filter the items in a <see cref="DataGrid"/>. 
    /// To use this behavior, attach it to a <see cref="TextBox"/> and bind the <see cref="TargetDataGrid"/> property 
    /// to the <see cref="DataGrid"/> you want to filter.
    ///
    /// Example usage in XAML:
    /// <code>
    /// <![CDATA[
    /// <Window x:Class="YourNamespace.MainWindow"
    ///         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    ///         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ///         xmlns:behaviors="clr-namespace:ApexGate.UI.Behaviors"
    ///         Title="DataGrid Filter Example" Height="350" Width="525">
    ///     <Grid>
    ///         <Grid.RowDefinitions>
    ///             <RowDefinition Height="Auto" />
    ///             <RowDefinition Height="*" />
    ///         </Grid.RowDefinitions>
    ///
    ///         <!-- TextBox for filtering -->
    ///         <TextBox Width="200" Margin="10" VerticalAlignment="Top">
    ///             <i:Interaction.Behaviors>
    ///                 <behaviors:DataGridFilterBehavior TargetDataGrid = "{Binding ElementName=MyDataGrid}" />
    ///             </ i:Interaction.Behaviors>
    ///         </TextBox>
    ///
    ///         <!-- DataGrid to be filtered -->
    ///         <DataGrid x:Name="MyDataGrid" Grid.Row="1" Margin="10" AutoGenerateColumns="True" />
    ///     </Grid>
    /// </Window>
    /// ]]>
    /// </code>
    /// </remarks>
    public class DataGridFilterBehavior : Behavior<TextBox>
    {
        private DispatcherTimer? _searchTimer;

        /// <summary>
        /// <see cref="DataGrid"/> to filter.
        /// </summary>
        public static readonly DependencyProperty TargetDataGridProperty =
            DependencyProperty.Register(nameof(TargetDataGrid), typeof(DataGrid), typeof(DataGridFilterBehavior), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DataGrid"/> to filter.
        /// </summary>
        public DataGrid TargetDataGrid
        {
            get => (DataGrid)GetValue(TargetDataGridProperty);
            set => SetValue(TargetDataGridProperty, value);
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is attached.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            _searchTimer.Tick += SearchTimer_Tick;

            AssociatedObject.TextChanged += SearchBox_TextChanged;
            AssociatedObject.Loaded += SearchBox_Loaded;
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is detached.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= SearchBox_Loaded;
            AssociatedObject.TextChanged -= SearchBox_TextChanged;

            if (_searchTimer == null)
            {
                return;
            }

            _searchTimer.Tick -= SearchTimer_Tick;
            _searchTimer.Stop();
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// When the text in the search box changes.  This will start or reset the
        /// debounce timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            _searchTimer?.Stop();
            _searchTimer?.Start();
        }

        /// <summary>
        /// Debounce search timer for the filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (TargetDataGrid == null)
            {
                return;
            }

            string searchText = AssociatedObject.Text;

            // Update the layout to ensure cursor updates correctly
            AssociatedObject.UpdateLayout();

            // Get the default view and apply the filter
            var collectionView = CollectionViewSource.GetDefaultView(TargetDataGrid.ItemsSource);

            if (collectionView == null)
            {
                return;
            }

            // A DataTable/DataView source produces a BindingListCollectionView, which does not support a
            // predicate filter at all. Those views filter through a row expression instead, so the same
            // "any displayed value contains the text" search is expressed as an OR of LIKE comparisons.
            if (!collectionView.CanFilter && collectionView is BindingListCollectionView { CanCustomFilter: true } bindingListView)
            {
                bindingListView.CustomFilter = BuildRowFilter(TargetDataGrid.ItemsSource as DataView, searchText);
                return;
            }

            // This is the filter that will filter the collection view.
            collectionView.Filter = item =>
            {
                if (item == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return true;
                }

                if (ContainsSearchText(item, searchText))
                {
                    return true;
                }

                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(item))
                {
                    object? value;

                    try
                    {
                        value = property.GetValue(item);
                    }
                    catch
                    {
                        // A failing property getter should not prevent the remaining
                        // displayed row values from participating in the filter.
                        continue;
                    }

                    if (ContainsSearchText(value, searchText))
                    {
                        return true;
                    }
                }

                return false;
            };
        }

        /// <summary>
        /// Builds a <see cref="DataView.RowFilter"/> expression that matches rows where any column
        /// contains the search text.
        /// </summary>
        /// <param name="view">The view being filtered, used to enumerate the available columns.</param>
        /// <param name="searchText">The text to search for.</param>
        /// <returns>The filter expression, or an empty string when every row should be shown.</returns>
        private static string BuildRowFilter(DataView? view, string searchText)
        {
            if (view == null || string.IsNullOrWhiteSpace(searchText))
            {
                return string.Empty;
            }

            string pattern = EscapeLiteral(EscapeLikeWildcards(searchText));
            var clauses = new List<string>();

            foreach (DataColumn column in view.Table!.Columns)
            {
                // Every column is converted to a string so that numeric and date columns match the
                // same way they read in the grid.
                clauses.Add($"CONVERT([{EscapeColumnName(column.ColumnName)}], 'System.String') LIKE '%{pattern}%'");
            }

            return string.Join(" OR ", clauses);
        }

        /// <summary>
        /// Escapes the characters that a row filter treats as LIKE wildcards.
        /// </summary>
        private static string EscapeLikeWildcards(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '*':
                        builder.Append("[*]");
                        break;
                    case '%':
                        builder.Append("[%]");
                        break;
                    case '[':
                        builder.Append("[[]");
                        break;
                    case ']':
                        builder.Append("[]]");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Escapes a string literal for use inside a row filter expression.
        /// </summary>
        private static string EscapeLiteral(string value) => value.Replace("'", "''");

        /// <summary>
        /// Escapes a column name for use inside the square brackets of a row filter expression.
        /// </summary>
        private static string EscapeColumnName(string name) => name.Replace(@"\", @"\\").Replace("]", @"\]");

        private static bool ContainsSearchText(object? value, string searchText)
        {
            if (value == null)
            {
                return false;
            }

            string? text = Convert.ToString(value, CultureInfo.CurrentCulture);

            return !string.IsNullOrWhiteSpace(text)
                && text.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
