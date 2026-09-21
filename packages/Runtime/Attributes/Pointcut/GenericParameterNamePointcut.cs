using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public sealed class GenericParameterNamePointcut : PointcutBase
    {
        public int RepeatedIndex { get; private set; }
        public bool HasRepeatedIndex { get; private set; }
        public string Name { get; private set; }

        public GenericParameterNamePointcut(string name)
        {
            Name = name;
        }

        public GenericParameterNamePointcut(int repeatedIndex, string name)
        {
            RepeatedIndex = repeatedIndex;
            HasRepeatedIndex = true;
            Name = name;
        }
    }
}
