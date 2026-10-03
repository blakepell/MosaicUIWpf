using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting
{
    internal sealed class TopazMemberAccessor
    {
        internal object Instance { get; }

        internal object Property { get; }
    
        internal bool Computed { get; }

        internal bool Optional { get; }

        internal TopazMemberAccessor(object instance, object property, bool computed, bool optional)
        {
            Instance = instance;
            Property = property;
            Computed = computed;
            Optional = optional;
        }

        internal object Execute(ScriptExecutor executionScope)
        {
            return executionScope.GetMemberValue(Instance, Property, Computed, Optional);
        }

        public override string ToString()
        {
            return $"{Instance}{(Optional ? "?" : "")}.{Property}";
        }
    }
}
