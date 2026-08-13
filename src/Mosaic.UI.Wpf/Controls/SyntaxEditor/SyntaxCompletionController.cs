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
using Mosaic.UI.Wpf.Themes;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Identifies what caused a completion list to be requested.
    /// </summary>
    public enum SyntaxCompletionTrigger
    {
        /// <summary>
        /// The user explicitly asked for completion, typically with Ctrl+Space.
        /// </summary>
        Explicit,

        /// <summary>
        /// A member access character (<c>.</c>) was typed.
        /// </summary>
        MemberAccess,

        /// <summary>
        /// White space was typed, completing the word that precedes it.
        /// </summary>
        WordBoundary
    }

    /// <summary>
    /// Describes a completion request handed to <see cref="SyntaxCompletionController.ProvideCompletions"/>.
    /// </summary>
    /// <param name="Trigger">What caused the request.</param>
    /// <param name="PrecedingText">
    /// The word before the caret. For <see cref="SyntaxCompletionTrigger.MemberAccess"/> this is the
    /// identifier that qualified the dot; for <see cref="SyntaxCompletionTrigger.WordBoundary"/> it is
    /// the word that preceded the white space. Never <see langword="null"/>, but may be empty.
    /// </param>
    /// <param name="Offset">The caret offset at the time of the request.</param>
    public readonly record struct SyntaxCompletionRequest(SyntaxCompletionTrigger Trigger, string PrecedingText, int Offset);

    /// <summary>
    /// Drives the AvalonEdit completion window for a <see cref="SyntaxEditor"/>: it owns the window's
    /// lifetime, applies the Mosaic theme to it, and translates keystrokes into completion requests.
    /// The language specific item list is supplied through <see cref="ProvideCompletions"/>.
    /// </summary>
    /// <remarks>
    /// The controller is deliberately defensive about the states that make a naive completion
    /// implementation throw: caret offsets near the start of the document, an empty candidate list,
    /// and committing an entry while nothing is selected are all handled here rather than by callers.
    /// </remarks>
    public sealed class SyntaxCompletionController : IDisposable
    {
        private static readonly Uri CompletionResourceUri = new("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/AvalonEdit/CompletionWindow.xaml", UriKind.Absolute);

        private readonly SyntaxEditor _editor;
        private CompletionWindow? _window;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxCompletionController"/> class and
        /// subscribes to the editor's text input events.
        /// </summary>
        /// <param name="editor">The editor to drive completion for.</param>
        public SyntaxCompletionController(SyntaxEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _editor.TextArea.TextEntered += this.OnTextEntered;
            _editor.TextArea.TextEntering += this.OnTextEntering;
        }

        /// <summary>
        /// Gets or sets a value indicating whether completion is offered. Setting this to
        /// <see langword="false"/> also closes any open window.
        /// </summary>
        public bool IsEnabled
        {
            get;
            set
            {
                field = value;

                if (!value)
                {
                    this.Close();
                }
            }
        } = true;

        /// <summary>
        /// Supplies the candidate entries for a request. Return an empty list (or <see langword="null"/>)
        /// to decline; no window is shown in that case.
        /// </summary>
        public Func<SyntaxCompletionRequest, IReadOnlyList<ICompletionData>?>? ProvideCompletions { get; set; }

        /// <summary>
        /// Gets a value indicating whether a completion window is currently open.
        /// </summary>
        public bool IsOpen => _window != null;

        /// <summary>
        /// Requests completion explicitly, as if the user had pressed Ctrl+Space.
        /// </summary>
        public void RequestCompletion()
        {
            if (_editor.Document == null)
            {
                return;
            }

            int caret = _editor.CaretOffset;
            this.Show(new SyntaxCompletionRequest(SyntaxCompletionTrigger.Explicit, GetIdentifierBefore(_editor.Document, caret), caret));
        }

        /// <summary>
        /// Closes the completion window if one is open.
        /// </summary>
        public void Close()
        {
            var window = _window;
            _window = null;

            if (window != null)
            {
                window.Closed -= this.OnWindowClosed;
                window.Close();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _editor.TextArea.TextEntered -= this.OnTextEntered;
            _editor.TextArea.TextEntering -= this.OnTextEntering;
            this.Close();
        }

        /// <summary>
        /// Opens a completion window for the specified request when the provider returns candidates.
        /// </summary>
        /// <param name="request">The request to satisfy.</param>
        private void Show(SyntaxCompletionRequest request)
        {
            if (!this.IsEnabled || this.ProvideCompletions == null)
            {
                return;
            }

            // Replacing a live window without closing it first orphans the old one and leaves a
            // stale popup on screen.
            this.Close();

            IReadOnlyList<ICompletionData>? items;

            try
            {
                items = this.ProvideCompletions(request);
            }
            catch (Exception)
            {
                // A misbehaving provider must not take down the editor mid keystroke.
                return;
            }

            // AvalonEdit throws when an entry is committed out of an empty list, so never open one.
            if (items == null || items.Count == 0)
            {
                return;
            }

            var window = new CompletionWindow(_editor.TextArea);
            ApplyTheme(window);

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            window.Closed += this.OnWindowClosed;
            _window = window;
            window.Show();
        }

        /// <summary>
        /// Handles a character having been inserted into the document.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data for the text input.</param>
        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (!this.IsEnabled || e.Text.Length == 0)
            {
                return;
            }

            var document = _editor.Document;

            if (document == null)
            {
                return;
            }

            char character = e.Text[0];
            int caret = _editor.CaretOffset;

            if (character == '.')
            {
                // The caret sits after the dot, so the qualifier ends one character further back.
                this.Show(new SyntaxCompletionRequest(SyntaxCompletionTrigger.MemberAccess, GetIdentifierBefore(document, caret - 1), caret));
            }
            else if (char.IsWhiteSpace(character))
            {
                this.Show(new SyntaxCompletionRequest(SyntaxCompletionTrigger.WordBoundary, GetWordBefore(document, caret), caret));
            }
        }

        /// <summary>
        /// Handles a character about to be inserted, committing the selected entry when the character
        /// terminates the current word.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data for the text input.</param>
        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            var window = _window;

            if (window == null || e.Text.Length == 0)
            {
                return;
            }

            char character = e.Text[0];

            if (char.IsLetterOrDigit(character) || character == '_')
            {
                return;
            }

            // Filtering can empty the list entirely; committing with no selection throws inside
            // AvalonEdit, so dismiss instead. The typed character still gets inserted either way.
            if (window.CompletionList.SelectedItem == null)
            {
                window.Close();
                return;
            }

            window.CompletionList.RequestInsertion(e);
        }

        /// <summary>
        /// Clears the cached window reference once it closes.
        /// </summary>
        /// <param name="sender">The window that closed.</param>
        /// <param name="e">The event data.</param>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (sender is CompletionWindow window)
            {
                window.Closed -= this.OnWindowClosed;
            }

            if (ReferenceEquals(_window, sender))
            {
                _window = null;
            }
        }

        /// <summary>
        /// Applies the Mosaic theme to a completion window. The window is a separate top level
        /// <see cref="Window"/>, so it does not inherit the editor's resource scope and has to be
        /// dressed explicitly each time it opens. Re-resolving on every open is what lets the popup
        /// follow a theme change without subscribing to one.
        /// </summary>
        /// <param name="window">The window to theme.</param>
        private static void ApplyTheme(CompletionWindow window)
        {
            try
            {
                window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = CompletionResourceUri });
            }
            catch (Exception)
            {
                // Fall through to the direct property assignments below.
            }

            window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.ControlTextBackgroundBrush);
            window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);
            window.SetResourceReference(Control.BorderBrushProperty, MosaicTheme.ControlBorderBrush);
            window.BorderThickness = new Thickness(1);

            window.CompletionList.SetResourceReference(Control.BackgroundProperty, MosaicTheme.ControlTextBackgroundBrush);
            window.CompletionList.SetResourceReference(Control.ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);
        }

        /// <summary>
        /// Reads the word that ends at <paramref name="offset"/>, skipping any white space directly
        /// before it. Used to answer "what word preceded the space the user just typed".
        /// </summary>
        /// <param name="text">The document to scan.</param>
        /// <param name="offset">The exclusive end of the scan; offsets outside the document are clamped.</param>
        /// <returns>The word, or an empty string when there is none.</returns>
        public static string GetWordBefore(ITextSource text, int offset)
        {
            if (text == null)
            {
                return string.Empty;
            }

            int index = Math.Min(offset, text.TextLength) - 1;

            while (index >= 0 && char.IsWhiteSpace(text.GetCharAt(index)))
            {
                index--;
            }

            int end = index + 1;

            while (index >= 0 && IsWordCharacter(text.GetCharAt(index)))
            {
                index--;
            }

            int start = index + 1;

            return end > start ? text.GetText(start, end - start) : string.Empty;
        }

        /// <summary>
        /// Reads the identifier that ends exactly at <paramref name="offset"/>, without skipping white
        /// space. Used to answer "what was the qualifier before the dot".
        /// </summary>
        /// <param name="text">The document to scan.</param>
        /// <param name="offset">The exclusive end of the scan; offsets outside the document are clamped.</param>
        /// <returns>The identifier, or an empty string when there is none.</returns>
        public static string GetIdentifierBefore(ITextSource text, int offset)
        {
            if (text == null)
            {
                return string.Empty;
            }

            int end = Math.Max(0, Math.Min(offset, text.TextLength));
            int index = end - 1;

            while (index >= 0 && IsWordCharacter(text.GetCharAt(index)))
            {
                index--;
            }

            int start = index + 1;

            return end > start ? text.GetText(start, end - start) : string.Empty;
        }

        /// <summary>
        /// Determines whether a character can appear inside an identifier.
        /// </summary>
        /// <param name="character">The character to test.</param>
        /// <returns><see langword="true"/> when the character is part of a word.</returns>
        private static bool IsWordCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
        }
    }
}
