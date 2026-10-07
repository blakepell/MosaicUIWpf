using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Statements
{
    internal static partial class IfStatementHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node statement, CancellationToken token)
        {
            var expr = (IfStatement)statement;
            var test = expr.Test;
            var onTrue = expr.Consequent;
            var onFalse = expr.Alternate;
            if (JavascriptTypeUtility
                .IsObjectTrue(scriptExecutor.ExecuteExpressionAndGetValue(test, token)))
            {
                return scriptExecutor.ExecuteStatement(onTrue, token);
            }

            return scriptExecutor.ExecuteStatement(onFalse, token);
        }
    }
}
