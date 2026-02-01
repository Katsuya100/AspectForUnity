using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Constructor)]
    public class BlockAspect : Attribute
    {
    }
}
