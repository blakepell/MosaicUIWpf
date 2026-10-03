using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class MemberExpressionHandler
    {
        internal async static ValueTask<object> ExecuteAsync(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (MemberExpression)expression;
            var obj = await scriptExecutor.ExecuteStatementAsync(expr.Object, token);
            var prop = await scriptExecutor.ExecuteStatementAsync(expr.Property, token);
            return new TopazMemberAccessor(obj, prop, expr.Computed, expr.Optional);
        }
    }
}
