using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.Expressions
{
    internal static partial class UpdateExpressionHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            return UnaryExpressionHandler.Execute(scriptExecutor, expression, token);
        }
    }
}
