using Mosaic.UI.Wpf.Scripting.API;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting
{
    internal sealed class ScriptObjectWrapper
    {
        internal ScriptExecutor ScriptExecutor { get; }

        internal IJsObject WrappedObject { get; }

        bool isUnwrapped;

        internal ScriptObjectWrapper(
            ScriptExecutor scriptExecutor,
            IJsObject value)
        {
            ScriptExecutor = scriptExecutor;
            WrappedObject = value;
        }

        internal IJsObject UnwrapObject()
        {
            var value = WrappedObject;
            if (value == null)
            {
                return null;
            }

            if (isUnwrapped)
            {
                return value;
            }

            value.UnwrapObject(ScriptExecutor);
            isUnwrapped = true;
            return value;
        }
    }
}
