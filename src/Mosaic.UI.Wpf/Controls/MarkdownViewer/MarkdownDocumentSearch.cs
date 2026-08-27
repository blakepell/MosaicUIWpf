/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Documents;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single hit produced by <see cref="MarkdownDocumentSearch"/>. A match lives either in the
    /// flow document itself (<see cref="Range"/>) or inside a <see cref="SyntaxEditor"/> hosted by
    /// the document for a code block (<see cref="Editor"/>).
    /// </summary>
    internal sealed class MarkdownSearchMatch
    {
        /// <summary>
        /// The matched text range in the flow document, or <c>null</c> for an editor match.
        /// </summary>
        public TextRange? Range { get; init; }

        /// <summary>
        /// The code block editor holding the match, or <c>null</c> for a flow document match.
        /// </summary>
        public SyntaxEditor? Editor { get; init; }

        /// <summary>
        /// The offset of the match within the <see cref="Editor"/> document.
        /// </summary>
        public int EditorOffset { get; init; }

        /// <summary>
        /// The length of the match in characters.
        /// </summary>
        public int Length { get; init; }
    }

    /// <summary>
    /// Finds text in a rendered Markdown <see cref="FlowDocument"/>, including the text of the
    /// read-only <see cref="SyntaxEditor"/> instances that host multi-line code blocks.
    /// </summary>
    /// <remarks>
    /// The document is flattened into a single searchable buffer while a piece table records where
    /// each fragment came from, so a match can be mapped back to a <see cref="TextPointer"/> range
    /// or to an offset inside a code block editor. Block boundaries contribute a line break to the
    /// buffer so a match never spans two unrelated blocks; inline boundaries do not, so a phrase is
    /// still found across a formatting change such as <c>this is **bold** text</c>.
    /// </remarks>
    internal static class MarkdownDocumentSearch
    {
        /// <summary>
        /// Builds a regular expression for the supplied find options.
        /// </summary>
        /// <param name="pattern">The text or regular expression to search for.</param>
        /// <param name="matchCase">Whether the search is case sensitive.</param>
        /// <param name="wholeWords">Whether the pattern must match whole words.</param>
        /// <param name="useRegex">Whether <paramref name="pattern"/> is a regular expression.</param>
        /// <returns>The compiled expression, or <c>null</c> when the pattern is empty or invalid.</returns>
        public static Regex? BuildRegex(string? pattern, bool matchCase, bool wholeWords, bool useRegex)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return null;
            }

            string expression = useRegex ? pattern : Regex.Escape(pattern);

            if (wholeWords)
            {
                expression = @"\b(?:" + expression + @")\b";
            }

            var options = RegexOptions.CultureInvariant | (matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);

            try
            {
                return new Regex(expression, options);
            }
            catch (ArgumentException ex)
            {
                // An incomplete expression is expected while the user is still typing one.
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Returns every match of <paramref name="regex"/> in the document, in document order.
        /// </summary>
        /// <param name="document">The document to search.</param>
        /// <param name="regex">The expression to search for.</param>
        public static List<MarkdownSearchMatch> FindAll(FlowDocument? document, Regex? regex)
        {
            var results = new List<MarkdownSearchMatch>();

            if (document == null || regex == null)
            {
                return results;
            }

            var builder = new StringBuilder();
            var textPieces = new List<(int Start, int Length, TextPointer Pointer)>();
            var editorPieces = new List<(int Start, int Length, SyntaxEditor Editor)>();

            Flatten(document, builder, textPieces, editorPieces);

            foreach (Match match in regex.Matches(builder.ToString()))
            {
                if (match.Length == 0)
                {
                    continue;
                }

                var editorPiece = editorPieces.FirstOrDefault(p =>
                    match.Index >= p.Start && match.Index + match.Length <= p.Start + p.Length);

                if (editorPiece.Editor != null)
                {
                    results.Add(new MarkdownSearchMatch
                    {
                        Editor = editorPiece.Editor,
                        EditorOffset = match.Index - editorPiece.Start,
                        Length = match.Length
                    });

                    continue;
                }

                var start = ResolvePointer(textPieces, match.Index, false);
                var end = ResolvePointer(textPieces, match.Index + match.Length, true);

                // A null pointer means the match straddled a block separator or a code block, which
                // is not a range the document can select; skip it rather than selecting the wrong text.
                if (start != null && end != null && start.CompareTo(end) < 0)
                {
                    results.Add(new MarkdownSearchMatch { Range = new TextRange(start, end), Length = match.Length });
                }
            }

            return results;
        }

        /// <summary>
        /// Walks the document, appending its searchable text to <paramref name="builder"/> and
        /// recording where each fragment originated.
        /// </summary>
        private static void Flatten(
            FlowDocument document,
            StringBuilder builder,
            List<(int Start, int Length, TextPointer Pointer)> textPieces,
            List<(int Start, int Length, SyntaxEditor Editor)> editorPieces)
        {
            var pointer = document.ContentStart;

            while (pointer != null)
            {
                switch (pointer.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.Text:
                        string run = pointer.GetTextInRun(LogicalDirection.Forward);

                        if (run.Length > 0)
                        {
                            textPieces.Add((builder.Length, run.Length, pointer));
                            builder.Append(run);
                        }

                        pointer = pointer.GetPositionAtOffset(run.Length, LogicalDirection.Forward);
                        continue;

                    case TextPointerContext.EmbeddedElement:
                        var editor = FindSyntaxEditor(pointer.GetAdjacentElement(LogicalDirection.Forward));

                        if (editor != null)
                        {
                            string text = editor.Text ?? string.Empty;
                            editorPieces.Add((builder.Length, text.Length, editor));
                            builder.Append(text);
                        }

                        builder.Append('\n');
                        break;

                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        // Only block boundaries separate text; inline boundaries are transparent so a
                        // phrase can still be found across a bold or italic run.
                        if (pointer.GetAdjacentElement(LogicalDirection.Forward) is not Inline)
                        {
                            builder.Append('\n');
                        }

                        break;
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        /// <summary>
        /// Maps a buffer offset back to a position in the flow document.
        /// </summary>
        /// <param name="pieces">The text pieces recorded while flattening.</param>
        /// <param name="offset">The offset into the flattened buffer.</param>
        /// <param name="isEnd">Whether the offset is the exclusive end of a match.</param>
        /// <returns>The matching position, or <c>null</c> when the offset falls outside every piece.</returns>
        private static TextPointer? ResolvePointer(
            List<(int Start, int Length, TextPointer Pointer)> pieces,
            int offset,
            bool isEnd)
        {
            foreach (var piece in pieces)
            {
                bool contains = isEnd
                    ? offset > piece.Start && offset <= piece.Start + piece.Length
                    : offset >= piece.Start && offset < piece.Start + piece.Length;

                if (contains)
                {
                    return piece.Pointer.GetPositionAtOffset(offset - piece.Start, LogicalDirection.Forward);
                }
            }

            return null;
        }

        /// <summary>
        /// Locates the <see cref="SyntaxEditor"/> hosted by an embedded document element, which the
        /// renderer wraps in a <see cref="Border"/>.
        /// </summary>
        /// <param name="element">The embedded element to inspect.</param>
        public static SyntaxEditor? FindSyntaxEditor(DependencyObject? element)
        {
            switch (element)
            {
                case null:
                    return null;
                case SyntaxEditor editor:
                    return editor;
                case Decorator decorator:
                    return FindSyntaxEditor(decorator.Child);
                case ContentControl contentControl:
                    return FindSyntaxEditor(contentControl.Content as DependencyObject);
                case Panel panel:
                    return panel.Children.Cast<UIElement>().Select(FindSyntaxEditor).FirstOrDefault(x => x != null);
                default:
                    return null;
            }
        }
    }
}
