using System;
using System.Collections.Generic;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public sealed class DefaultMemberAccessPolicy : IMemberAccessPolicy
    {
        readonly ScriptEngine _scriptEngine;

        static readonly HashSet<string> TypeMemberWhiteList = new ()
        {
            "IsClass",
            "FullName",
            "Namespace",
            "ToString",
            "IsEnum",
            "IsValueType",
            "IsPrimitive",
            "GetTypeCode",
            "GetEnumName",
            "GetEnumNames",
            "GetEnumValues",
            "IsAssignableFrom",
            "IsAssignableTo",
            "IsSubclassOf"
        };

        public DefaultMemberAccessPolicy(ScriptEngine scriptEngine)
        {
            _scriptEngine = scriptEngine;
        }

        public bool IsObjectMemberAccessAllowed(object obj, string memberName)
        {
            if (obj == null || memberName == null)
            {
                return true;
            }

            var enableReflection = _scriptEngine.Options.SecurityPolicy
                .HasFlag(Options.SecurityPolicy.EnableReflection);
            if (enableReflection)
            {
                return true;
            }

            if (obj is Type)
            {
                return TypeMemberWhiteList.Contains(memberName);
            }
            return true;
        }
    }
}
