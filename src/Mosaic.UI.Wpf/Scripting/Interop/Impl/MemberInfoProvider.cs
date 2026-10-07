using System;
using System.Reflection;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public class MemberInfoProvider : IMemberInfoProvider
    {
        public MemberInfo[] GetInstanceMembers(object instance, string memberName)
        {
            return instance.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);
        }

        public MemberInfo[] GetStaticMembers(Type type, string memberName)
        {
            return type.GetMember(memberName, BindingFlags.Public | BindingFlags.Static);
        }
    }
}
