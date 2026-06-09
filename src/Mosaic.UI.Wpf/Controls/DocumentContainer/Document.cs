/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a document displayed by a <see cref="DocumentContainer"/>.
    /// </summary>
    [ContentProperty(nameof(Content))]
    public partial class Document : ObservableObject
    {
        /// <summary>
        /// Gets or sets the title displayed in the document tab.
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the WPF content displayed when the document is active.
        /// </summary>
        [ObservableProperty]
        public partial object? Content { get; set; }

        /// <summary>
        /// Gets or sets the tooltip displayed when the pointer rests over the document tab. A null
        /// value indicates no ToolTip should be displayed.
        /// </summary>
        [ObservableProperty]
        public partial object? ToolTip { get; set; } = null;

        /// <summary>
        /// Gets or sets a value that indicates whether the document can be closed by the user.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the document can be closed; otherwise, <see langword="false"/>.
        /// The default is <see langword="true"/>.
        /// </value>
        [ObservableProperty]
        public partial bool CanClose { get; set; } = true;

        /// <summary>
        /// Returns the title of the document.
        /// </summary>
        /// <returns>The value of <see cref="Title"/>.</returns>
        public override string ToString()
        {
            return Title;
        }
    }
}
