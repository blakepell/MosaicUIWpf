using Esprima;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mosaic.UI.Scripting.Core
{
    internal sealed partial class ScriptExecutor : IScriptEngineScope
    {
        bool IScriptEngineScope.IsThreadSafe => IsThreadSafeScope;

        bool IScriptEngineScope.IsReadOnly { get => IsReadOnly; set => IsReadOnly = value; }

        bool IScriptEngineScope.IsFrozen { get => IsFrozen; set => IsFrozen = value; }

        bool IScriptEngineScope.IsGlobalScope => ScopeType == ScopeType.Global;

        void IScriptEngineScope.ExecuteScript(string code, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            var script = new JavaScriptParser(code, Options.ParserOptions)
                .ParseScript();
            ExecuteScript(script, token);
        }

        object IScriptEngineScope.ExecuteExpression(string code, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var script = new JavaScriptParser(code, Options.ParserOptions)
                .ParseExpression();
            return ExecuteExpressionAndGetValue(script, token);
        }

        object IScriptEngineScope.InvokeFunction(string name, CancellationToken token, params object[] args)
        {
            return CallFunction(
                new TopazIdentifier(name),
                args.ToArray(), false, token);
        }

        object IScriptEngineScope.InvokeFunction(object functionObject, CancellationToken token, params object[] args)
        {
            return CallFunction(functionObject, args, false, token);
        }

        IScriptEngineScope IScriptEngineScope.NewChildScope(bool? isThreadSafe)
        {
            return NewCustomScope(isThreadSafe);
        }

        object IScriptEngineScope.GetValue(string name)
        {
            return GetVariableValue(name);
        }

        void IScriptEngineScope.SetValue(string name, object value)
        {
            AddOrUpdateVariableValueInTheScope(name, value, VariableKind.Var);
        }

        void IScriptEngineScope.SetValueAndKind(
            string name, object value, VariableKind variableKind)
        {
            AddOrUpdateVariableValueAndKindInTheScope
                (name, value, variableKind);
        }

        async Task IScriptEngineScope.ExecuteScriptAsync(string code, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            var script = new JavaScriptParser(code, Options.ParserOptions)
                .ParseScript();
            await ExecuteScriptAsync(script, token);
        }

        async Task<object> IScriptEngineScope.ExecuteExpressionAsync(string code, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var script = new JavaScriptParser(code, Options.ParserOptions)
                .ParseExpression();
            return await ExecuteExpressionAndGetValueAsync(script, token);
        }

        async Task<object> IScriptEngineScope.InvokeFunctionAsync(string name, CancellationToken token, params object[] args)
        {
            return await CallFunctionAsync(
                new TopazIdentifier(name),
                args.ToArray(), false, token);
        }

        async Task<object> IScriptEngineScope.InvokeFunctionAsync(object functionObject, CancellationToken token, params object[] args)
        {
            return await CallFunctionAsync(functionObject, args, false, token);
        }
    }
}
