using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class MethodNamePointcut : PointcutBase
    {
        public string Name { get; private set; }

        public MethodNamePointcut(string name)
        {
            Name = name;
        }
    }
}
