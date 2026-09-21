using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal abstract class TypeNamePointcutInfo : IPointcutInfo
    {
        protected readonly string TypeName;

        protected TypeNamePointcutInfo(CustomAttribute pointcut)
            : this(pointcut, 0)
        {
        }

        protected TypeNamePointcutInfo(CustomAttribute pointcut, int argumentIndex)
        {
            TypeName = GetTypeName(pointcut.ConstructorArguments[argumentIndex]);
        }

        public abstract bool IsMatch(MethodReference method);

        protected static bool IsTypeMatch(TypeReference type, string typeName)
        {
            if (type == null || string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            var normalizedTypeName = NormalizeTypeName(typeName);
            return NormalizeTypeName(type.FullName) == normalizedTypeName ||
                   type.Name == typeName;
        }

        protected static string GetTypeName(CustomAttributeArgument argument)
        {
            if (argument.Value is TypeReference typeReference)
            {
                return typeReference.FullName;
            }

            return argument.Value as string;
        }

        private static string NormalizeTypeName(string typeName)
        {
            var assemblySeparator = typeName.IndexOf(',');
            if (assemblySeparator >= 0)
            {
                typeName = typeName.Substring(0, assemblySeparator);
            }

            return typeName.Trim().Replace('+', '/');
        }
    }
}
