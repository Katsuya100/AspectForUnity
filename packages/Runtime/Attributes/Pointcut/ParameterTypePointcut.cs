using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class ParameterTypePointcut : TypeNamePointcutBase
    {
        public int RepeatedIndex { get; private set; }
        public bool HasRepeatedIndex { get; private set; }

        public ParameterTypePointcut(string typeName)
            : base(typeName)
        {
        }

        public ParameterTypePointcut(Type type)
            : base(type)
        {
        }

        public ParameterTypePointcut(int repeatedIndex, string typeName)
            : base(typeName)
        {
            RepeatedIndex = repeatedIndex;
            HasRepeatedIndex = true;
        }

        public ParameterTypePointcut(int repeatedIndex, Type type)
            : base(type)
        {
            RepeatedIndex = repeatedIndex;
            HasRepeatedIndex = true;
        }
    }
}
