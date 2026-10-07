using System;

namespace Mosaic.UI.Wpf.Scripting.API
{
    [Flags]
    public enum JsObjectConverterOption
    {
        None,
        UseLowerCasePropertyNames,
        CreateConcurrentJsObject
    }
}
