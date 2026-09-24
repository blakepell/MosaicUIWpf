/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// The call whose argument list contains the caret.
/// </summary>
/// <param name="OpenParenOffset">The offset of the call's opening parenthesis.</param>
/// <param name="NameOffset">The offset of the qualifier (or the type name after new).</param>
/// <param name="Qualifier">The module alias before the dot, or null for a constructor or bare call.</param>
/// <param name="Name">The method or constructed type name.</param>
/// <param name="IsConstructor">Whether the call is preceded by new.</param>
/// <param name="ArgumentIndex">The zero based argument the caret is in.</param>
/// <param name="ArgumentCount">The number of arguments in the whole call; an empty list has none.</param>
internal readonly record struct ScriptCallContext(int OpenParenOffset, int NameOffset, string? Qualifier, string Name, bool IsConstructor, int ArgumentIndex = 0, int ArgumentCount = 0)
{
    /// <summary>
    /// Identifies the callee independently of the caret, such as app.Add or new AppValue.
    /// </summary>
    public string Key => IsConstructor ? $"new {Name}" : Qualifier == null ? Name : $"{Qualifier}.{Name}";
}

/// <summary>
/// Locates the call surrounding the caret in JavaScript source. It is a tolerant scanner rather than a
/// parser: strings, comments, template literals and bracket nesting are honored so commas only count at
/// the call's own level, and unbalanced text (the usual state while typing) never throws.
/// </summary>
internal static class ScriptCallParser
{
    /// <summary>
    /// How far past the caret to look for the rest of the argument list.
    /// </summary>
    private const int MaxLookahead = 64 * 1024;

    /// <summary>
    /// Finds the innermost call containing the caret that the predicate accepts. Calls it rejects (such as
    /// an unregistered function) and grouping parentheses are skipped so an enclosing call still shows.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <param name="caret">The caret offset.</param>
    /// <param name="accept">Decides whether a call has signatures to show; null accepts every call.</param>
    public static ScriptCallContext? Find(string text, int caret, Func<ScriptCallContext, bool>? accept = null)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var scanner = new Scanner(text);
        scanner.Advance(caret);
        if (scanner.Mode is ScanMode.LineComment or ScanMode.BlockComment)
        {
            return null;
        }

