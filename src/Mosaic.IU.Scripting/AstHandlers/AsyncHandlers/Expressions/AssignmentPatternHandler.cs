using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class AssignmentPatternHandler
    {
        internal async static ValueTask<object> ExecuteAsync(
            ScriptExecutor scriptExecutor,
            Node expression, CancellationToken token)
        {
            var expr = (AssignmentPattern)expression;
            var left = await scriptExecutor.ExecuteStatementAsync(expr.Left, token);
            var right = await scriptExecutor.ExecuteExpressionAndGetValueAsync(expr.Right, token);
            scriptExecutor
                .SetReferenceValue(left, right);
            return right;
        }
    }
}
