using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using System.Text;
using System.Threading;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Expressions
{
    internal static partial class TemplateLiteralHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node expression, CancellationToken token)
        {
            var literal = (TemplateLiteral)expression;
            var quasis = literal.Quasis;
            var list = literal.Expressions;
            var quasisLen = quasis.Count;
            var listLen = list.Count;
            var sb = new StringBuilder();
            for (var i = 0; i < quasisLen; i++)
            {
                var quasi = quasis[i];
                sb.Append(quasi.Value.Cooked);
                if (i < listLen)
                {
                    sb.Append(scriptExecutor.ExecuteExpressionAndGetValue(list[i], token));
                }
            }
            return sb.ToString();
        }
    }
}
