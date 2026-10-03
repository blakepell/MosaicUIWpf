using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class ConditionalExpressionHandler
    {
        internal async static ValueTask<object> ExecuteAsync(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (ConditionalExpression)expression;
            var test = expr.Test;
            var onTrue = expr.Consequent;
            var onFalse = expr.Alternate;
            if (JavascriptTypeUtility
                .IsObjectTrue(await scriptExecutor.ExecuteExpressionAndGetValueAsync(test, token)))
            {
                return await scriptExecutor.ExecuteStatementAsync(onTrue, token);
            }

            return await scriptExecutor.ExecuteStatementAsync(onFalse, token);
        }
    }
}
