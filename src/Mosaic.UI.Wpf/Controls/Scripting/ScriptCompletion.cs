/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Mosaic.UI.Wpf.Controls.Scripting;

/// <summary>
/// Builds reflection-based completion entries for a scripting environment.
/// </summary>
public static class ScriptCompletion
{
    /// <summary>
    /// Gets registered modules, or constructible type names after new.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="typesOnly">Whether to include only constructible types.</param>
    public static IReadOnlyList<ICompletionData> GetModules(ScriptEnvironment environment, bool typesOnly = false) =>
        environment.Registrations.Values
            .Where(r => !typesOnly || (r.IsType && !r.Type.IsAbstract && r.Type.GetConstructors().Length > 0))
            .OrderBy(r => r.Alias, StringComparer.Ordinal)
            .Select(r => (ICompletionData)new ScriptCompletionData(r.Alias, r.IsType ? ScriptCompletionKind.Class : ScriptCompletionKind.Module,
                new ScriptCompletionDescription(r.Alias, r.IsType ? "class" : "module",
                    r.Type.GetCustomAttribute<ScriptModuleAttribute>()?.Description ?? r.Type.FullName ?? r.Type.Name))).ToArray();

    /// <summary>
    /// Gets methods, overload signatures, properties, fields and live dictionary keys for an alias.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="alias">The member access qualifier.</param>
    public static IReadOnlyList<ICompletionData> GetMembers(ScriptEnvironment environment, string alias) =>
        environment.TryGetRegistration(alias, out var registration) ? GetMembers(ScriptValueType.From(registration), environment.ExtensionMethods) : [];

