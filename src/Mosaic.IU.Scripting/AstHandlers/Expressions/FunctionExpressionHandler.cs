using Esprima.Ast;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class FunctionExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression)
        {
            var expr = (FunctionExpression)expression;
            var identifier = expr.Id;
            var name = identifier?.Name ?? string.Empty;
            var function = new ScriptFunction(
                scriptExecutor.NewFunctionScope(),
                name,
                expr);
            scriptExecutor.DefineVariable(identifier, function, VariableKind.Var);
            return function;
        }
    }
}
