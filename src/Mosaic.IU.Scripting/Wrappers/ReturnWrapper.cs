namespace Mosaic.UI.Scripting
{
    internal sealed class ReturnWrapper
    {
        internal object Result { get; }

        internal ReturnWrapper(object result)
        {
            Result = result;
        }
    }
}
