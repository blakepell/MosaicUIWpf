/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Tasks;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// The .NET type of a script value as far as the editor can tell.
/// </summary>
/// <param name="Type">The reflected type.</param>
/// <param name="IsStatic">Whether the value is a type alias, exposing static members, rather than an instance.</param>
/// <param name="Registration">The registration the value came from, when it is a registered alias.</param>
internal readonly record struct ScriptValueType(Type Type, bool IsStatic, ScriptRegistration? Registration = null)
{
    public static ScriptValueType From(ScriptRegistration registration) => new(registration.Type, registration.IsType, registration);
}

/// <summary>
/// Infers the .NET types of script variables so members can be completed on them, for example
/// <c>let sb = new StringBuilder();</c> or <c>let p = panels.CreateTool('who', 'Who');</c>.
/// </summary>
/// <remarks>
/// Like <see cref="ScriptCallParser"/> this is a tolerant scanner rather than a parser: it tokenizes the
/// document up to the caret, tracks var, let and const declarations and plain reassignments in brace
/// scopes, and resolves each initializer that is a simple chain of new, registered aliases, variables,
/// property and field access, method calls (by overload return type), indexing and await. Anything
/// else, such as arithmetic or JavaScript objects, is unknown; an unknown variable still shadows an
/// outer variable or a registered alias of the same name, so nothing misleading is offered.
/// </remarks>
internal static class ScriptTypeInference
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "var", "let", "const", "function", "return", "if", "else", "for", "while", "do", "switch", "case", "break",
        "continue", "new", "typeof", "instanceof", "in", "of", "await", "async", "yield", "throw", "try", "catch",
        "finally", "class", "extends", "this", "super", "null", "undefined", "true", "false", "void", "delete",
        "default", "import", "export"
    };

    /// <summary>
    /// Resolves the member access target that ends at <paramref name="end"/>, such as <c>sb</c> or
    /// <c>panels.CreateTool('a', 'b')</c> when the caret follows the dot after it.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="text">The document text.</param>
    /// <param name="end">The offset just past the target, which is the offset of the dot (or of ?.).</param>
    /// <returns>The target's type, or null when it is unknown or the position is in a comment or string.</returns>
    public static ScriptValueType? ResolveTarget(ScriptEnvironment environment, string text, int end)
    {
        end = Math.Clamp(end, 0, text.Length);
        if (end > 0 && text[end - 1] == '?')
        {
            end--;
        }

        var tokens = ScriptTokenizer.Tokenize(text, end, out bool endsInTrivia);
        if (endsInTrivia)
        {
            return null;
        }

        var analyzer = new Analyzer(environment, tokens);
        analyzer.Run();
        int start = FindChainStart(tokens);
        if (start < 0)
        {
            return null;
        }

        int index = start;
        var value = analyzer.ResolveExpression(ref index);
        return index == tokens.Count ? value : null;
    }

    /// <summary>
    /// Gets the variables in scope at <paramref name="end"/> with their inferred types; unknown types are null.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="text">The document text.</param>
    /// <param name="end">The caret offset.</param>
    public static IReadOnlyDictionary<string, ScriptValueType?> GetVariables(ScriptEnvironment environment, string text, int end)
    {
        var analyzer = new Analyzer(environment, ScriptTokenizer.Tokenize(text, Math.Clamp(end, 0, text.Length), out _));
        analyzer.Run();
        return analyzer.GetVariables();
    }

    /// <summary>
    /// Walks back from the last token over identifiers, dots, call and index groups to where the chain starts.
    /// </summary>
    private static int FindChainStart(List<ScriptToken> tokens)
    {
        int j = tokens.Count - 1;
        while (j >= 0)
        {
            var token = tokens[j];
            int start;
            if (token.Is(")") || token.Is("]"))
            {
                start = FindOpen(tokens, j);
                if (start < 0)
                {
                    return -1;
                }

                // A group after a callee is a call or an index; otherwise it is a parenthesized expression.
                if (start > 0 && IsCallee(tokens[start - 1]))
                {
                    j = start - 1;
                    continue;
                }
            }
            else if (token.Kind == ScriptTokenKind.String || (token.Kind == ScriptTokenKind.Identifier && !Keywords.Contains(token.Text)))
            {
                start = j;
            }
            else
            {
                return -1;
            }

            if (start > 0 && (tokens[start - 1].Is(".") || tokens[start - 1].Is("?.")))
            {
                j = start - 2;
                continue;
            }

            return start > 0 && tokens[start - 1].Is("new") ? start - 1 : start;
        }
        return -1;
    }

    private static bool IsCallee(ScriptToken token) => token.Is(")") || token.Is("]") ||
        (token.Kind == ScriptTokenKind.Identifier && !Keywords.Contains(token.Text));

    private static int FindOpen(List<ScriptToken> tokens, int close)
    {
        int depth = 0;
        for (int i = close; i >= 0; i--)
        {
            if (tokens[i].Is(")") || tokens[i].Is("]") || tokens[i].Is("}"))
            {
                depth++;
            }
            else if ((tokens[i].Is("(") || tokens[i].Is("[") || tokens[i].Is("{")) && --depth == 0)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Maps a reflected type to what a script sees: nullable values unwrap, and void or object are unknown.
    /// </summary>
    private static ScriptValueType? Instance(Type? type)
    {
        type = type == null ? null : Nullable.GetUnderlyingType(type) ?? type;
        return type == null || type == typeof(void) || type == typeof(object) ? null : new ScriptValueType(type, false);
    }

    private static Type? Awaited(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() is var definition &&
            (definition == typeof(Task<>) || definition == typeof(ValueTask<>)))
        {
            return type.GetGenericArguments()[0];
        }

        return typeof(Task).IsAssignableFrom(type) || type == typeof(ValueTask) ? null : type;
    }

    /// <summary>
    /// Gets the item type of an indexer (the default member, such as Item or Chars) or an array.
    /// </summary>
    private static Type? IndexedType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        string name = type.GetCustomAttribute<DefaultMemberAttribute>()?.MemberName ?? "Item";
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name == name && p.GetIndexParameters().Length == 1)?.PropertyType;
    }

    /// <summary>
    /// Gets the type for...of yields: the IEnumerable&lt;T&gt; item type, or an array's element type.
    /// </summary>
    private static Type? EnumeratedType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    private static bool Accepts(MethodInfo method, int argumentCount)
    {
        var parameters = method.GetParameters();
        bool hasParamsArray = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
        int required = parameters.Count(p => !p.IsOptional && !p.IsDefined(typeof(ParamArrayAttribute)));
        return argumentCount >= required && (hasParamsArray || argumentCount <= parameters.Length);
    }

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetType(fullName) is { } type)
            {
                return type;
            }
        }
        return null;
    }

    private sealed class Analyzer(ScriptEnvironment environment, List<ScriptToken> tokens)
    {
        private readonly List<Dictionary<string, ScriptValueType?>> _scopes = [new(StringComparer.Ordinal)];

        /// <summary>
        /// Declarator names already recorded by <see cref="Declare"/>, so they are not re-read as assignments.
        /// </summary>
        private readonly HashSet<int> _declared = [];

        /// <summary>
        /// Records every declaration and assignment in scope at the end of the tokens. Initializers are
        /// resolved in place and the scan continues token by token, so declarations inside an unclosed
        /// callback around the caret are still seen.
        /// </summary>
        public void Run()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Is("{"))
                {
                    _scopes.Add(new Dictionary<string, ScriptValueType?>(StringComparer.Ordinal));
                    DeclareParameters(i);
                }
                else if (token.Is("}"))
                {
                    if (_scopes.Count > 1)
                    {
                        _scopes.RemoveAt(_scopes.Count - 1);
                    }
                }
                else if (token.Kind != ScriptTokenKind.Identifier || IsMemberName(i) || _declared.Contains(i))
                {
                    continue;
                }
                else if (token.Text is "var" or "let" or "const")
                {
                    Declare(i + 1, token.Text == "var");
                }
                else if (!Keywords.Contains(token.Text) && At(i + 1).Is("="))
                {
                    int index = i + 2;
                    Assign(token.Text, ResolveInitializer(ref index));
                }
            }
        }

        public IReadOnlyDictionary<string, ScriptValueType?> GetVariables()
        {
            var variables = new Dictionary<string, ScriptValueType?>(StringComparer.Ordinal);
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                foreach (var (name, value) in _scopes[i])
                {
                    variables.TryAdd(name, value);
                }
            }
            return variables;
        }

        /// <summary>
        /// Resolves a primary expression and its member, call and index chain, advancing past it. Returns
        /// null for anything it cannot type; the index then may stop short of the expression's end.
        /// </summary>
        public ScriptValueType? ResolveExpression(ref int i)
        {
            bool isAwait = At(i).Is("await");
            if (isAwait)
            {
                i++;
            }

            var token = At(i);
            ScriptValueType? value;
            if (token.Is("new"))
            {
                i++;
                if (At(i).Kind != ScriptTokenKind.Identifier)
                {
                    return null;
                }

                string name = tokens[i++].Text;
                bool isQualified = false;
                while (At(i).Is(".") && At(i + 1).Kind == ScriptTokenKind.Identifier)
                {
                    name += "." + tokens[i + 1].Text;
                    isQualified = true;
                    i += 2;
                }

                var type = isQualified ? FindType(name)
                    : environment.Registrations.TryGetValue(name, out var registration) && registration.IsType ? registration.Type : null;
                value = type == null ? null : new ScriptValueType(type, false);
                if (At(i).Is("(") && !SkipGroup(ref i))
                {
                    return null;
                }
            }
            else if (token.Kind == ScriptTokenKind.String)
            {
                value = new ScriptValueType(typeof(string), false);
                i++;
            }
            else if (token.Kind == ScriptTokenKind.Identifier && !Keywords.Contains(token.Text))
            {
                value = Lookup(token.Text);
                i++;
            }
            else if (token.Is("("))
            {
                int close = FindClose(i);
                if (close < 0)
                {
                    return null;
                }

                int inner = i + 1;
                value = ResolveExpression(ref inner);
                value = inner == close ? value : null;
                i = close + 1;
            }
            else
            {
                return null;
            }

            while (i < tokens.Count)
            {
                if ((At(i).Is(".") || At(i).Is("?.")) && At(i + 1).Kind == ScriptTokenKind.Identifier)
                {
                    string member = tokens[i + 1].Text;
                    i += 2;
                    if (At(i).Is("("))
                    {
                        int open = i;
                        if (!SkipGroup(ref i))
                        {
                            return null;
                        }

                        value = value is { } target ? ResolveMethod(target, member, CountArguments(open, i - 1)) : null;
                    }
                    else
                    {
                        value = value is { } target ? ResolveProperty(target, member) : null;
                    }
                }
                else if (At(i).Is("["))
                {
                    if (!SkipGroup(ref i))
                    {
                        return null;
                    }

                    value = value is { IsStatic: false } target ? Instance(IndexedType(target.Type)) : null;
                }
                else if (At(i).Is("("))
                {
                    // Calling a value, such as a delegate or a JavaScript function, has no known result.
                    if (!SkipGroup(ref i))
                    {
                        return null;
                    }

                    value = null;
                }
                else
                {
                    break;
                }
            }

            if (isAwait && value is { } awaited)
            {
                value = Instance(Awaited(awaited.Type));
            }

            return value;
        }

        private void Declare(int i, bool isVar)
        {
            var scope = isVar ? _scopes[0] : _scopes[^1];
            while (At(i).Kind == ScriptTokenKind.Identifier)
            {
                string name = tokens[i].Text;
                _declared.Add(i);
                ScriptValueType? value = null;
                i++;
                if (At(i).Is("="))
                {
                    i++;
                    value = ResolveInitializer(ref i);
                }
                else if (At(i).Is("of"))
                {
                    i++;
                    value = ResolveExpression(ref i) is { IsStatic: false } source ? Instance(EnumeratedType(source.Type)) : null;
                }

                scope[name] = value;
                if (!At(i).Is(","))
                {
                    return;
                }

                i++;
            }
        }

        /// <summary>
        /// Resolves the right side of = and discards it unless the expression ends there, so a + b is unknown.
        /// </summary>
        private ScriptValueType? ResolveInitializer(ref int i)
        {
            var value = ResolveExpression(ref i);
            var next = At(i);
            bool ends = i >= tokens.Count || next.Is(";") || next.Is(",") || next.Is(")") || next.Is("]") || next.Is("}") ||
                (next.NewLineBefore && next.Kind != ScriptTokenKind.Punctuator);
            return ends ? value : null;
        }

        private void Assign(string name, ScriptValueType? value)
        {
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].ContainsKey(name))
                {
                    _scopes[i][name] = value;
                    return;
                }
            }
            // An undeclared assignment creates a global.
            _scopes[0][name] = value;
        }

        /// <summary>
        /// Declares the parameters of a function, arrow function or catch clause whose body opens at the brace,
        /// as unknown, so they shadow outer variables and registered aliases.
        /// </summary>
        private void DeclareParameters(int brace)
        {
            var scope = _scopes[^1];
            var previous = At(brace - 1);
            if (previous.Is("=>"))
            {
                if (At(brace - 2).Kind == ScriptTokenKind.Identifier)
                {
                    scope[tokens[brace - 2].Text] = null;
                    return;
                }

                if (!At(brace - 2).Is(")"))
                {
                    return;
                }
            }
            else if (!previous.Is(")"))
            {
                return;
            }

            int close = previous.Is("=>") ? brace - 2 : brace - 1;
            int open = FindOpen(tokens, close);
            if (open < 0)
            {
                return;
            }

            bool isFunction = previous.Is("=>") || At(open - 1).Is("catch") || At(open - 1).Is("function") || At(open - 2).Is("function");
            if (!isFunction)
            {
                return;
            }

            int depth = 0;
            for (int i = open + 1; i < close; i++)
            {
                var token = tokens[i];
                if (token.Is("(") || token.Is("[") || token.Is("{"))
                {
                    depth++;
                }
                else if (token.Is(")") || token.Is("]") || token.Is("}"))
                {
                    depth--;
                }
                else if (depth == 0 && token.Kind == ScriptTokenKind.Identifier &&
                    (tokens[i - 1].Is("(") || tokens[i - 1].Is(",") || tokens[i - 1].Is("...")))
                {
                    scope[token.Text] = null;
                }
            }
        }

        private ScriptValueType? Lookup(string name)
        {
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].TryGetValue(name, out var value))
                {
                    return value;
                }
            }
            return environment.Registrations.TryGetValue(name, out var registration) ? ScriptValueType.From(registration) : null;
        }

        private static ScriptValueType? ResolveMethod(ScriptValueType target, string name, int argumentCount)
        {
            var methods = target.Type.GetMethods(ScriptCompletion.MemberFlags(target))
                .Where(m => m.Name == name && !m.IsSpecialName && ScriptCompletion.Visible(m))
                .OrderBy(m => m.GetParameters().Length).ToArray();
            var method = methods.FirstOrDefault(m => Accepts(m, argumentCount)) ?? methods.FirstOrDefault();
            return method == null ? null
                : Instance(method.GetCustomAttribute<ScriptModuleMethodAttribute>()?.ReturnType ?? method.ReturnType);
        }

        private static ScriptValueType? ResolveProperty(ScriptValueType target, string name)
        {
            var flags = ScriptCompletion.MemberFlags(target);
            // A registered dictionary, such as globals, exposes its keys; their current values have a known type.
            if (target.Registration?.Instance is IEnumerable<KeyValuePair<string, object>> dictionary &&
                dictionary.FirstOrDefault(p => p.Key == name) is { Key: not null } pair)
            {
                return Instance(pair.Value?.GetType());
            }

            var property = target.Type.GetProperties(flags).FirstOrDefault(p => p.Name == name && p.GetIndexParameters().Length == 0 && ScriptCompletion.Visible(p));
            if (property != null)
            {
                return Instance(property.GetCustomAttribute<ScriptModuleMethodAttribute>()?.ReturnType ?? property.PropertyType);
            }

            var field = target.Type.GetFields(flags).FirstOrDefault(f => f.Name == name && ScriptCompletion.Visible(f));
            return field == null ? null : Instance(field.GetCustomAttribute<ScriptModuleMethodAttribute>()?.ReturnType ?? field.FieldType);
        }

        private int CountArguments(int open, int close)
        {
            if (close == open + 1)
            {
                return 0;
            }

            int count = 1, depth = 0;
            for (int i = open + 1; i < close; i++)
            {
                if (tokens[i].Is("(") || tokens[i].Is("[") || tokens[i].Is("{"))
                {
                    depth++;
                }
                else if (tokens[i].Is(")") || tokens[i].Is("]") || tokens[i].Is("}"))
                {
                    depth--;
                }
                else if (depth == 0 && tokens[i].Is(","))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Moves past a balanced group starting at an opening bracket; false when it does not close.
        /// </summary>
        private bool SkipGroup(ref int i)
        {
            int close = FindClose(i);
            if (close < 0)
            {
                return false;
            }

            i = close + 1;
            return true;
        }

        private int FindClose(int open)
        {
            int depth = 0;
            for (int i = open; i < tokens.Count; i++)
            {
                if (tokens[i].Is("(") || tokens[i].Is("[") || tokens[i].Is("{"))
                {
                    depth++;
                }
                else if ((tokens[i].Is(")") || tokens[i].Is("]") || tokens[i].Is("}")) && --depth == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool IsMemberName(int i) => At(i - 1).Is(".") || At(i - 1).Is("?.");

        private ScriptToken At(int i) => i >= 0 && i < tokens.Count ? tokens[i] : default;
    }
}

internal enum ScriptTokenKind { None, Identifier, String, Number, Regex, Punctuator }

/// <summary>
/// A JavaScript token; comments and whitespace are dropped.
/// </summary>
/// <param name="Kind">The token category; strings include template literals.</param>
/// <param name="Text">The token text.</param>
/// <param name="Offset">The offset of the first character.</param>
/// <param name="NewLineBefore">Whether a line break separates it from the previous token.</param>
internal readonly record struct ScriptToken(ScriptTokenKind Kind, string Text, int Offset, bool NewLineBefore)
{
    /// <summary>
    /// Whether this is the given identifier, keyword or punctuator.
    /// </summary>
    public bool Is(string text) => Kind is ScriptTokenKind.Identifier or ScriptTokenKind.Punctuator && Text == text;
}

/// <summary>
/// A tolerant JavaScript tokenizer: unterminated strings, comments and templates end at the limit instead of throwing.
/// </summary>
internal static class ScriptTokenizer
{
    /// <summary>
    /// Multi character punctuators, longest first so the longest match wins.
    /// </summary>
    private static readonly string[] Punctuators =
    [
        ">>>=", "...", "===", "!==", "**=", "<<=", ">>=", ">>>", "&&=", "||=", "??=", "=>", "==", "!=", "<=", ">=",
        "&&", "||", "??", "?.", "++", "--", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "**", "<<", ">>"
    ];

    /// <summary>
    /// Tokenizes text[0..end].
    /// </summary>
    /// <param name="text">The source.</param>
    /// <param name="end">The offset to stop at, usually the caret.</param>
    /// <param name="endsInTrivia">Set when end falls inside a comment, string, template or regular expression.</param>
    public static List<ScriptToken> Tokenize(string text, int end, out bool endsInTrivia)
    {
        var tokens = new List<ScriptToken>();
        bool newLine = false;
        endsInTrivia = false;
        int i = 0;
        while (i < end)
        {
            char c = text[i];
            if (c == '\n')
            {
                newLine = true;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && At(i + 1) == '/')
            {
                int lineEnd = text.IndexOf('\n', i, end - i);
                endsInTrivia = lineEnd < 0;
                i = lineEnd < 0 ? end : lineEnd;
                continue;
            }

            if (c == '/' && At(i + 1) == '*')
            {
                int close = i + 2 < end ? text.IndexOf("*/", i + 2, end - i - 2, StringComparison.Ordinal) : -1;
                endsInTrivia = close < 0;
                int stop = close < 0 ? end : close + 2;
                newLine |= text.IndexOf('\n', i, stop - i) >= 0;
                i = stop;
                continue;
            }

            int start = i;
            ScriptTokenKind kind;
            bool terminated = true;
            if (c is '\'' or '"')
            {
                kind = ScriptTokenKind.String;
                i = SkipQuoted(i, out terminated);
            }
            else if (c == '`')
            {
                kind = ScriptTokenKind.String;
                i = SkipTemplate(i, out terminated);
            }
            else if (char.IsLetter(c) || c is '_' or '$')
            {
                kind = ScriptTokenKind.Identifier;
                while (i < end && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '$'))
                {
                    i++;
                }
            }
            else if (char.IsDigit(c) || (c == '.' && char.IsDigit(At(i + 1))))
            {
                kind = ScriptTokenKind.Number;
                i++;
                while (i < end && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || (text[i] == '.' && char.IsDigit(At(i + 1)))))
                {
                    i++;
                }
            }
            else if (c == '/' && IsRegexAllowed(tokens))
            {
                kind = ScriptTokenKind.Regex;
                i = SkipRegex(i, out terminated);
            }
            else
            {
                kind = ScriptTokenKind.Punctuator;
                string? match = Punctuators.FirstOrDefault(p => i + p.Length <= end && string.CompareOrdinal(text, i, p, 0, p.Length) == 0);
                i += match?.Length ?? 1;
            }

            endsInTrivia = !terminated;
            tokens.Add(new ScriptToken(kind, text[start..i], start, newLine));
            newLine = false;
        }
        return tokens;

        char At(int index) => index < end ? text[index] : '\0';

        int SkipQuoted(int index, out bool closed)
        {
            char quote = text[index++];
            while (index < end)
            {
                char c = text[index++];
                if (c == '\\')
                {
                    index++;
                }
                else if (c == quote)
                {
                    closed = true;
                    return index;
                }
                else if (c == '\n')
                {
                    // An unterminated string ends at the line break, as the engine would report it.
                    closed = true;
                    return index - 1;
                }
            }
            closed = false;
            return end;
        }

        int SkipTemplate(int index, out bool closed)
        {
            index++;
            while (index < end)
            {
                char c = text[index++];
                if (c == '\\')
                {
                    index++;
                }
                else if (c == '`')
                {
                    closed = true;
                    return Math.Min(index, end);
                }
                else if (c == '$' && At(index) == '{')
                {
                    index++;
                    for (int depth = 1; index < end && depth > 0;)
                    {
                        char e = text[index];
                        if (e is '\'' or '"')
                        {
                            index = SkipQuoted(index, out _);
                        }
                        else if (e == '`')
                        {
                            index = SkipTemplate(index, out _);
                        }
                        else
                        {
                            depth += e == '{' ? 1 : e == '}' ? -1 : 0;
                            index++;
                        }
                    }
                }
            }
            closed = false;
            return end;
        }

        int SkipRegex(int index, out bool closed)
        {
            bool inClass = false;
            index++;
            while (index < end)
            {
                char c = text[index++];
                if (c == '\\')
                {
                    index++;
                }
                else if (c == '[')
                {
                    inClass = true;
                }
                else if (c == ']')
                {
                    inClass = false;
                }
                else if (c == '\n')
                {
                    closed = true;
                    return index - 1;
                }
                else if (c == '/' && !inClass)
                {
                    while (index < end && char.IsLetter(text[index]))
                    {
                        index++;
                    }

                    closed = true;
                    return Math.Min(index, end);
                }
            }
            closed = false;
            return end;
        }
    }

    /// <summary>
    /// A slash starts a regular expression where an operand is expected, rather than dividing.
    /// </summary>
    private static bool IsRegexAllowed(List<ScriptToken> tokens)
    {
        if (tokens.Count == 0)
        {
            return true;
        }

        var previous = tokens[^1];
        return previous.Kind switch
        {
            ScriptTokenKind.Punctuator => previous.Text is not (")" or "]" or "}" or "++" or "--"),
            ScriptTokenKind.Identifier => previous.Text is "return" or "typeof" or "case" or "do" or "else" or "in" or "of"
                or "new" or "delete" or "void" or "throw" or "yield" or "await",
            _ => false
        };
    }
}
