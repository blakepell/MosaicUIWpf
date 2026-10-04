using Mosaic.UI.Scripting.ErrorHandling;
using Mosaic.UI.Scripting.Interop;

namespace Mosaic.UI.Scripting.Core
{
    internal sealed partial class ScriptExecutor
    {
        internal bool TryGetVariableInTheScope(string name, out Variable variable)
        {
            var scope = this;
            if (scope.IsThreadSafeScope)
            {
                if (scope.SafeVariables.TryGetValue(name, out variable))
                {
                    return true;
                }
            }
            else
            {
                if (scope.UnsafeVariables.TryGetValue(name, out variable))
                {
                    return true;
                }
            }
            variable = null;
            return false;
        }

        internal Variable GetVariable(string name)
        {
            var scope = this;
            while (scope != null)
            {
                if (scope.isEmptyScope)
                {
                    scope = scope.ParentScope;
                    continue;
                }
                if (scope.IsThreadSafeScope)
                {
                    if (scope.SafeVariables.TryGetValue(name, out var variable))
                    {
                        return variable;
                    }
                }
                else
                {
                    if (scope.UnsafeVariables.TryGetValue(name, out var variable))
                    {
                        return variable;
                    }
                }
                scope = scope.ParentScope;
            }

            return null;
        }

        internal object GetVariableValue(string name)
        {
            var scope = this;
            while (scope != null)
            {
                if (scope.isEmptyScope)
                {
                    scope = scope.ParentScope;
                    continue;
                }
                if (scope.IsThreadSafeScope)
                {
                    if (scope.SafeVariables.TryGetValue(name, out var variable))
                    {
                        return variable.Value;
                    }
                }
                else
                {
                    if (scope.UnsafeVariables.TryGetValue(name, out var variable))
                    {
                        return variable.Value;
                    }
                }
                scope = scope.ParentScope;
            }

            if (ScriptEngine.TryResolveImport(name, out var importedType))
            {
                return importedType;
            }

            if (Options.AllowUndefinedReferenceAccess)
            {
                return Options.NoUndefined ? null : Undefined.Value;
            }

            Exceptions.ThrowVariableIsNotDefined(name);
            return null;
        }

        internal object GetValue(object value)
        {
            if (value is ScriptIdentifier identifier)
            {
                return identifier.GetVariableValue(this);
            }
            else if (value is ScriptMemberAccessor memberAccessor)
            {
                return memberAccessor.Execute(this);
            }
            else if (value is ScriptArrayWrapper arrayWrapper)
            {
                value = arrayWrapper.UnwrapArray();
            }
            else if (value is ScriptObjectWrapper objectWrapper)
            {
                value = objectWrapper.UnwrapObject();
            }

            return value;
        }

        internal object GetVariableNameOrValue(object value, bool getVariableName)
        {
            if (getVariableName && value is ScriptIdentifier identifier)
            {
                return identifier.Name;
            }
            return GetValue(value);
        }

        internal object GetMemberValue(object instance, object property, bool computed, bool optional)
        {
            var obj = GetValue(instance);
            var member = GetVariableNameOrValue(property, !computed);

            if (obj == null)
            {
                if (optional)
                {
                    return null;
                }

                if (Options.AllowNullReferenceMemberAccess)
                {
                    return GetNullOrUndefined();
                }

                Exceptions.ThrowCannotReadPropertiesOfNull(member);
            }

            if (obj is ITypeProxy typeProxy)
            {
                if (typeProxy
                    .TryGetStaticMember(
                        member?.ToString(),
                        out var staticMemberValue,
                        computed))
                {
                    return staticMemberValue;
                }

                return GetNullOrUndefined();
            }

            if (obj == Undefined.Value)
            {
                if (Options.AllowUndefinedReferenceMemberAccess)
                {
                    return GetNullOrUndefined();
                }

                Exceptions.ThrowReferenceIsUndefined(instance);
            }

            if (ScriptEngine.TryGetObjectMember(obj, member, out var value, computed))
            {
                return value;
            }

            return GetNullOrUndefined();
        }
    }
}
