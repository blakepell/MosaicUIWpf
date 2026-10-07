using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class UpdateExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            return UnaryExpressionHandler.Execute(scriptExecutor, expression, token);
        }
    }
}
