using System;

namespace Mosaic.UI.Scripting.Interop
{
    [Flags]
    public enum ProxyOptions
    {
        None,
        AllowConstructor,
        AllowMethod,
        AllowField,
        AllowProperty,
        AutomaticTypeConversion,
        Default = 
            AllowMethod |
            AllowField |
            AllowProperty |
            AllowConstructor |
            AutomaticTypeConversion
    }
}
