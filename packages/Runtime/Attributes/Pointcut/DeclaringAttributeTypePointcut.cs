using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class DeclaringAttributeTypePointcut : TypeNamePointcutBase
    {
        public DeclaringAttributeTypePointcut(string typeName)
            : base(typeName)
        {
        }

        public DeclaringAttributeTypePointcut(Type type)
            : base(type)
        {
        }
    }
}
