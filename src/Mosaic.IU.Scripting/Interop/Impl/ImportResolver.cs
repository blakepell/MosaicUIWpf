/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Mosaic.UI.Scripting.Options;

namespace Mosaic.UI.Scripting.Interop
{
    /// <summary>
    /// Resolves unqualified type names against the namespaces imported with
    /// <see cref="ScriptEngine.Imports(string[])"/>, the way C# resolves names through using directives.
    /// </summary>
    /// <remarks>
    /// Only consulted when an identifier is not found in any scope, so script variables and
    /// registered globals always win. When two imports contain the same name, the namespace
    /// imported first wins; use the full name or AddType to reach the other one.
    /// Results are cached per name, and the type index is rebuilt when new assemblies load.
    /// </remarks>
    internal sealed class ImportResolver
    {
        private sealed record NamespaceImport(string Namespace, IReadOnlySet<string> Whitelist);

        private sealed record CacheEntry(ITypeProxy TypeProxy, bool IsReflection, int AssemblyVersion);

        private static int assemblyVersion;

        private readonly ScriptEngine scriptEngine;

        private readonly object sync = new();

        private readonly List<NamespaceImport> imports = new();

        private readonly ConcurrentDictionary<string, CacheEntry> cache = new(StringComparer.Ordinal);

        /// <summary>
        /// Simple type name (without the generic arity suffix) to the matching types per import, in import order.
        /// </summary>
        private Dictionary<string, List<Type>[]> index;

        private int indexAssemblyVersion = -1;

        private volatile int importCount;

