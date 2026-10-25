using System;

namespace Katuusagi.AspectForUnity
{
    /// <summary>Base class for built-in pointcuts that match a type by name or type.</summary>
    /// <remarks>
    /// This base supports AspectForUnity's built-in concrete pointcuts. Defining and registering custom pointcuts by deriving from this type is not supported.
    /// </remarks>
    public abstract class TypeNamePointcutBase : PointcutBase
    {
        public string TypeName { get; private set; }
        public Type Type { get; private set; }

        protected TypeNamePointcutBase(string typeName)
        {
            TypeName = typeName;
        }

        protected TypeNamePointcutBase(Type type)
        {
            Type = type;
            TypeName = type?.FullName;
        }
    }
}
