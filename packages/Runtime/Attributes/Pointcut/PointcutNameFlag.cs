namespace Katuusagi.AspectForUnity
{
    public enum PointcutNameFlag : ulong
    {
        AssemblyAttribute = 1ul << 0,
        AssemblyName = 1ul << 1,
        ModuleAttribute = 1ul << 2,
        DeclaringTypeAttribute = 1ul << 3,
        DeclaringTypeName = 1ul << 4,
        DeclaringTypeGenericArgumentAttribute = 1ul << 5,
        DeclaringTypeGenericArgumentName = 1ul << 6,
        MethodAttribute = 1ul << 7,
        MethodName = 1ul << 8,
        ReturnTypeAttribute = 1ul << 9,
        ReturnTypeName = 1ul << 10,
        GenericArgumentAttribute = 1ul << 11,
        GenericArgumentName = 1ul << 12,
        ParameterAttribute = 1ul << 13,
        ParameterTypeName = 1ul << 14,
        ParameterName = 1ul << 15,
        MethodAccessModifier = 1ul << 16,
        MethodStaticModifier = 1ul << 17,
        MethodOverrideModifier = 1ul << 18,

        AttributeArguments = 1ul << 59,
        AttributeProperties = 1ul << 60,
        AncestorDeclaringTypeAttribute = 1ul << 61,
        AssemblyFullName = 1ul << 62,
        TypeFullName = 1ul << 63,

        Simple = ReturnTypeName | DeclaringTypeName | MethodName | GenericArgumentName | ParameterTypeName | ParameterName,
        LocalSignature = TypeFullName | Simple,
        GlobalSignature = AssemblyName | LocalSignature,
        All = ulong.MaxValue,
    }
}
