/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Behavior that will allow a <see cref="TextBox"/> to filter a <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Example XAML usage:
    ///
    /// <Window
    ///     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    ///     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ///     xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
    ///     xmlns:behaviors="clr-namespace:Mosaic.UI.Wpf.Behaviors;assembly=ApexGateUI">
    ///
    ///     <StackPanel>
    ///         <TextBox x:Name="SearchBox" Width="200" Margin="0,0,0,10">
    ///             <i:Interaction.Behaviors>
    ///                 <behaviors:ItemsControlFilterBehavior TargetItemsControl="{Binding ElementName=MyListBox}" />
    ///             </i:Interaction.Behaviors>
    ///         </TextBox>
    ///
    ///         <ListBox x:Name="MyListBox" ItemsSource="{Binding MyItems}" />
    ///     </StackPanel>
    ///
    /// </Window>
    /// ]]>
    /// </remarks>
    public class ItemsControlFilterBehavior : Behavior<TextBox>
    {
        private DispatcherTimer? _searchTimer;

        /// <summary>
        /// <see cref="ItemsControl"/> to filter.
        /// </summary>
        public static readonly DependencyProperty TargetItemsControlProperty =
            DependencyProperty.Register(nameof(TargetItemsControl), typeof(ItemsControl), typeof(ItemsControlFilterBehavior), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="ItemsControl"/> to filter.
        /// </summary>
        public ItemsControl TargetItemsControl
        {
            get => (ItemsControl)GetValue(TargetItemsControlProperty);
            set => SetValue(TargetItemsControlProperty, value);
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
            AssociatedObject.Loaded += SearchBox_OnLoaded;
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is detached.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= SearchBox_OnLoaded;
            AssociatedObject.TextChanged -= SearchBox_TextChanged;

            if (_searchTimer == null)
            {
                return;
            }

            _searchTimer.Tick -= SearchTimer_Tick;
            _searchTimer.Stop();
        }

        private void SearchBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            SearchTimer_Tick(sender, null);
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

            if (TargetItemsControl == null)
            {
                return;
            }

            string searchText = AssociatedObject.Text;

            // Update the layout to ensure cursor updates correctly
            AssociatedObject.UpdateLayout();

            // Get the default view and apply the filter
            var collectionView = CollectionViewSource.GetDefaultView(TargetItemsControl.ItemsSource);

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
