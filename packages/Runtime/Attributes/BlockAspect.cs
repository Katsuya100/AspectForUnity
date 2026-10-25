using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Prevents aspects from applying to the attributed target.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Constructor)]
    public class BlockAspect : Attribute
    {
    }
}
