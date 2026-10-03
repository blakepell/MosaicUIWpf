using System.Collections.Generic;

namespace Mosaic.UI.Scripting.Interop
{
    public interface IDelegateInvoker
    {
        object Invoke(object function, IReadOnlyList<object> args);
    }
}
