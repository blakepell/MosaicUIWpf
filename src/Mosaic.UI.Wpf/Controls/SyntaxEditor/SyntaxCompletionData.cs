/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A general purpose <see cref="ICompletionData"/> implementation for the AvalonEdit completion
    /// window used by <see cref="SyntaxEditor"/> based controls.
    /// </summary>
    public class SyntaxCompletionData : ICompletionData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxCompletionData"/> class.
        /// </summary>
        /// <param name="text">The text inserted into the document when the entry is committed.</param>
        /// <param name="description">Optional detail shown in the description tool tip.</param>
        /// <param name="contentPrefix">Optional prefix shown before <paramref name="text"/> in the list.</param>
        /// <param name="priority">The sort priority; higher values sort earlier.</param>
        public SyntaxCompletionData(string text, object? description = null, string? contentPrefix = null, double priority = 1.0)
        {
            this.Text = text;
            this.Description = description;
            this.ContentPrefix = contentPrefix ?? string.Empty;
            this.Priority = priority;
        }

        /// <summary>
        /// An optional icon shown to the left of the entry.
        /// </summary>
        public ImageSource? Image { get; set; }

        /// <inheritdoc />
        public string Text { get; }

        /// <summary>
        /// A prefix rendered before <see cref="Text"/> in the completion list, useful for
        /// distinguishing categories of entry (for example <c>"[T] "</c> for a table).
        /// </summary>
        public string ContentPrefix { get; set; }

        /// <inheritdoc />
        public object Content => $"{this.ContentPrefix}{this.Text}";

        /// <inheritdoc />
        public object? Description { get; set; }

        /// <inheritdoc />
        public double Priority { get; set; }

        /// <inheritdoc />
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
