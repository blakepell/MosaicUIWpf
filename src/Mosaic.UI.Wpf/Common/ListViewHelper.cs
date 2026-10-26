/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides attached properties for <see cref="ListView"/> and its <see cref="ListViewItem"/> containers.
    /// </summary>
    /// <remarks>
    /// The alternating row backgrounds are applied by the Mosaic native <see cref="ListViewItem"/> style
    /// (<c>ThemeManager.Native="True"</c>), using the <see cref="Themes.MosaicTheme.ListViewItemBackgroundBrush"/>
    /// and <see cref="Themes.MosaicTheme.ListViewItemAlternateBackgroundBrush"/> theme resources.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// <ListView common:ListViewHelper.IsAlternatingRowsEnabled="True" ItemsSource="{Binding Items}" />
    /// ]]>
    /// </code>
    /// </example>
    public class ListViewHelper
    {
        /// <summary>
        /// Identifies the attached property that stores the <see cref="ItemsControl.AlternationCount"/> value
        /// that was in place before alternating rows were enabled, so it can be restored when disabled.
        /// </summary>
        private static readonly DependencyProperty PreviousAlternationCountProperty =
            DependencyProperty.RegisterAttached("PreviousAlternationCount", typeof(int?), typeof(ListViewHelper),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the attached property that enables alternating row backgrounds on a <see cref="ListView"/>.
        /// The value is inherited by the generated <see cref="ListViewItem"/> containers so their style can
        /// react to it together with <see cref="ItemsControl.AlternationIndexProperty"/>.
        /// </summary>
        public static readonly DependencyProperty IsAlternatingRowsEnabledProperty =
            DependencyProperty.RegisterAttached("IsAlternatingRowsEnabled", typeof(bool), typeof(ListViewHelper),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnIsAlternatingRowsEnabledChanged));

        /// <summary>
        /// Gets a value that indicates whether alternating row backgrounds are enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns><see langword="true" /> if alternating row backgrounds are enabled; otherwise, <see langword="false" />.</returns>
        public static bool GetIsAlternatingRowsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAlternatingRowsEnabledProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether alternating row backgrounds are enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value"><see langword="true" /> to enable alternating row backgrounds; otherwise, <see langword="false" />.</param>
        public static void SetIsAlternatingRowsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAlternatingRowsEnabledProperty, value);
        }

        /// <summary>
        /// Ensures the <see cref="ListView"/> produces alternation indexes when the feature is enabled, and
        /// restores the original <see cref="ItemsControl.AlternationCount"/> when it is disabled.
        /// </summary>
        /// <param name="d">The element whose value changed.</param>
        /// <param name="e">The event data.</param>
        private static void OnIsAlternatingRowsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The value is inherited by the item containers (and any nested ListView), only a ListView
            // that had the value set on it directly manages its count.
            if (d is not ListView listView
                || DependencyPropertyHelper.GetValueSource(listView, IsAlternatingRowsEnabledProperty).BaseValueSource == BaseValueSource.Inherited)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                // Leave a consumer supplied count of 2 or more alone.
                if (listView.AlternationCount < 2)
                {
                    listView.SetValue(PreviousAlternationCountProperty, listView.AlternationCount);
                    listView.AlternationCount = 2;
                }
            }
            else if (listView.GetValue(PreviousAlternationCountProperty) is int previous)
            {
                listView.AlternationCount = previous;
                listView.ClearValue(PreviousAlternationCountProperty);
            }
        }
    }
}
