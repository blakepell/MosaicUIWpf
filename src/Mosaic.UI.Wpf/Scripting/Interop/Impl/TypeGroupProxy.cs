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
using System.Collections.Generic;
using System.Linq;
using Mosaic.UI.Wpf.Scripting.ErrorHandling;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    /// <summary>
    /// Proxy for a set of types that share one script name but differ in generic arity,
    /// eg: System.Action, System.Action`1, System.Action`2.
    /// The constructor picks the type whose arity equals the number of leading type arguments,
    /// so <c>new Tuple(String, Int32, 'a', 1)</c> creates a Tuple&lt;string, int&gt;.
    /// </summary>
    public sealed class TypeGroupProxy : ITypeProxy
    {
        private readonly Dictionary<int, ITypeProxy> proxiesByArity;

        private readonly ITypeProxy defaultProxy;

        /// <summary>
        /// The script name of the group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The types in the group, ordered by generic arity.
        /// </summary>
        public IReadOnlyList<Type> Types { get; }

        /// <summary>
        /// The non-generic type if the group has one,
        /// otherwise the generic type definition with the lowest arity.
        /// Static member access is forwarded to this type.
        /// </summary>
        public Type ProxiedType => defaultProxy.ProxiedType;

        public TypeGroupProxy(string name, IEnumerable<Type> types, Func<Type, ITypeProxy> proxyFactory)
        {
            Name = name;
            Types = types.OrderBy(GetArity).ToArray();
            if (Types.Count == 0)
            {
                throw new ArgumentException("A type group requires at least one type.", nameof(types));
            }

            proxiesByArity = new Dictionary<int, ITypeProxy>();
            foreach (var type in Types)
            {
                proxiesByArity.TryAdd(GetArity(type), proxyFactory(type));
            }
            defaultProxy = proxiesByArity[GetArity(Types[0])];
        }

        public object CallConstructor(IReadOnlyList<object> args)
        {
            var typeArgumentCount = 0;
            while (typeArgumentCount < args.Count && IsTypeArgument(args[typeArgumentCount]))
            {
                ++typeArgumentCount;
            }

            // Prefer the generic type that consumes every leading type argument, then fall back
            // to fewer so a non-generic constructor can still take a Type as its first argument.
            for (var arity = typeArgumentCount; arity >= 0; --arity)
            {
                if (proxiesByArity.TryGetValue(arity, out var proxy))
                {
                    return proxy.CallConstructor(args);
                }
            }

            Exceptions.ThrowCanNotCallConstructorWithGivenArguments(Name, args);
            return null;
        }

        public bool TryGetStaticMember(object member, out object value, bool isIndexedProperty = false)
        {
            return defaultProxy.TryGetStaticMember(member, out value, isIndexedProperty);
        }

        public bool TrySetStaticMember(object member, object value, bool isIndexedProperty = false)
        {
            return defaultProxy.TrySetStaticMember(member, value, isIndexedProperty);
        }

        public override string ToString()
        {
            return Name;
        }

        internal static int GetArity(Type type)
        {
            return type.IsGenericTypeDefinition ? type.GetGenericArguments().Length : 0;
        }

        private static bool IsTypeArgument(object arg)
        {
            return arg is Type ||
                (arg is ITypeProxy typeProxy && typeProxy is not NamespaceProxy && typeProxy.ProxiedType != null);
        }
    }
}
