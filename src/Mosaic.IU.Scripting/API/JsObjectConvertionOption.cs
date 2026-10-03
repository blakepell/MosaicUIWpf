using System;

namespace Mosaic.UI.Scripting.API
{
    [Flags]
    public enum JsObjectConverterOption
    {
        None,
        UseLowerCasePropertyNames,
        CreateConcurrentJsObject
    }
}
