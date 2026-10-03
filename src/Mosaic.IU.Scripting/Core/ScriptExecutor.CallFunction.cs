using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.ErrorHandling;
using Mosaic.UI.Scripting.Interop;

namespace Mosaic.UI.Scripting.Core
{
    internal sealed partial class ScriptExecutor
    {
        internal object CallFunction(object callee, IReadOnlyList<object> args, bool optional, CancellationToken token)
        {
            var value = GetValue(callee);
            if (value == null)
            {
                if (optional)
                {
                    return GetNullOrUndefined();
                }

                Exceptions.ThrowFunctionIsNotDefined(callee, this);
            }

            if (value is ScriptFunction topazFunction)
            {
                return topazFunction.Execute(args, token);
            }

            if (value is IInvokable invokable)
            {
                return invokable.Invoke(args);
            }

            return ScriptEngine.DelegateInvoker.Invoke(value, args);
        }

        internal async ValueTask<object> CallFunctionAsync(object callee, IReadOnlyList<object> args, bool optional, CancellationToken token)
        {
            var value = GetValue(callee);
            if (value == null)
            {
                if (optional)
                {
                    return GetNullOrUndefined();
                }

                Exceptions.ThrowFunctionIsNotDefined(callee, this);
            }

            if (value is ScriptFunction topazFunction)
            {
                return await topazFunction.ExecuteAsync(args, token);
            }

            if (value is IInvokable invokable)
            {
                return invokable.Invoke(args);
            }

            return ScriptEngine.DelegateInvoker.Invoke(value, args);
        }

        internal object GetNullOrUndefined()
        {
            return Options.NoUndefined ? null : Undefined.Value;
        }
    }
}
