using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Binds an advice parameter to the matched method.</summary>
    /// <remarks>
    /// Deriving custom framework attributes from this type is not supported by AspectForUnity. The IL post-processor does not guarantee recognition of derived attribute types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PointcutMethod : Attribute
    {
    }
}
