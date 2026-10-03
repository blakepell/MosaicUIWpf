using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class BinaryExpressionHandler
    {
        internal async static ValueTask<object> ExecuteAsync(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (BinaryExpression)expression;
            var left = await scriptExecutor.ExecuteExpressionAndGetValueAsync(expr.Left, token);
            var right = await scriptExecutor .ExecuteExpressionAndGetValueAsync(expr.Right, token);
            return ExecuteBinaryOperator(scriptExecutor, expr.Operator, left, right);
        }
    }
}
