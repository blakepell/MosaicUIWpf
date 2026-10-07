using Mosaic.UI.Wpf.Scripting.API;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting
{
    internal sealed class ScriptArrayWrapper
    {
        internal ScriptExecutor ScriptExecutor { get; }

        internal IJsArray WrappedArray { get; }

        bool isUnwrapped;

        internal ScriptArrayWrapper(ScriptExecutor scriptExecutor, IJsArray array)
        {
            ScriptExecutor = scriptExecutor;
            WrappedArray = array;
        }

        internal object UnwrapArray()
        {
            var array = WrappedArray;
            if (array == null)
            {
                return null;
            }

            if (isUnwrapped)
            {
                return array;
            }

            WrappedArray.UnwrapArray(ScriptExecutor);
            isUnwrapped = true;
            return array;
        }
    }
}
