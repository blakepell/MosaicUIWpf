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
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is detached.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.TextChanged -= SearchBox_TextChanged;

            if (_searchTimer == null)
            {
                return;
            }

            _searchTimer.Tick -= SearchTimer_Tick;
            _searchTimer.Stop();
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

            // This is the filter that will filter the collection view.            
            collectionView.Filter = item =>
            {
                if (item == null)
                {
                    return false;
                }

                string? fieldSearch = item.ToString();

                if (string.IsNullOrWhiteSpace(fieldSearch))
                {
                    return false;
                }

                return fieldSearch.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            };
        }
    }
}
