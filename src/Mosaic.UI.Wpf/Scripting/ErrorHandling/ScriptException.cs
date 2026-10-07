using System;

namespace Mosaic.UI.Wpf.Scripting
{
    public sealed class ScriptException : Exception
    {
        public ScriptException(string message) : base(message)
        {
        }
    }
}
