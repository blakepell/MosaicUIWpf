using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class ChainExpressionHandler
    {
        internal async static ValueTask<object> ExecuteAsync(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (ChainExpression)expression;
            // Possible values:
            // CallExpression | ComputedMemberExpression | StaticMemberExpression
            return await scriptExecutor.ExecuteStatementAsync(expr.Expression, token);
        }
    }
}
