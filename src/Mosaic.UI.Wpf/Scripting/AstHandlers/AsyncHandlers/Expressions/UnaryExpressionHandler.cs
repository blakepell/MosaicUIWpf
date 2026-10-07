using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class UnaryExpressionHandler
    {
        internal async static ValueTask<object> ExecuteAsync(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var unaryExpr = (UnaryExpression)expression;
            var expr = await scriptExecutor.ExecuteStatementAsync(unaryExpr.Argument, token);
            var value = scriptExecutor.GetValue(expr);
            var unaryOperator = unaryExpr.Operator;
            if (unaryOperator == UnaryOperator.Delete)
            {
                scriptExecutor.SetReferenceValue(expr, scriptExecutor.GetNullOrUndefined());
                return scriptExecutor.GetNullOrUndefined();
            }
            var newValue = ExecuteUnaryOperator(scriptExecutor, unaryOperator, value);
            if (unaryOperator == UnaryOperator.Increment ||
                unaryOperator == UnaryOperator.Decrement ||
                unaryOperator == UnaryOperator.Delete)
            {
                scriptExecutor.SetReferenceValue(expr, newValue);
            }

            return unaryExpr.Prefix ? newValue : value;
        }
    }
}
