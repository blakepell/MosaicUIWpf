using Esprima.Ast;
using System;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Statements
{
    internal static partial class ThrowStatementHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node statement, CancellationToken token)
        {
            var expr = (ThrowStatement)statement;
            var err = scriptExecutor.ExecuteExpressionAndGetValue(expr.Argument, token);
            if (err is Exception e)
            {
                throw e;
            }

            throw new Exception(err.ToString());
        }
    }
}
