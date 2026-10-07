using System.Collections;
using Mosaic.UI.Wpf.Scripting.Core;

namespace Mosaic.UI.Wpf.Scripting.API
{
    public interface IJsObject
    {
        bool TryGetValue(object key, out object value);

        IEnumerable GetObjectKeys();

        void SetValue(object key, object value);

        internal void UnwrapObject(ScriptExecutor scriptExecutor);

        internal bool IsPrototypeProperty(object member);
    }
}