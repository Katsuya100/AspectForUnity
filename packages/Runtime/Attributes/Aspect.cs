using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Marks a type as an aspect.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class Aspect: Attribute
    {
    }
}
