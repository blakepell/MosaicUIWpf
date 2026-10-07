using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class AssignmentPatternHandler
    {
        internal static object Execute(
            ScriptExecutor scriptExecutor,
            Node expression, 
            CancellationToken token)
        {
            var expr = (AssignmentPattern)expression;
            var left = scriptExecutor.ExecuteStatement(expr.Left, token);
            var right = scriptExecutor.ExecuteExpressionAndGetValue(expr.Right, token);
            scriptExecutor
                .SetReferenceValue(left, right);
            return right;
        }
    }
}
