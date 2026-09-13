/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.ScriptCommands;
using Tenray.Topaz;
using Tenray.Topaz.API;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// Owns the registrations shared by a Topaz engine and its editors.
/// </summary>
/// <remarks>
/// Register on the UI thread before executing scripts. A supplied engine is not reset or disposed.
/// Use RegisterCompletionType for values already installed in that engine. Editors sharing an engine
/// serialize execution, but direct calls to the engine remain the caller's responsibility.
/// </remarks>
public sealed class ScriptEnvironment
{
    private static readonly ConditionalWeakTable<TopazEngine, SemaphoreSlim> EngineLocks = new();
    private readonly Dictionary<string, ScriptRegistration> _registrations = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the ScriptEnvironment class.
    /// </summary>
    /// <param name="engine">An existing engine, or null to create a configured default.</param>
    /// <param name="includeDefaults">Whether to install default bridges into an existing engine.</param>
    public ScriptEnvironment(TopazEngine? engine = null, bool includeDefaults = false)
    {
        Engine = engine ?? new TopazEngine();
        Registrations = new ReadOnlyDictionary<string, ScriptRegistration>(_registrations);
        if (engine == null || includeDefaults)
            RegisterDefaults();
    }

    /// <summary>
    /// Gets the engine used to execute scripts.
    /// </summary>
    public TopazEngine Engine { get; }

    /// <summary>
    /// Gets the globals belonging to this environment; the default setup exposes them as globals.
    /// </summary>
    public ConcurrentDictionary<string, object> Globals { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the aliases available for completion and highlighting.
    /// </summary>
    public IReadOnlyDictionary<string, ScriptRegistration> Registrations { get; }

    /// <summary>
    /// Occurs when a registration is added or replaced.
    /// </summary>
    public event EventHandler? RegistrationsChanged;

    /// <summary>
    /// Registers a constructible or static .NET type in both the engine and editor.
    /// </summary>
    /// <param name="type">The type to expose.</param>
    /// <param name="alias">An optional script name, defaulting to ScriptModuleAttribute.Name or the type name.</param>
    public void RegisterType(Type type, string? alias = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        alias = ResolveAlias(type, alias);
        Engine.AddType(type, alias);
        RegisterCompletionType(alias, type, true);
    }

    /// <summary>
    /// Registers an object, including derived application command bridges, in the engine and editor.
    /// </summary>
    /// <param name="alias">The script name to add or replace.</param>
    /// <param name="instance">The object exposed under that name.</param>
    public void RegisterObject(string alias, object instance)
    {
        ValidateAlias(alias);
        ArgumentNullException.ThrowIfNull(instance);
        Engine.SetValue(alias, instance);
        RegisterCompletionType(alias, instance.GetType(), false, instance);
    }

    /// <summary>
    /// Registers an attributed command object using its module name.
    /// </summary>
    /// <param name="instance">The application command object.</param>
    public void RegisterModule(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        RegisterObject(ResolveAlias(instance.GetType(), null), instance);
    }

    /// <summary>
    /// Describes a value already present in a caller-supplied engine without replacing it.
    /// </summary>
    /// <param name="alias">The script name.</param>
    /// <param name="type">The exposed type.</param>
    /// <param name="isType">Whether the alias represents a .NET type rather than an instance.</param>
    /// <param name="instance">An optional instance for dictionary key completion.</param>
    public void RegisterCompletionType(string alias, Type type, bool isType = false, object? instance = null)
    {
        ValidateAlias(alias);
        ArgumentNullException.ThrowIfNull(type);
        _registrations[alias] = new ScriptRegistration(alias, type, isType, instance);
        RegistrationsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Executes in a fresh lexical block so let and const declarations can be run repeatedly.
    /// </summary>
    /// <param name="code">The script source.</param>
    /// <param name="cancellationToken">A cancellation token for queued and executing work.</param>
    public async Task ExecuteAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        var gate = EngineLocks.GetValue(Engine, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Topaz can execute CPU-bound script synchronously before its first await.
            await Task.Run(async () => await Engine.ExecuteScriptAsync("{\n" + code + "\n}", cancellationToken)
                .ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private void RegisterDefaults()
    {
        Engine.AddNamespace("System", null, true);
        foreach (var (alias, type) in new (string, Type)[] {
            ("string", typeof(string)), ("int", typeof(int)), ("date", typeof(DateTime)),
            ("file", typeof(File)), ("directory", typeof(Directory)), ("double", typeof(double)),
            ("math", typeof(Math)), ("guid", typeof(Guid)), ("StringBuilder", typeof(StringBuilder)) })
            RegisterType(type, alias);
        RegisterObject("JSON", new JSONObject());
        RegisterObject("globalThis", new GlobalThis(Engine.GlobalScope));
        RegisterObject("globals", Globals);
        Engine.AddExtensionMethods(typeof(StringExtensions));
        Engine.AddExtensionMethods(typeof(NumericExtensions));
        Engine.AddExtensionMethods(typeof(ObjectExtensions));
        Engine.AddExtensionMethods(typeof(Enumerable));
        foreach (var module in new object[] { new ProcessScriptCommands(), new HashScriptCommands(),
            new ClipboardScriptCommands(), new HttpScriptCommands(), new ScreenshotScriptCommands(),
            new MouseScriptCommands(), new EnvironmentScriptCommands(), new LogScriptCommands(),
            new AiScriptCommands(), new RegexScriptCommands(), new UiScriptCommands() })
            RegisterModule(module);
    }

    private static string ResolveAlias(Type type, string? alias)
    {
        alias ??= type.GetCustomAttribute<ScriptModuleAttribute>()?.Name ?? type.Name;
        ValidateAlias(alias);
        return alias;
    }

    private static void ValidateAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || !Regex.IsMatch(alias, @"^[$\p{L}_][$\p{L}\p{N}_]*$"))
            throw new ArgumentException("A script alias must be a JavaScript identifier.", nameof(alias));
    }
}

/// <summary>
/// Describes an alias exposed to scripting and completion.
/// </summary>
/// <param name="Alias">The JavaScript identifier.</param>
/// <param name="Type">The reflected .NET type.</param>
/// <param name="IsType">Whether this is a type alias.</param>
/// <param name="Instance">The optional registered instance.</param>
public sealed record ScriptRegistration(string Alias, Type Type, bool IsType, object? Instance);
