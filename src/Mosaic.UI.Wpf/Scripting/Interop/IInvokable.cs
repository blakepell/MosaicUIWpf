using System.Collections.Generic;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public interface IInvokable
    {
        object Invoke(IReadOnlyList<object> args);
    }
}
