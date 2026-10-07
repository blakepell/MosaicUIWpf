using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class MemberExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var expr = (MemberExpression)expression;
            var obj = scriptExecutor.ExecuteStatement(expr.Object, token);
            var prop = scriptExecutor.ExecuteStatement(expr.Property, token);
            return new ScriptMemberAccessor(obj, prop, expr.Computed, expr.Optional);
        }
    }
}
