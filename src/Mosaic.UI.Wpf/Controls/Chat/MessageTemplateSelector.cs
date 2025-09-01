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
    /// Provides a mechanism to select a <see cref="DataTemplate"/> for messages based on their direction and the
    /// direction of the previous message.
    /// </summary>
    public class MessageTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual structure of sent messages.
        /// </summary>
        /// <remarks>
        /// A sent message under a received message.
        /// </remarks>
        public DataTemplate? SentTemplateA { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual structure of sent messages.
        /// </summary>
        /// <remarks>
        /// Received under sent.
        /// </remarks>
        public DataTemplate? ReceivedTemplateB { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual structure of sent messages.
        /// </summary>
        /// <remarks>
        /// A received message under received message.
        /// </remarks>
        public DataTemplate? ReceivedTemplateC { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to define the visual structure of sent messages.
        /// </summary>
        /// <remarks>
        /// A sent message under a sent message.
        /// </remarks>
        public DataTemplate? SentTemplateD { get; set; }

        /// <summary>
        /// Selects an appropriate <see cref="DataTemplate"/> based on the properties of the provided <paramref
        /// name="item"/>.
        /// </summary>
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item is not Message message)
            {
                return ReceivedTemplateC;
            }

            if (message.Direction == MessageDirection.Sent)
            {
                return message.PreviousMessageDirection == MessageDirection.Sent ? SentTemplateA : SentTemplateD;
            }

            return message.PreviousMessageDirection == MessageDirection.Received ? ReceivedTemplateC : ReceivedTemplateB;
        }
    }
}