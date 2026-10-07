using System.Collections.Generic;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public interface IDelegateInvoker
    {
        object Invoke(object function, IReadOnlyList<object> args);
    }
}
