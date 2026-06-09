/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents the tab container generated for a <see cref="Document"/>.
    /// </summary>
    public class DocumentTabItem : System.Windows.Controls.TabItem
    {
        /// <summary>
        /// Identifies the <see cref="Document"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
            nameof(Document),
            typeof(Document),
            typeof(DocumentTabItem),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CanClose"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register(
            nameof(CanClose),
            typeof(bool),
            typeof(DocumentTabItem),
            new FrameworkPropertyMetadata(true));

        static DocumentTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DocumentTabItem),
                new FrameworkPropertyMetadata(typeof(DocumentTabItem)));
        }

        /// <summary>
        /// Gets or sets the document represented by this tab item.
        /// </summary>
        /// <value>The document represented by the tab item.</value>
        public Document? Document
        {
            get => (Document?)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the close button is displayed.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the close button is displayed; otherwise, <see langword="false"/>.
        /// The default is <see langword="true"/>.
        /// </value>
        public bool CanClose
        {
            get => (bool)GetValue(CanCloseProperty);
            set => SetValue(CanCloseProperty, value);
        }
    }
}
