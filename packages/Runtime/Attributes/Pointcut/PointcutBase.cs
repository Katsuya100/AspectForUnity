using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Base class for built-in pointcut attributes.</summary>
    /// <remarks>
    /// This base supports AspectForUnity's built-in concrete pointcuts. Defining and registering custom pointcuts by deriving from this type is not supported.
    /// </remarks>
    public abstract class PointcutBase: Attribute
    {
    }
}
