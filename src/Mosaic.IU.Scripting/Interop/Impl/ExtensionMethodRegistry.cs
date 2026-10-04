using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mosaic.UI.Scripting.Interop
{
    /// <summary>
    /// The extension methods a script can call on any value. Types are added by the host with
    /// AddExtensionMethods and by include statements that import a namespace with extension classes.
    /// </summary>
    /// <remarks>
    /// Scripts add types from their worker thread while editors read the methods, so every change
    /// publishes a new immutable snapshot; a reader that holds a snapshot can tell it apart by reference.
    /// </remarks>
    public sealed class ExtensionMethodRegistry
    {
        private readonly object sync = new();

        private readonly HashSet<Type> registeredTypes = new();

        private MethodInfo[] registeredMethods = Array.Empty<MethodInfo>();

        private Dictionary<string, MethodAndParameterInfo> methodAndParameterInfoMap;

        /// <summary>
        /// A snapshot of the registered extension methods; replaced, never mutated, when types are added or removed.
        /// </summary>
        public IReadOnlyList<MethodInfo> ExtensionMethods => registeredMethods;

        public MethodAndParameterInfo GetMethodAndParameterInfo(string name)
        {
            var map = methodAndParameterInfoMap ?? InitMethodCache();
            return map.TryGetValue(name, out var result) ? result : MethodAndParameterInfo.Empty;
        }

        /// <summary>
        /// Adds the extension methods declared by a type.
        /// </summary>
        /// <param name="type">The static class that declares the extension methods.</param>
        /// <returns>False when the type was already registered.</returns>
        public bool AddType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            lock (sync)
            {
                if (!registeredTypes.Add(type))
                {
                    return false;
                }

                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(m => m.IsDefined(typeof(ExtensionAttribute), true))
                    .ToArray();
                if (methods.Length > 0)
                {
                    registeredMethods = registeredMethods.Concat(methods).ToArray();
                    methodAndParameterInfoMap = null;
                }
                return true;
            }
        }

        /// <summary>
        /// Removes the extension methods declared by a type.
        /// </summary>
        /// <param name="type">A type added with <see cref="AddType"/>.</param>
        /// <returns>False when the type was not registered.</returns>
        public bool RemoveType(Type type)
        {
            lock (sync)
            {
                if (!registeredTypes.Remove(type))
                {
                    return false;
                }

                registeredMethods = registeredMethods.Where(m => m.DeclaringType != type).ToArray();
                methodAndParameterInfoMap = null;
                return true;
            }
        }

        public Dictionary<string, MethodAndParameterInfo> InitMethodCache()
        {
            lock (sync)
            {
                var methods = registeredMethods;
                var map = methods.GroupBy(x => x.Name).ToDictionary(
                    g => g.Key,
                    g => new MethodAndParameterInfo(g.ToArray(), g.Select(x => x.GetParameters()).ToArray()));
                methodAndParameterInfoMap = map;
                return map;
            }
        }
    }
}
