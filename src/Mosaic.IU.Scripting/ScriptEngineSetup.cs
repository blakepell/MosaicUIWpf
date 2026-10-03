using Mosaic.UI.Scripting.Interop;
using Mosaic.UI.Scripting.Options;

namespace Mosaic.UI.Scripting
{
    /// <summary>
    /// Initialization properties for TopazEngine constructor.
    /// If you don't set some property in the setup, TopazEngine will use default implementation.
    /// </summary>
    public sealed class ScriptEngineSetup
    {
        public bool IsThreadSafe { get; set; } = true;

        public TopazEngineOptions Options { get; set; }

        public IObjectProxyRegistry ObjectProxyRegistry { get; set; }

        public IObjectProxy DefaultObjectProxy { get; set; }

        public IDelegateInvoker DelegateInvoker { get; set; }

        public IMemberAccessPolicy MemberAccessPolicy { get; set; }

        public IValueConverter ValueConverter { get; set; }

        public IMemberInfoProvider MemberInfoProvider { get; set; }

        public IAwaitExpressionHandler AwaitExpressionHandler { get; set; }
    }
}
