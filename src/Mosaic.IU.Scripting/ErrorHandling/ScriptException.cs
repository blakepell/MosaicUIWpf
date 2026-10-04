using System;

namespace Mosaic.UI.Scripting
{
    public sealed class ScriptException : Exception
    {
        public ScriptException(string message) : base(message)
        {
        }
    }
}
