using System.Collections;
using Mosaic.UI.Scripting.Core;

namespace Mosaic.UI.Scripting.API
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