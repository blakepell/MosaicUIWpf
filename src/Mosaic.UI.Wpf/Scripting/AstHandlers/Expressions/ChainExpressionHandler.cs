using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class ChainExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (ChainExpression)expression;
            // Possible values:
            // CallExpression | ComputedMemberExpression | StaticMemberExpression
            return scriptExecutor.ExecuteStatement(expr.Expression, token);
        }
    }
}
