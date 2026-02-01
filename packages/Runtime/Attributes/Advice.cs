using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class Advice : Attribute
    {
        public JoinPoint JoinPoint { get; private set; }
        public bool UnsafeInjection { get; private set; }

        public Advice(JoinPoint joinPoint, bool unsafeInjection = false)
        {
            JoinPoint = joinPoint;
            UnsafeInjection = unsafeInjection;
        }
    }
}
