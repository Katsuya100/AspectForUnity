using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class DeclaringTypePointcutInfo : TypeNamePointcutInfo
    {
        public DeclaringTypePointcutInfo(CustomAttribute pointcut)
            : base(pointcut)
        {
        }

        public override bool IsMatch(MethodReference method)
        {
            return IsTypeMatch(method.DeclaringType, TypeName);
        }
    }
}
