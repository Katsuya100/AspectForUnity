using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class DeclaringTypePointcut : TypeNamePointcutBase
    {
        public DeclaringTypePointcut(string typeName)
            : base(typeName)
        {
        }

        public DeclaringTypePointcut(Type type)
            : base(type)
        {
        }
    }
}
