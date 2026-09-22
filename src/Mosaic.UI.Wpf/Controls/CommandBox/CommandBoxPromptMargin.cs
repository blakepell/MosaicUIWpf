/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Draws the <see cref="CommandBox.Prompt"/> glyph to the left of the editable text.
    /// </summary>
    /// <remarks>
    /// The prompt is an AvalonEdit left margin rather than a sibling control, so it lives inside
    /// the editor's own border and background and the text simply starts to the right of it. The
    /// element paints nothing but the glyph, which lets the command box's
    /// <see cref="System.Windows.Controls.Control.Background"/> show through and makes the prompt
    /// read as part of the box. It does not scroll with the text.
    /// </remarks>
    internal sealed class CommandBoxPromptMargin : FrameworkElement
    {
        /// <summary>
        /// The command box the prompt is drawn for.
        /// </summary>
        private readonly CommandBox _owner;

        /// <summary>
        /// Creates a new prompt margin.
        /// </summary>
        /// <param name="owner">The command box the prompt is drawn for.</param>
        internal CommandBoxPromptMargin(CommandBox owner)
        {
            _owner = owner;

            // The glyph is decoration; clicks belong to the text area behind it so that clicking
            // the prompt focuses the box and places the caret as any other click would.
            this.IsHitTestVisible = false;
        }

        /// <summary>
        /// Re-measures and repaints the prompt after the text, brush, padding or font changed.
        /// </summary>
        internal void Refresh()
        {
            this.InvalidateMeasure();
            this.InvalidateVisual();
        }

        /// <summary>
        /// Reports the width the glyph and its padding need. The height is ignored by the text
        /// area, which stretches every left margin to the height of the editor.
        /// </summary>
        /// <param name="availableSize">The space offered by the text area.</param>
        /// <returns>The size the prompt wants.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var formattedText = this.CreateFormattedText();

            if (formattedText == null)
            {
                return new Size(0, 0);
            }

            var padding = _owner.PromptPadding;

            return new Size(
                formattedText.WidthIncludingTrailingWhitespace + padding.Left + padding.Right,
                formattedText.Height + padding.Top + padding.Bottom);
        }

        /// <summary>
        /// Paints the prompt glyph.
        /// </summary>
        /// <param name="drawingContext">The drawing context to render into.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var formattedText = this.CreateFormattedText();

            if (formattedText == null)
            {
                return;
            }

            var padding = _owner.PromptPadding;

            // Line the glyph up with the first text line rather than with the margin, which the
            // text area may have stretched taller than the single row the box shows.
            double lineHeight = _owner.TextArea.TextView.DefaultLineHeight;
            double y = padding.Top + Math.Max(0, (lineHeight - formattedText.Height) / 2.0);

            drawingContext.DrawText(formattedText, new Point(padding.Left, y));
        }

        /// <summary>
        /// Builds the formatted glyph from the owner's font and prompt settings, or returns
        /// <see langword="null"/> when there is nothing to draw.
        /// </summary>
        /// <returns>The formatted prompt text, or <see langword="null"/>.</returns>
        private FormattedText? CreateFormattedText()
        {
            string prompt = _owner.Prompt;

            if (string.IsNullOrEmpty(prompt))
            {
                return null;
            }

            var brush = _owner.PromptBrush ?? _owner.Foreground;

            if (brush == null)
            {
                return null;
            }

            var typeface = new Typeface(_owner.FontFamily, _owner.FontStyle, _owner.FontWeight, _owner.FontStretch);

            return new FormattedText(
                prompt,
                CultureInfo.CurrentCulture,
                _owner.FlowDirection,
                typeface,
                _owner.FontSize,
                brush,
                VisualTreeHelper.GetDpi(_owner).PixelsPerDip);
        }
    }
}