        for (int depth = scanner.Stack.Count - 1; depth >= 0; depth--)
        {
            var frame = scanner.Stack[depth];
            // A function body ends the search: its statements are not arguments of the enclosing call.
            if (frame.Kind == FrameKind.Block)
            {
                return null;
            }

            if (frame.Kind != FrameKind.Paren || ReadCallee(text, frame.Offset) is not { } callee)
            {
                continue;
            }

            var context = callee with { ArgumentIndex = frame.Commas };
            if (accept != null && !accept(context))
            {
                continue;
            }

            var closed = scanner.Advance((int)Math.Min(text.Length, (long)caret + MaxLookahead), depth) ?? scanner.Stack[depth];
            return context with { ArgumentCount = closed.HasContent ? closed.Commas + 1 : 0 };
        }
        return null;
    }

    private static ScriptCallContext? ReadCallee(string text, int openParen)
    {
        int nameEnd = SkipWhitespaceBack(text, openParen - 1) + 1;
        int nameStart = ReadIdentifierBack(text, nameEnd);
        if (nameStart == nameEnd || char.IsDigit(text[nameStart]))
        {
            return null;
        }

        string name = text[nameStart..nameEnd];
        int before = SkipWhitespaceBack(text, nameStart - 1);
        if (before >= 0 && text[before] == '.')
        {
            int qualifierEnd = SkipWhitespaceBack(text, before > 0 && text[before - 1] == '?' ? before - 2 : before - 1) + 1;
            int qualifierStart = ReadIdentifierBack(text, qualifierEnd);
            int preceding = SkipWhitespaceBack(text, qualifierStart - 1);
            // Only root aliases resolve: x.app.Add is not the registered app module.
            if (qualifierStart == qualifierEnd || char.IsDigit(text[qualifierStart]) || (preceding >= 0 && text[preceding] is '.' or '?'))
            {
                return null;
            }

            return new ScriptCallContext(openParen, qualifierStart, text[qualifierStart..qualifierEnd], name, false);
        }
        int wordStart = ReadIdentifierBack(text, before + 1);
        bool isNew = text.AsSpan(wordStart, before + 1 - wordStart) is "new";
        return new ScriptCallContext(openParen, nameStart, null, name, isNew);
    }

    private static int SkipWhitespaceBack(string text, int index)
    {
        while (index >= 0 && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        return index;
    }

    private static int ReadIdentifierBack(string text, int end)
    {
        int start = end;
        while (start > 0 && IsIdentifierPart(text[start - 1]))
        {
            start--;
        }

        return start;
    }

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '$';

    private enum FrameKind { Paren, Bracket, Brace, Block, Template, TemplateExpression }

    private enum ScanMode { Code, LineComment, BlockComment, SingleQuote, DoubleQuote }

    private record struct Frame(FrameKind Kind, int Offset, int Commas = 0, bool HasContent = false);

    private sealed class Scanner(string text)
    {
        public readonly List<Frame> Stack = [];
        public ScanMode Mode;
        private int _position;

        /// <summary>
        /// Scans up to end, or until the frame at stopDepth closes, in which case that frame is returned.
        /// </summary>
        public Frame? Advance(int end, int stopDepth = -1)
        {
            while (_position < end)
            {
                int i = _position++;
                char c = text[i];
                switch (Mode)
                {
                    case ScanMode.LineComment:
                        if (c == '\n')
                        {
                            Mode = ScanMode.Code;
                        }

                        continue;
                    case ScanMode.BlockComment:
                        if (c == '*' && At(i + 1) == '/')
                        {
                            Mode = ScanMode.Code;
                            _position++;
                        }
                        continue;
                    case ScanMode.SingleQuote or ScanMode.DoubleQuote:
                        if (c == '\\')
                        {
                            _position++;
                        }
                        else if (c == '\n' || c == (Mode == ScanMode.SingleQuote ? '\'' : '"'))
                        {
                            Mode = ScanMode.Code;
                        }

                        continue;
                }
                if (Stack.Count > 0 && Stack[^1].Kind == FrameKind.Template)
                {
                    if (c == '\\')
                    {
                        _position++;
                    }
                    else if (c == '`')
                    {
                        Stack.RemoveAt(Stack.Count - 1);
                    }
                    else if (c == '$' && At(i + 1) == '{')
                    {
                        _position++;
                        Stack.Add(new Frame(FrameKind.TemplateExpression, i));
                    }
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                if (c == '/' && At(i + 1) is '/' or '*')
                {
                    Mode = At(i + 1) == '/' ? ScanMode.LineComment : ScanMode.BlockComment;
                    _position++;
                    continue;
                }
                if (c is not (')' or ']' or '}') && Stack.Count > 0)
                {
                    Stack[^1] = Stack[^1] with { HasContent = true };
                }

                switch (c)
                {
                    case '\'':
                        Mode = ScanMode.SingleQuote;
                        break;
                    case '"':
                        Mode = ScanMode.DoubleQuote;
                        break;
                    case '`':
                        Stack.Add(new Frame(FrameKind.Template, i));
                        break;
                    case '(':
                        Stack.Add(new Frame(FrameKind.Paren, i));
                        break;
                    case '[':
                        Stack.Add(new Frame(FrameKind.Bracket, i));
                        break;
                    case '{':
                        Stack.Add(new Frame(IsBlock(i) ? FrameKind.Block : FrameKind.Brace, i));
                        break;
                    case ')' or ']' or '}':
                        if (Close(c, stopDepth) is { } closed)
                        {
                            return closed;
                        }

                        break;
                    case ',':
                        if (Stack.Count > 0 && Stack[^1].Kind == FrameKind.Paren)
                        {
                            Stack[^1] = Stack[^1] with { Commas = Stack[^1].Commas + 1 };
                        }

                        break;
                    // A statement terminator directly inside the call means its closing parenthesis is missing.
                    case ';' when stopDepth >= 0 && Stack.Count - 1 == stopDepth:
                        return Stack[stopDepth];
                }
            }
            return null;
        }

        private char At(int index) => index < text.Length ? text[index] : '\0';

        private Frame? Close(char closer, int stopDepth)
        {
            for (int index = Stack.Count - 1; index >= 0; index--)
            {
                var kind = Stack[index].Kind;
                bool matches = closer switch
                {
                    ')' => kind == FrameKind.Paren,
                    ']' => kind == FrameKind.Bracket,
                    _ => kind is FrameKind.Brace or FrameKind.Block or FrameKind.TemplateExpression
                };
                if (kind == FrameKind.Template)
                {
                    return null;
                }

                if (!matches)
                {
                    continue;
                }

                Frame? stopped = stopDepth >= 0 && index <= stopDepth ? Stack[stopDepth] : null;
                Stack.RemoveRange(index, Stack.Count - index);
                return stopped;
            }
            return null;
        }

        /// <summary>
        /// Distinguishes a statement block (function body, if, else) from an object literal argument.
        /// </summary>
        private bool IsBlock(int brace)
        {
            int previous = SkipWhitespaceBack(text, brace - 1);
            if (previous < 0)
            {
                return true;
            }

            char c = text[previous];
            if (c is ')' or ';' or '{' or '}' || (c == '>' && previous > 0 && text[previous - 1] == '='))
            {
                return true;
            }

            if (!IsIdentifierPart(c))
            {
                return false;
            }

            string word = text[ReadIdentifierBack(text, previous + 1)..(previous + 1)];
            return word is not ("return" or "typeof" or "yield" or "await" or "case" or "in" or "of" or "void" or "delete");
        }
    }
}

/// <summary>
/// One parameter of a signature; IsActive is true while the caret is in its argument.
/// </summary>
internal sealed class ScriptSignatureParameter(string type, string name, string description = "", string? defaultValue = null, bool isParams = false) : ObservableObject
{
    private bool _isActive;

    public string Type => type;
    public string Name => name;
    public string Description => description;
    public string? DefaultValue => defaultValue;
    public bool IsParams => isParams;

    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    public override string ToString() => $"{(IsParams ? "params " : "")}{Type} {Name}{(DefaultValue == null ? "" : $" = {DefaultValue}")}".Trim();
}

/// <summary>
/// One overload; IsActive is true for the overload that matches the arguments typed so far.
/// </summary>
internal sealed class ScriptSignature(string returnType, string name, IReadOnlyList<ScriptSignatureParameter> parameters, string summary, bool hasParamsArray) : ObservableObject
{
    private bool _isActive;

    public string ReturnType => returnType;
    public string Name => name;
    public IReadOnlyList<ScriptSignatureParameter> Parameters => parameters;
    public string Summary => summary;
    public bool HasParamsArray => hasParamsArray;

    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    /// <summary>
    /// Whether the overload can accept the given number of arguments; fewer is allowed while typing.
    /// </summary>
    public bool Accepts(int argumentCount) => HasParamsArray || Parameters.Count >= argumentCount;

    /// <summary>
    /// Maps an argument position to a parameter index, or -1 when the overload has no such parameter.
    /// </summary>
    public int GetParameterIndex(int argumentIndex) =>
        HasParamsArray && argumentIndex >= Parameters.Count - 1 ? Parameters.Count - 1 : argumentIndex < Parameters.Count ? argumentIndex : -1;

    public override string ToString() => $"{ReturnType} {Name}({string.Join(", ", Parameters)})".Trim();
}

/// <summary>
/// The overloads of one call and the overload and parameter matching the caret, shown by ScriptSignaturePopup.
/// </summary>
internal sealed class ScriptSignatureHelp : ObservableObject
{
    private ScriptSignature? _activeSignature;
    private ScriptSignatureParameter? _activeParameter;
    private bool _isUserSelected;
    private int _argumentIndex;

    public ScriptSignatureHelp(string key, IReadOnlyList<ScriptSignature> signatures)
    {
        Key = key;
        Signatures = signatures;
    }

    /// <summary>
    /// The callee the overloads belong to.
    /// </summary>
    public string Key { get; }

    public IReadOnlyList<ScriptSignature> Signatures { get; }

    public ScriptSignature? ActiveSignature { get => _activeSignature; private set => SetProperty(ref _activeSignature, value); }

    public ScriptSignatureParameter? ActiveParameter { get => _activeParameter; private set => SetProperty(ref _activeParameter, value); }

    /// <summary>
    /// Matches the caret's argument position. An overload the user picked is kept while it still fits,
    /// otherwise the overload with the fewest parameters that can take the arguments is chosen.
    /// </summary>
    /// <param name="argumentIndex">The argument containing the caret.</param>
    /// <param name="argumentCount">The number of arguments in the call.</param>
    public void Update(int argumentIndex, int argumentCount)
    {
        if (Signatures.Count == 0)
        {
            return;
        }

        _argumentIndex = argumentIndex;
        int required = Math.Max(argumentCount, argumentIndex + 1);
        if (_isUserSelected && ActiveSignature?.Accepts(required) == true)
        {
            Activate(ActiveSignature);
            return;
        }
        _isUserSelected = false;
        Activate(Signatures.FirstOrDefault(s => !s.HasParamsArray && s.Accepts(required))
            ?? Signatures.FirstOrDefault(s => s.HasParamsArray && s.Parameters.Count - 1 <= required)
            ?? Signatures.MaxBy(s => s.Parameters.Count)!);
    }

    /// <summary>
    /// Moves the active overload up or down, wrapping, and keeps it while it fits (Up and Down keys).
    /// </summary>
    public void Cycle(int delta)
    {
        if (Signatures.Count == 0)
        {
            return;
        }

        int index = ActiveSignature == null ? 0 : IndexOf(ActiveSignature);
        Select(Signatures[((index + delta) % Signatures.Count + Signatures.Count) % Signatures.Count]);
    }

    /// <summary>
    /// Makes an overload active until the arguments no longer fit it (a mouse click on the overload).
    /// </summary>
    public void Select(ScriptSignature signature)
    {
        if (IndexOf(signature) < 0)
        {
            return;
        }

        _isUserSelected = true;
        Activate(signature);
    }

    private int IndexOf(ScriptSignature signature)
    {
        for (int i = 0; i < Signatures.Count; i++)
        {
            if (ReferenceEquals(Signatures[i], signature))
            {
                return i;
            }
        }

        return -1;
    }

    private void Activate(ScriptSignature active)
    {
        int parameterIndex = active.GetParameterIndex(_argumentIndex);
        foreach (var signature in Signatures)
        {
            signature.IsActive = ReferenceEquals(signature, active);
            for (int i = 0; i < signature.Parameters.Count; i++)
            {
                signature.Parameters[i].IsActive = signature.IsActive && i == parameterIndex;
            }
        }
        ActiveSignature = active;
        ActiveParameter = parameterIndex >= 0 ? active.Parameters[parameterIndex] : null;
    }
}
