using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Configures which method-name components are emitted for a pointcut.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OutputPointcutMethodName : Attribute
    {
        public PointcutNameFlag PointcutNameFlag { get; private set; }
        public OutputPointcutMethodName(PointcutNameFlag pointcutNameFlag)
        {
            PointcutNameFlag = pointcutNameFlag;
        }
    }
}
