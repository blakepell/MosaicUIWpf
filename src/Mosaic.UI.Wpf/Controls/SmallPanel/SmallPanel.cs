/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a custom panel that arranges its child elements in a single layer and ensures that each child is
    /// measured and arranged within the available space.
    ///
    /// Use cases:
    /// - Overlay multiple visuals on top of each other (for example decorations, adorners, or layered effects) where
    ///   all children should share the same bounds rather than be stacked or tiled.
    /// - When you want a simple single-layer container that sizes to the largest child instead of stacking children
    ///   vertically or horizontally as StackPanel does.
    /// - Useful for templated controls, backgrounds, or overlays where deterministic measure/arrange behavior and
    ///   consistent sizing are required without the complexity of a Grid.
    /// - A lightweight alternative when only one layout layer is needed and you want child elements to occupy the
    ///   same available area.
    ///
    /// </summary>
    /// <remarks>
    /// This is based on code from https://github.com/WPFDevelopersOrg/WPFDevelopers available via the MIT License.
    /// </remarks>
    public class SmallPanel : Panel
    {
        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var gridDesiredSize = new Size();
            var children = InternalChildren;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child != null)
                {
                    child.Measure(constraint);
                    gridDesiredSize.Width = Math.Max(gridDesiredSize.Width, child.DesiredSize.Width);
                    gridDesiredSize.Height = Math.Max(gridDesiredSize.Height, child.DesiredSize.Height);
                }
            }

            return (gridDesiredSize);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var children = InternalChildren;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                child?.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }
    }
}
