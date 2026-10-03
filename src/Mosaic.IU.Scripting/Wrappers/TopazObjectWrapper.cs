using Mosaic.UI.Scripting.API;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting
{
    internal sealed class TopazObjectWrapper
    {
        internal ScriptExecutor ScriptExecutor { get; }

        internal IJsObject WrappedObject { get; }

        bool isUnwrapped;

        internal TopazObjectWrapper(
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
