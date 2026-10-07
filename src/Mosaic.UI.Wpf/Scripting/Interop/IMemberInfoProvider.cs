using System;
using System.Reflection;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public interface IMemberInfoProvider
    {
        MemberInfo[] GetInstanceMembers(object instance, string memberName);

        MemberInfo[] GetStaticMembers(Type type, string memberName);
    }
}