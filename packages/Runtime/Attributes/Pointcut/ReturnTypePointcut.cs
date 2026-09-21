using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class ReturnTypePointcut : TypeNamePointcutBase
    {
        public ReturnTypePointcut(string typeName)
            : base(typeName)
        {
        }

        public ReturnTypePointcut(Type type)
            : base(type)
        {
        }
    }
}
