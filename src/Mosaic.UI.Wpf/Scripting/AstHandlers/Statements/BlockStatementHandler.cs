using Esprima.Ast;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Statements
{
    internal static partial class BlockStatementHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node statement, CancellationToken token)
        {
            var expr = (BlockStatement)statement;
            var list = expr.Body;
            var len = list.Count;
            scriptExecutor = scriptExecutor.NewBlockScope();
            for (var i = 0; i < len; ++i)
            {
                var el = list[i];
                var result = scriptExecutor.ExecuteStatement(el, token);
                if (result is ReturnWrapper ||
                    result is BreakWrapper ||
                    result is ContinueWrapper)
                {
                    scriptExecutor.ReturnToPool();
                    return result;
                }
            }
            var returnValue = scriptExecutor.GetNullOrUndefined();
            scriptExecutor.ReturnToPool();
            return returnValue;
        }
    }
}
