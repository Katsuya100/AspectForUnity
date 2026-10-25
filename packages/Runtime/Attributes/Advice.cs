using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Marks a method or constructor as an advice.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class Advice : Attribute
    {
        public JoinPoint JoinPoint { get; private set; }
        public bool UnsafeInjection { get; private set; }
        public AdviceScope Scope { get; private set; }

        public Advice(JoinPoint joinPoint, bool unsafeInjection = false)
            : this(joinPoint, unsafeInjection, AdviceScope.Invocation)
        {
        }

        public Advice(JoinPoint joinPoint, AdviceScope scope)
            : this(joinPoint, false, scope)
        {
        }

        public Advice(JoinPoint joinPoint,
                      bool unsafeInjection,
                      AdviceScope scope)
        {
            JoinPoint = joinPoint;
            UnsafeInjection = unsafeInjection;
            Scope = scope;
        }
    }
}
