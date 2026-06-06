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

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that ensures the last column of a DataGrid fills the remaining space.
    /// </summary>
    /// <example>
    /// The following example demonstrates how to use the DataGridLastColumnFill behavior in XAML:
    /// <code>
    /// <![CDATA[
    /// <![CDATA[
    ///     <Grid>
    ///         <DataGrid AutoGenerateColumns="True">
    ///             <i:Interaction.Behaviors>
    ///                 <b:DataGridLastColumnFill />
    ///             </i:Interaction.Behaviors>
    ///         </DataGrid>
    ///     </Grid>
    /// ]]>
    /// </code>
    /// </example>
    public class DataGridLastColumnFillBehavior : Behavior<DataGrid>
    {
        /// <summary>
        /// Attaches the behavior to the DataGrid and subscribes to the Loaded and SizeChanged events.
        /// </summary>
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += ResizeColumns;
            AssociatedObject.SizeChanged += ResizeColumns;
            base.OnAttached();
        }

        /// <summary>
        /// Detaches the behavior from the DataGrid and unsubscribes from the Loaded and SizeChanged events.
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= ResizeColumns;
            AssociatedObject.SizeChanged -= ResizeColumns;
            base.OnDetaching();
        }

        /// <summary>
        /// Resizes the last column of the DataGrid to fill the remaining space.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ResizeColumns(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.Columns.LastOrDefault() is { } lastColumn)
            {
                lastColumn.Width = new(1, DataGridLengthUnitType.Star);
            }
        }
    }
}
