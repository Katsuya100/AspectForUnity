using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class AttributeTypePointcut : TypeNamePointcutBase
    {
        public AttributeTypePointcut(string typeName)
            : base(typeName)
        {
        }

        public AttributeTypePointcut(Type type)
            : base(type)
        {
        }
    }
}
