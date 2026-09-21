using System;

namespace Katuusagi.AspectForUnity
{
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
