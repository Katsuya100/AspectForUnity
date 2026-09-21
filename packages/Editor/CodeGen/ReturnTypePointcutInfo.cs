using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class ReturnTypePointcutInfo : TypeNamePointcutInfo
    {
        public ReturnTypePointcutInfo(CustomAttribute pointcut)
            : base(pointcut)
        {
        }

        public override bool IsMatch(MethodReference method)
        {
            return IsTypeMatch(method.ReturnType, TypeName);
        }
    }
}
