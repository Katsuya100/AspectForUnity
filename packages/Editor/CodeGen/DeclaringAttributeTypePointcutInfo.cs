using Mono.Cecil;
using System.Linq;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class DeclaringAttributeTypePointcutInfo : TypeNamePointcutInfo
    {
        public DeclaringAttributeTypePointcutInfo(CustomAttribute pointcut)
            : base(pointcut)
        {
        }

        public override bool IsMatch(MethodReference method)
        {
            return SimplePointcutInfoUtils.GetDeclaringTypes(method)
                .SelectMany(v => v.CustomAttributes)
                .Any(v => IsTypeMatch(v.AttributeType, TypeName));
        }
    }
}
