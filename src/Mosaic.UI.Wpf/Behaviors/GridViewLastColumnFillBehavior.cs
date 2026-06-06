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
    /// A behavior that ensures the last column of a GridView fills the remaining space.
    /// </summary>
    /// <example>
    /// The following example demonstrates how to use the GridViewLastColumnFill behavior in XAML:
    /// <code>
    /// <![CDATA[
    ///     <Grid>
    ///         <ListView>
    ///             <ListView.View>
    ///                 <GridView>
    ///                     <GridViewColumn Header="Column1" Width="100"/>
    ///                     <GridViewColumn Header="Column2" Width="200"/>
    ///                     <GridViewColumn Header="Column3" Width="Auto"/>
    ///                 </GridView>
    ///             </ListView.View>
    ///             <i:Interaction.Behaviors>
    ///                 <b:GridViewLastColumnFillBehavior />
    ///             </i:Interaction.Behaviors>
    ///         </ListView>
    ///     </Grid>
    /// ]]>
    /// </code>
    /// </example>
    public class GridViewLastColumnFillBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Attaches the behavior to the ListView and subscribes to the Loaded and SizeChanged events.
        /// </summary>
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += ResizeColumns;
            AssociatedObject.SizeChanged += ResizeColumns;
            base.OnAttached();
        }

        /// <summary>
        /// Detaches the behavior from the ListView and unsubscribes from the Loaded and SizeChanged events.
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= ResizeColumns;
            AssociatedObject.SizeChanged -= ResizeColumns;
            base.OnDetaching();
        }

        /// <summary>
        /// Resizes the last column of the GridView to fill the remaining space.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ResizeColumns(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.View is GridView gridView && gridView.Columns.LastOrDefault() is { } lastColumn)
            {
                lastColumn.Width = double.NaN; // Auto size to fit content
            }
        }
    }
}
