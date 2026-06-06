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
using System.Collections;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Deletes selected <see cref="ListView"/> items when the Delete key is pressed.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <ListView ItemsSource="{Binding Items}">
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:ListViewDeleteBehavior ConfirmDelete="True" />
    ///     </i:Interaction.Behaviors>
    /// </ListView>
    /// ]]>
    /// </example>
    public class ListViewDeleteBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Whether to show a confirmation dialog before deleting items.
        /// </summary>
        public static readonly DependencyProperty ConfirmDeleteProperty = DependencyProperty.Register(
                nameof(ConfirmDelete), typeof(bool), typeof(ListViewDeleteBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether to show a confirmation
        /// dialog before deleting items.
        /// </summary>
        public bool ConfirmDelete
        {
            get => (bool)GetValue(ConfirmDeleteProperty);
            set => SetValue(ConfirmDeleteProperty, value);
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is attached.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject != null)
            {
                // Hook into the KeyDown event
                this.AssociatedObject.KeyDown += AssociatedObject_KeyDown;
            }
        }

        /// <summary>
        /// When the <see cref="Behavior"/> is detached.
        /// </summary>
        protected override void OnDetaching()
        {
            if (this.AssociatedObject != null)
            {
                // Unhook the event to prevent memory leaks
                this.AssociatedObject.KeyDown -= AssociatedObject_KeyDown;
            }
            base.OnDetaching();
        }

        /// <summary>
        /// Handles the KeyDown event on the associated ListView.
        /// </summary>
        private void AssociatedObject_KeyDown(object sender, KeyEventArgs e)
        {
            // Only proceed if the Delete key was pressed
            if (e.Key != Key.Delete)
            {
                return;
            }

            var listView = this.AssociatedObject;
            if (listView == null)
            {
                return;
            }

            // We need an IList to remove items from.
            if (!(listView.ItemsSource is IList itemsSource))
            {
                return;
            }

            // Get the selected items
            var selectedItems = listView.SelectedItems;
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }

            if (this.ConfirmDelete)
            {
                int count = selectedItems.Count;
                string itemText = count == 1 ? "item" : "items";
                string message = $"Are you sure you want to delete the {count} selected {itemText}?";

                // Show the confirmation dialog
                if (MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    // User clicked "No" or closed the dialog.
                    // Stop processing and do not delete.
                    return;
                }
            }

            // --- Calculate the index to select *after* deletion ---

            // We must make a copy, as the SelectedItems collection will
            // change as we remove items from the source.
            var itemsToRemove = selectedItems.Cast<object>().ToList();
            int selectedCount = itemsToRemove.Count;

            // Find the highest index of any item being removed.
            int lastSelectedIndex = -1;
            foreach (var item in itemsToRemove)
            {
                lastSelectedIndex = Math.Max(lastSelectedIndex, itemsSource.IndexOf(item));
            }

            // This is the index that *should* be selected after the items are removed.
            int newIndexToSelect = lastSelectedIndex - selectedCount + 1;

            // --- Perform the deletion ---
            foreach (var item in itemsToRemove)
            {
                itemsSource.Remove(item);
            }

            // --- Set the new selection ---
            int newCount = itemsSource.Count;
            if (newCount > 0)
            {
                // Clamp the index to be valid.
                listView.SelectedIndex = Math.Min(Math.Max(0, newIndexToSelect), newCount - 1);
            }
            else
            {
                // The list is now empty
                listView.SelectedIndex = -1;
            }

            // Mark the event as handled so it doesn't propagate further
            e.Handled = true;
        }
    }
}