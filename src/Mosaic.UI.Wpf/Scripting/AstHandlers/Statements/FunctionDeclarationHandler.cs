using Esprima.Ast;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.Statements
{
    internal static partial class FunctionDeclarationHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node statement)
        {
            var expr = (FunctionDeclaration)statement;
            var identifier = expr.Id;
            var name = identifier?.Name ?? string.Empty;
            var function = new ScriptFunction(
                scriptExecutor.NewFunctionScope(),
                name,
                expr);
            scriptExecutor.DefineVariable(identifier, function, VariableKind.Var);
            return function;
        }
    }
}