    /// <summary>
    /// Gets methods, overload signatures, properties, fields and live dictionary keys for a registered or inferred value.
    /// </summary>
    /// <param name="value">The member access target.</param>
    /// <param name="extensions">The extension methods in scope; those that apply to an inferred instance are listed with
    /// its methods. A registered alias lists only its own members, so a module is not crowded by extensions on object.</param>
    internal static IReadOnlyList<ICompletionData> GetMembers(ScriptValueType value, IReadOnlyList<MethodInfo>? extensions = null)
    {
        var result = new List<ICompletionData>();
        var flags = MemberFlags(value);
        MethodInfo[] extensionMethods = value.IsStatic || value.Registration != null || extensions == null ? []
            : GetExtensionMethods(value.Type, extensions).Where(Visible).ToArray();
        var isExtension = new HashSet<MethodInfo>(extensionMethods);
        if (value.Registration?.Instance is IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            foreach (var pair in dictionary.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                result.Add(new ScriptCompletionData(pair.Key, ScriptCompletionKind.Global,
                    new ScriptCompletionDescription("Global", pair.Value?.GetType().Name ?? "null", $"Current value: {pair.Value}")));
            }
        }
        // The engine calls instance methods and extension methods of the same name as one overload set.
        foreach (var group in value.Type.GetMethods(flags).Where(m => !m.IsSpecialName && Visible(m)).Concat(extensionMethods).GroupBy(m => m.Name))
        {
            var methods = group.ToArray();
            bool extensionsOnly = methods.All(isExtension.Contains);
            string returnType = string.Join(" | ", methods.Select(ReturnTypeName).Distinct(StringComparer.Ordinal));
            string summary = methods.Select(Description).FirstOrDefault(d => d.Length > 0) ?? string.Empty;
            result.Add(new ScriptCompletionData(group.Key, extensionsOnly ? ScriptCompletionKind.ExtensionMethod : ScriptCompletionKind.Method,
                new ScriptCompletionDescription(extensionsOnly ? "Extension Method" : "Method", returnType, summary,
                    string.Join("\n", methods.Select(m => Signature(m, isExtension.Contains(m))))))
            { IsMethod = true, HasParameters = methods.Any(m => m.GetParameters().Length > (isExtension.Contains(m) ? 1 : 0)) });
        }
        foreach (var property in value.Type.GetProperties(flags).Where(p => Visible(p) && p.GetIndexParameters().Length == 0))
        {
            string type = TypeName(property.PropertyType);
            string access = property.SetMethod?.IsPublic == true ? "Gets or sets" : "Gets";
            result.Add(new ScriptCompletionData(property.Name, ScriptCompletionKind.Property,
                new ScriptCompletionDescription("Property", type, Description(property), $"{access} {type} {property.Name}")));
        }
        foreach (var field in value.Type.GetFields(flags).Where(Visible))
        {
            string type = TypeName(field.FieldType);
            string access = field.IsInitOnly || field.IsLiteral ? "Gets" : "Gets or sets";
            result.Add(new ScriptCompletionData(field.Name, ScriptCompletionKind.Field,
                new ScriptCompletionDescription("Field", type, Description(field), $"{access} {type} {field.Name}")));
        }
        return result.OrderBy(r => r.Text, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Gets the loop and multiline string snippets ported from ApexGate.
    /// </summary>
    public static IReadOnlyList<ICompletionData> GetSnippets() => [
        Snippet("For Loop", "A snippet to show how to do a basic for loop.", "for (let i = 1; i <= 5; i++) {\n    \n}"),
        Snippet("For Loop Of", "A snippet to show how to iterate over a collection named items.", "for (const item of items) {\n    \n}"),
        Snippet("Multiline String", "A snippet that creates a string variable with a multiline string syntax.", "let buf = `\n\n`;"),
        Snippet("While Loop", "A snippet to show how to do a while loop with pausing.", "let count = 1;\nwhile (count <= 5) {\n    log.Info(count.ToString());\n    await ui.PauseAsync(1000);\n    count++;\n}")
    ];

    /// <summary>
    /// Gets the overloads of a member call (alias.Method) or a constructor (new Type), ordered by parameter count.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="call">The call surrounding the caret.</param>
    internal static IReadOnlyList<ScriptSignature> GetSignatures(ScriptEnvironment environment, ScriptCallContext call) =>
        GetSignatures(environment, call, call.Qualifier != null && environment.TryGetRegistration(call.Qualifier, out var registration)
            ? ScriptValueType.From(registration) : null);

    /// <summary>
    /// Gets the overloads of a member call on a registered or inferred target, or of a constructor, ordered by parameter count.
    /// </summary>
    /// <param name="environment">The registration source.</param>
    /// <param name="call">The call surrounding the caret.</param>
    /// <param name="target">The value the method is called on; ignored for a constructor.</param>
    internal static IReadOnlyList<ScriptSignature> GetSignatures(ScriptEnvironment environment, ScriptCallContext call, ScriptValueType? target)
    {
        IEnumerable<ScriptSignature> signatures;
        if (call.IsConstructor)
        {
            if (!environment.TryGetRegistration(call.Name, out var type) || !type.IsType || type.Type.IsAbstract)
            {
                return [];
            }

            signatures = type.Type.GetConstructors().Where(Visible).Select(c => CreateSignature(c, string.Empty, call.Key));
        }
        else
        {
            if (call.Qualifier == null || target is not { } value)
            {
                return [];
            }

            var extensions = value.IsStatic ? [] : GetExtensionMethods(value.Type, environment.ExtensionMethods)
                .Where(m => m.Name == call.Name && Visible(m))
                .Select(m => CreateSignature(m, ReturnTypeName(m), call.Key, true));
            signatures = value.Type.GetMethods(MemberFlags(value))
                .Where(m => m.Name == call.Name && !m.IsSpecialName && Visible(m))
                .Select(m => CreateSignature(m, ReturnTypeName(m), call.Key))
                .Concat(extensions);
        }
        return signatures.OrderBy(s => s.Parameters.Count).ThenBy(s => s.HasParamsArray).ToArray();
    }

    /// <summary>
    /// Gets the extension methods that can be called on an instance of a type. A generic method is closed over
    /// the type arguments its receiver determines, eg: Where on List&lt;string&gt; takes Func&lt;string, bool&gt;;
    /// when the receiver does not determine all of them it is returned open.
    /// </summary>
    /// <param name="receiver">The type of the value the method is called on.</param>
    /// <param name="extensions">The extension methods in scope.</param>
    internal static IEnumerable<MethodInfo> GetExtensionMethods(Type receiver, IReadOnlyList<MethodInfo> extensions)
    {
        foreach (var method in extensions)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                continue;
            }

            if (!method.IsGenericMethodDefinition)
            {
                if (parameters[0].ParameterType.IsAssignableFrom(receiver))
                {
                    yield return method;
                }

                continue;
            }

            var bindings = new Dictionary<Type, Type>();
            if (!Unify(parameters[0].ParameterType, receiver, bindings))
            {
                continue;
            }

            var arguments = method.GetGenericArguments();
            if (!arguments.All(bindings.ContainsKey))
            {
                yield return method;
                continue;
            }

            MethodInfo? closed;
            try
            {
                closed = method.MakeGenericMethod(arguments.Select(a => bindings[a]).ToArray());
            }
            catch (ArgumentException)
            {
                // The receiver violates a constraint, eg: where T : struct.
                closed = null;
            }

            if (closed != null)
            {
                yield return closed;
            }
        }
    }

