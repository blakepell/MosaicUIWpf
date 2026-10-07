using System.Collections;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.API
{
    public interface IJsArray
    {
        bool TryGetArrayValue(int index, out object value);

        void SetArrayValue(int index, object value);

        void AddArrayValue(object value);
    
        void AddArrayValues(IEnumerable enumerable);

        internal void UnwrapArray(ScriptExecutor scriptExecutor);
    }
}
