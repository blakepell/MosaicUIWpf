using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class ArrowFunctionExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression)
        {
            var expr = (ArrowFunctionExpression)expression;
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
