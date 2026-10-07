using Esprima.Ast;
using Mosaic.UI.Wpf.Scripting.ErrorHandling;

namespace Mosaic.UI.Wpf.Scripting.Core
{
    internal sealed partial class ScriptExecutor
    {
        internal void DefineVariable(
            object identifierOrReference,
            object value,
            VariableKind kind,
            VariableState state = VariableState.None)
        {
            if (identifierOrReference is Identifier identifier)
            {
                DefineVariable(identifier, value, kind, state);
                return;
            }
            if (identifierOrReference is ScriptIdentifier scriptIdentifier)
            {
                DefineVariable(scriptIdentifier, value, kind, state);
                return;
            }
            if (identifierOrReference is string str)
            {
                DefineVariable(str, value, kind, state);
                return;
            }
            Exceptions.ThrowCannotDefineVariableWithGivenObject(identifierOrReference);
        }

        internal void DefineVariable(
            string name,
            object value,
            VariableKind kind,
            VariableState state = VariableState.None)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Exceptions.ThrowVariableNameCannotBeNullOrWhitespace();
            }

            if (!isEmptyScope && TryGetVariableInTheScope(name, out var variable) &&
                kind != VariableKind.Var &&
                variable.State != VariableState.Captured)
            {
                Exceptions.ThrowVariableIsAlreadyDefined(name, this);
            }

            AddOrUpdateVariableValueAndKindInTheScope(name, value, kind, state);
        }

        internal void DefineVariable(
            ScriptIdentifier scriptIdentifier,
            object value,
            VariableKind kind,
            VariableState state = VariableState.None)
        {
            if (scriptIdentifier == null)
            {
                return;
            }

            var name = scriptIdentifier.Name;
            DefineVariable(name, value, kind, state);
            scriptIdentifier.InvalidateLocalCache();
        }

        internal void DefineVariable(
            Identifier identifier,
            object value,
            VariableKind kind,
            VariableState state = VariableState.None)
        {
            if (identifier == null)
            {
                return;
            }

            var name = identifier.Name;
            DefineVariable(name, value, kind, state);
            identifier.ScriptIdentifier.InvalidateLocalCache();
        }
    }
}
