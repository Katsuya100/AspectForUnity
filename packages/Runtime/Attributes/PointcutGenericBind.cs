using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.GenericParameter)]
    public class PointcutGenericBind : Attribute
    {
        public GenericBinding Binding { get; private set; }

        public PointcutGenericBind(GenericBinding binding = GenericBinding.GenericParameterName)
        {
            Binding = binding;
        }
    }
}