    /// <summary>
    /// Matches a parameter type that may contain generic parameters against an argument type, recording the bindings.
    /// </summary>
    private static bool Unify(Type parameter, Type argument, Dictionary<Type, Type> bindings)
    {
        if (parameter.IsGenericParameter)
        {
            if (bindings.TryGetValue(parameter, out var bound))
            {
                return bound == argument;
            }

            bindings[parameter] = argument;
            return true;
        }

        if (!parameter.ContainsGenericParameters)
        {
            return parameter.IsAssignableFrom(argument);
        }

        if (parameter.IsArray)
        {
            return argument.IsArray && parameter.GetArrayRank() == argument.GetArrayRank() &&
                Unify(parameter.GetElementType()!, argument.GetElementType()!, bindings);
        }

        if (!parameter.IsGenericType)
        {
            return false;
        }

        // The argument, a base type or an implemented interface built from the same generic definition, eg: IEnumerable<T>.
        var definition = parameter.GetGenericTypeDefinition();
        var candidates = new List<Type>();
        for (var type = argument; type != null; type = type.BaseType)
        {
            candidates.Add(type);
        }

        candidates.AddRange(argument.GetInterfaces());
        foreach (var candidate in candidates.Where(c => c.IsGenericType && c.GetGenericTypeDefinition() == definition))
        {
            var attempt = new Dictionary<Type, Type>(bindings);
            var expected = parameter.GetGenericArguments();
            var actual = candidate.GetGenericArguments();
            bool matched = true;
            for (int i = 0; i < expected.Length && matched; i++)
            {
                matched = expected[i].ContainsGenericParameters ? Unify(expected[i], actual[i], attempt) : expected[i] == actual[i];
            }

            if (matched)
            {
                foreach (var (key, value) in attempt)
                {
                    bindings[key] = value;
                }

                return true;
            }
        }
        return false;
    }

    internal static bool Visible(MemberInfo member) => member.DeclaringType != typeof(object) &&
        member.GetCustomAttribute<ScriptHiddenAttribute>() == null;

    /// <summary>
    /// A type alias exposes static members; a registered object also lists its type's statics, an inferred instance does not.
    /// </summary>
    internal static BindingFlags MemberFlags(ScriptValueType value) => BindingFlags.Public | BindingFlags.FlattenHierarchy |
        (value.IsStatic ? BindingFlags.Static : BindingFlags.Instance | (value.Registration != null ? BindingFlags.Static : 0));

    private static ScriptSignature CreateSignature(MethodBase method, string returnType, string name, bool isExtension = false)
    {
        // An extension method's first parameter is the value it is called on.
        var parameters = method.GetParameters().Skip(isExtension ? 1 : 0).ToArray();
        bool hasParamsArray = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
        var items = ParseHint(method.GetCustomAttribute<ScriptModuleMethodAttribute>()?.AutoCompleteHint)
            ?? parameters.Select(p => new ScriptSignatureParameter(TypeName(p.ParameterType), p.Name ?? string.Empty,
                p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty, DefaultValue(p), p.IsDefined(typeof(ParamArrayAttribute)))).ToArray();
        return new ScriptSignature(returnType, name, items, Description(method), hasParamsArray);
    }

    /// <summary>
    /// Splits an AutoCompleteHint such as Name(string one, two) into display parameters; null when it has no argument list.
    /// </summary>
    private static ScriptSignatureParameter[]? ParseHint(string? hint)
    {
        int open = hint?.IndexOf('(') ?? -1, close = hint?.LastIndexOf(')') ?? -1;
        if (hint == null || open < 0 || close < open)
        {
            return null;
        }

        var parts = new List<string>();
        int depth = 0, start = open + 1;
        for (int i = start; i < close; i++)
        {
            if (hint[i] is '(' or '[' or '<' or '{')
            {
                depth++;
            }
            else if (hint[i] is ')' or ']' or '>' or '}')
            {
                depth--;
            }
            else if (hint[i] == ',' && depth == 0)
            {
                parts.Add(hint[start..i]);
                start = i + 1;
            }
        }
        parts.Add(hint[start..close]);
        return parts.Select(p => p.Trim()).Where(p => p.Length > 0).Select(p =>
        {
            int space = p.LastIndexOf(' ');
            return space < 0 ? new ScriptSignatureParameter(string.Empty, p) : new ScriptSignatureParameter(p[..space].Trim(), p[(space + 1)..]);
        }).ToArray();
    }

