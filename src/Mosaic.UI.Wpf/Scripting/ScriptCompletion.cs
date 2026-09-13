/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Mosaic.UI.Wpf.Scripting;

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
    public static IReadOnlyList<ICompletionData> GetMembers(ScriptEnvironment environment, string alias)
    {
        if (!environment.Registrations.TryGetValue(alias, out var registration)) return [];
        var result = new List<ICompletionData>();
        var flags = MemberFlags(registration);
        if (registration.Instance is IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            foreach (var pair in dictionary.OrderBy(p => p.Key, StringComparer.Ordinal))
                result.Add(new ScriptCompletionData(pair.Key, ScriptCompletionKind.Global,
                    new ScriptCompletionDescription("Global", pair.Value?.GetType().Name ?? "null", $"Current value: {pair.Value}")));
        }
        foreach (var group in registration.Type.GetMethods(flags).Where(m => !m.IsSpecialName && Visible(m)).GroupBy(m => m.Name))
        {
            var methods = group.ToArray();
            string returnType = string.Join(" | ", methods.Select(ReturnTypeName).Distinct(StringComparer.Ordinal));
            string summary = methods.Select(Description).FirstOrDefault(d => d.Length > 0) ?? string.Empty;
            result.Add(new ScriptCompletionData(group.Key, ScriptCompletionKind.Method,
                new ScriptCompletionDescription("Method", returnType, summary, string.Join("\n", methods.Select(Signature))))
            { IsMethod = true, HasParameters = methods.Any(m => m.GetParameters().Length > 0) });
        }
        foreach (var property in registration.Type.GetProperties(flags).Where(p => Visible(p) && p.GetIndexParameters().Length == 0))
        {
            string type = TypeName(property.PropertyType);
            string access = property.SetMethod?.IsPublic == true ? "Gets or sets" : "Gets";
            result.Add(new ScriptCompletionData(property.Name, ScriptCompletionKind.Property,
                new ScriptCompletionDescription("Property", type, Description(property), $"{access} {type} {property.Name}")));
        }
        foreach (var field in registration.Type.GetFields(flags).Where(Visible))
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
    internal static IReadOnlyList<ScriptSignature> GetSignatures(ScriptEnvironment environment, ScriptCallContext call)
    {
        IEnumerable<ScriptSignature> signatures;
        if (call.IsConstructor)
        {
            if (!environment.Registrations.TryGetValue(call.Name, out var type) || !type.IsType || type.Type.IsAbstract) return [];
            signatures = type.Type.GetConstructors().Where(Visible).Select(c => CreateSignature(c, string.Empty, call.Key));
        }
        else
        {
            if (call.Qualifier == null || !environment.Registrations.TryGetValue(call.Qualifier, out var registration)) return [];
            signatures = registration.Type.GetMethods(MemberFlags(registration))
                .Where(m => m.Name == call.Name && !m.IsSpecialName && Visible(m))
                .Select(m => CreateSignature(m, ReturnTypeName(m), call.Key));
        }
        return signatures.OrderBy(s => s.Parameters.Count).ThenBy(s => s.HasParamsArray).ToArray();
    }

    internal static bool Visible(MemberInfo member) => member.DeclaringType != typeof(object) &&
        member.GetCustomAttribute<ScriptHiddenAttribute>() == null;

    private static BindingFlags MemberFlags(ScriptRegistration registration) =>
        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | (registration.IsType ? 0 : BindingFlags.Instance);

    private static ScriptSignature CreateSignature(MethodBase method, string returnType, string name)
    {
        var parameters = method.GetParameters();
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
        if (hint == null || open < 0 || close < open) return null;
        var parts = new List<string>();
        int depth = 0, start = open + 1;
        for (int i = start; i < close; i++)
        {
            if (hint[i] is '(' or '[' or '<' or '{') depth++;
            else if (hint[i] is ')' or ']' or '>' or '}') depth--;
            else if (hint[i] == ',' && depth == 0) { parts.Add(hint[start..i]); start = i + 1; }
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

    private static string Signature(MethodInfo method) => method.GetCustomAttribute<ScriptModuleMethodAttribute>()?.AutoCompleteHint
        ?? $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{TypeName(p.ParameterType)} {p.Name}"))})";

    private static string TypeName(Type type) => type.IsGenericType
        ? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>" : type.Name;
}

/// <summary>
/// The category of a completion entry, which selects its icon and icon color.
/// </summary>
internal enum ScriptCompletionKind { Module, Class, Method, Property, Field, Global, Snippet }

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
        if (addParentheses) value += "()";
        textArea.Document.Replace(completionSegment, value);
        textArea.Caret.Offset = completionSegment.Offset + value.Length - (addParentheses && (HasParameters || typedOpening) ? 1 : 0);
        if (typedOpening && addParentheses && insertionRequestEventArgs is TextCompositionEventArgs input) input.Handled = true;
    }
}
