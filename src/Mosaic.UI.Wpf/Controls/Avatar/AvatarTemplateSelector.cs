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
    /// Provides logic to select an appropriate <see cref="DataTemplate"/> based on the type of the provided item.
    /// </summary>
    public class AvatarTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual representation of text content.
        /// </summary>
        public DataTemplate? TextTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual structure of a <see
        /// cref="FrameworkElement"/>.
        /// </summary>
        public DataTemplate? FrameworkElementTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display images.
        /// </summary>
        public DataTemplate? ImageTemplate { get; set; }

        /// <summary>
        /// Selects an appropriate <see cref="DataTemplate"/> based on the type of the provided item.
        /// </summary>
        public override DataTemplate SelectTemplate(object? item, DependencyObject container)
        {
            // If an ImageSource is provided we'll handle is specifically.
            if (item is ImageSource)
            {
                return ImageTemplate!;
            }

            // Otherwise we'll catch all FrameworkElements and put them into as ContentControl.
            if (item is FrameworkElement)
            {
                return FrameworkElementTemplate!;
            }

            // And let anything else fall through to the TextTemplate.
            return TextTemplate!;
        }
    }
}
