using Mosaic.UI.Wpf.Scripting.Interop;
using Mosaic.UI.Wpf.Scripting.Options;

namespace Mosaic.UI.Wpf.Scripting
{
    /// <summary>
    /// Initialization properties for ScriptEngine constructor.
    /// If you don't set some property in the setup, ScriptEngine will use default implementation.
    /// </summary>
    public sealed class ScriptEngineSetup
    {
        public bool IsThreadSafe { get; set; } = true;

        public ScriptEngineOptions Options { get; set; }

        public IObjectProxyRegistry ObjectProxyRegistry { get; set; }

        public IObjectProxy DefaultObjectProxy { get; set; }

        public IDelegateInvoker DelegateInvoker { get; set; }

        public IMemberAccessPolicy MemberAccessPolicy { get; set; }

        public IValueConverter ValueConverter { get; set; }

        public IMemberInfoProvider MemberInfoProvider { get; set; }

        public IAwaitExpressionHandler AwaitExpressionHandler { get; set; }
    }
}