        static ImportResolver()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, _) => Interlocked.Increment(ref assemblyVersion);
        }

        internal ImportResolver(ScriptEngine scriptEngine)
        {
            this.scriptEngine = scriptEngine;
        }

        internal IReadOnlyList<string> Namespaces
        {
            get
            {
                lock (sync)
                {
                    return imports.Select(x => x.Namespace).ToArray();
                }
            }
        }

        /// <returns>False when the namespace was already imported with the same whitelist.</returns>
        internal bool Add(string @namespace, IReadOnlySet<string> whitelist)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentException("A namespace cannot be null or whitespace.", nameof(@namespace));
            }

            lock (sync)
            {
                var existing = imports.FindIndex(x => x.Namespace == @namespace);
                if (existing >= 0 && ReferenceEquals(imports[existing].Whitelist, whitelist))
                {
                    // Scripts re-run their include statements, so an unchanged import keeps the cached index.
                    return false;
                }

                if (existing >= 0)
                {
                    // Re-importing keeps the original precedence but takes the new whitelist.
                    imports[existing] = new NamespaceImport(@namespace, whitelist);
                }
                else
                {
                    imports.Add(new NamespaceImport(@namespace, whitelist));
                }
                Invalidate();
                return true;
            }
        }

        /// <summary>
        /// Gets the public static classes in a namespace that declare extension methods, honoring the whitelist,
        /// so importing a namespace brings its extension methods into scope like a C# using directive.
        /// </summary>
        /// <param name="namespace">The full name of the namespace; sub namespaces are not included.</param>
        /// <param name="whitelist">The full type names that are allowed, or null for every type.</param>
        internal static IEnumerable<Type> GetExtensionTypes(string @namespace, IReadOnlySet<string> whitelist)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetExportedTypes)
                .Where(type => type.IsPublic && type.IsAbstract && type.IsSealed && type.Namespace == @namespace &&
                    (whitelist == null || whitelist.Contains(type.Namespace + "." + GetSimpleName(type))) &&
                    type.IsDefined(typeof(ExtensionAttribute), false));
        }

        internal void Clear()
        {
            lock (sync)
            {
                imports.Clear();
                Invalidate();
            }
        }

        internal bool TryResolve(string name, out ITypeProxy typeProxy)
        {
            typeProxy = null;
            if (importCount == 0 || string.IsNullOrEmpty(name))
            {
                return false;
            }

            var currentAssemblyVersion = Volatile.Read(ref assemblyVersion);
            if (!cache.TryGetValue(name, out var entry) ||
                (entry.TypeProxy == null && entry.AssemblyVersion != currentAssemblyVersion))
            {
                lock (sync)
                {
                    entry = new CacheEntry(null, false, currentAssemblyVersion);
                    if (importCount > 0)
                    {
                        EnsureIndex(currentAssemblyVersion);
                        if (index.TryGetValue(name, out var candidates))
                        {
                            var types = candidates.FirstOrDefault(x => x != null);
                            if (types != null)
                            {
                                entry = new CacheEntry(CreateProxy(name, types), IsReflectionNamespace(types[0].Namespace), currentAssemblyVersion);
                            }
                        }
                    }
                    cache[name] = entry;
                }
            }

            if (entry.TypeProxy == null)
            {
                return false;
            }

            if (entry.IsReflection &&
                !scriptEngine.Options.SecurityPolicy.HasFlag(SecurityPolicy.EnableReflection))
            {
                return false;
            }

            typeProxy = entry.TypeProxy;
            return true;
        }

        private void Invalidate()
        {
            importCount = imports.Count;
            index = null;
            indexAssemblyVersion = -1;
            cache.Clear();
        }

        private void EnsureIndex(int currentAssemblyVersion)
        {
            if (index != null && indexAssemblyVersion == currentAssemblyVersion)
            {
                return;
            }

            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < imports.Count; ++i)
            {
                positions[imports[i].Namespace] = i;
            }

            var newIndex = new Dictionary<string, List<Type>[]>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetExportedTypes(assembly))
                {
                    // Nested types are reached through their declaring type, eg: Environment.SpecialFolder.
                    if (!type.IsPublic || type.Namespace == null ||
                        !positions.TryGetValue(type.Namespace, out var position))
                    {
                        continue;
                    }

                    var name = GetSimpleName(type);
                    var whitelist = imports[position].Whitelist;
                    if (whitelist != null && !whitelist.Contains(type.Namespace + "." + name))
                    {
                        continue;
                    }

                    // The same full name can be exported by more than one assembly; first one wins.
                    if (!seen.Add(type.FullName))
                    {
                        continue;
                    }

                    if (!newIndex.TryGetValue(name, out var perImport))
                    {
                        newIndex[name] = perImport = new List<Type>[imports.Count];
                    }
                    (perImport[position] ??= new List<Type>()).Add(type);
                }
            }

            index = newIndex;
            indexAssemblyVersion = currentAssemblyVersion;
            // Names that missed against the old index may now resolve.
            foreach (var pair in cache)
            {
                if (pair.Value.TypeProxy == null)
                {
                    cache.TryRemove(pair.Key, out _);
                }
            }
        }

        private ITypeProxy CreateProxy(string name, List<Type> types)
        {
            if (types.Count == 1)
            {
                return CreateTypeProxy(types[0]);
            }
            return new TypeGroupProxy(name, types, CreateTypeProxy);
        }

        private ITypeProxy CreateTypeProxy(Type type)
        {
            return (ITypeProxy)TypeProxyUsingReflection.GetTypeProxy(type) ??
                new TypeProxyUsingReflection(type, scriptEngine.ValueConverter, scriptEngine.MemberInfoProvider);
        }

        private static IEnumerable<Type> GetExportedTypes(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return Array.Empty<Type>();
            }

            try
            {
                return assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
            catch (Exception)
            {
                // An assembly that cannot be inspected simply contributes no imported names.
                return Array.Empty<Type>();
            }
        }

        private static string GetSimpleName(Type type)
        {
            var name = type.Name;
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }

        internal static bool IsReflectionNamespace(string @namespace)
        {
            return @namespace != null &&
                (@namespace == "System.Reflection" || @namespace.StartsWith("System.Reflection.", StringComparison.Ordinal));
        }
    }
}
