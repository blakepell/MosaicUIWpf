using System.Collections.Generic;

namespace Mosaic.UI.Scripting.Interop
{
    public interface IInvokable
    {
        object Invoke(IReadOnlyList<object> args);
    }
}
