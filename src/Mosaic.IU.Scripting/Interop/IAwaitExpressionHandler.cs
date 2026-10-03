using System.Threading;
using System.Threading.Tasks;

namespace Mosaic.UI.Scripting.Interop
{
    public interface IAwaitExpressionHandler
    {
        Task<object> HandleAwaitExpression(object awaitObject, CancellationToken token);
    }
}