using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Specifies how a generic advice parameter is bound to a pointcut.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.GenericParameter)]
    public class PointcutGenericBind : Attribute
    {
        public GenericBinding Binding { get; private set; }

        public PointcutGenericBind(GenericBinding binding = GenericBinding.GenericParameterName)
        {
            Binding = binding;
        }
    }
}
