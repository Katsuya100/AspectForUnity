using System;

namespace Katuusagi.AspectForUnity.Tests.LocalAssembly
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SimplePointcutMethodMarkerAttribute : Attribute
    {
    }
}
