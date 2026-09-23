/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Threading;
using Tenray.Topaz.Interop;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// Marshals Topaz member access to a registered WPF object's owning dispatcher.
/// </summary>
internal sealed class DispatcherObjectScriptProxy(IObjectProxy inner, IObjectProxy fallback) : IObjectProxy
{
    /// <inheritdoc />
    public bool TryGetObjectMember(object instance, object member, out object value, bool isIndexedProperty = false)
    {
        var dispatcher = ((DispatcherObject)instance).Dispatcher;
        var result = dispatcher.Invoke(() =>
        {
            bool found = inner.TryGetObjectMember(instance, member, out var memberValue, isIndexedProperty);
            if (!found && !ReferenceEquals(inner, fallback))
            {
                found = fallback.TryGetObjectMember(instance, member, out memberValue, isIndexedProperty);
            }

            // Topaz retrieves a method before invoking it; both operations need the dispatcher.
            if (memberValue is IInvokable method)
            {
                memberValue = new DispatcherInvokable(dispatcher, method);
            }

            return (Found: found, Value: memberValue);
        });
        value = result.Value;
        return result.Found;
    }

    /// <inheritdoc />
    public bool TrySetObjectMember(object instance, object member, object value, bool isIndexedProperty = false)
    {
        return ((DispatcherObject)instance).Dispatcher.Invoke(() =>
            inner.TrySetObjectMember(instance, member, value, isIndexedProperty) ||
            (!ReferenceEquals(inner, fallback) && fallback.TrySetObjectMember(instance, member, value, isIndexedProperty)));
    }

    /// <summary>
    /// Invokes a reflected method on its target object's dispatcher.
    /// </summary>
    private sealed class DispatcherInvokable(Dispatcher dispatcher, IInvokable inner) : IInvokable
    {
        /// <inheritdoc />
        public object Invoke(IReadOnlyList<object> args) => dispatcher.Invoke(() => inner.Invoke(args));
    }
}