    private static string? DefaultValue(ParameterInfo parameter) => !parameter.HasDefaultValue ? null : parameter.DefaultValue switch
    {
        null => "null",
        string text => $"\"{text}\"",
        bool value => value ? "true" : "false",
        var value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
    };

    private static ScriptCompletionData Snippet(string name, string summary, string insertionText) =>
        new(name, ScriptCompletionKind.Snippet, new ScriptCompletionDescription(name, "snippet", summary)) { InsertionText = insertionText };

    private static string Description(MemberInfo member) => member.GetCustomAttribute<ScriptModuleMethodAttribute>()?.Description
        ?? member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

    private static string ReturnTypeName(MethodInfo method) =>
        method.GetCustomAttribute<ScriptModuleMethodAttribute>()?.ReturnTypeHint ?? TypeName(method.ReturnType);

    private static string Signature(MethodInfo method, bool isExtension) => method.GetCustomAttribute<ScriptModuleMethodAttribute>()?.AutoCompleteHint
        ?? $"{(isExtension ? "(extension) " : "")}{method.Name}({string.Join(", ", method.GetParameters().Skip(isExtension ? 1 : 0).Select(p => $"{TypeName(p.ParameterType)} {p.Name}"))})";

    private static string TypeName(Type type) => type.IsGenericType
        ? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>" : type.Name;
}

/// <summary>
/// The category of a completion entry, which selects its icon and icon color.
/// </summary>
internal enum ScriptCompletionKind { Module, Class, Method, ExtensionMethod, Property, Field, Global, Snippet }

/// <summary>
/// The metadata shown in the completion tool tip; rendered by ScriptCompletionDescriptionTemplate.
/// </summary>
/// <param name="MemberType">The heading, such as Method, Property or the module alias.</param>
/// <param name="ReturnType">The value type, highlighted beside the heading.</param>
/// <param name="Summary">The description; a placeholder is shown when empty.</param>
/// <param name="Hint">Signatures or accessor text; the section is hidden when empty.</param>
internal sealed record ScriptCompletionDescription(string MemberType, string ReturnType, string Summary, string Hint = "")
{
    public string Summary { get; } = string.IsNullOrWhiteSpace(Summary) ? "No Description Available" : Summary;

    public override string ToString() => string.Join("\n", new[] { $"{MemberType} {ReturnType}".Trim(), Summary, Hint }.Where(s => s.Length > 0));
}

internal sealed class ScriptCompletionData(string text, ScriptCompletionKind kind, ScriptCompletionDescription description) : ICompletionData
{
    public ImageSource? Image => null;
    public string Text => text;
    public ScriptCompletionKind Kind => kind;
    /// <summary>
    /// The right aligned list text; modules and classes are identified by their icon instead.
    /// </summary>
    public string Detail => kind is ScriptCompletionKind.Module or ScriptCompletionKind.Class ? string.Empty : description.ReturnType;
    public object Content => text;
    public object Description => description;
    public double Priority => 1;
    public bool IsMethod { get; init; }
    public bool HasParameters { get; init; }
    public string? InsertionText { get; init; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        string value = InsertionText ?? Text;
        bool typedOpening = insertionRequestEventArgs is TextCompositionEventArgs { Text: "(" };
        bool followingOpening = completionSegment.EndOffset < textArea.Document.TextLength &&
            textArea.Document.GetCharAt(completionSegment.EndOffset) == '(';
        bool addParentheses = IsMethod && !followingOpening;
        if (addParentheses)
        {
            value += "()";
        }
        // The segment is anchor based and its Offset moves to the end of the inserted text after Replace, so capture it first.
        int start = completionSegment.Offset;
        textArea.Document.Replace(completionSegment, value);
        textArea.Caret.Offset = start + value.Length - (addParentheses && (HasParameters || typedOpening) ? 1 : 0);
        if (typedOpening && addParentheses && insertionRequestEventArgs is TextCompositionEventArgs input)
        {
            input.Handled = true;
        }
    }
}
