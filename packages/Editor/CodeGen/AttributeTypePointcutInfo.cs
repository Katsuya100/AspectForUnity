using Mono.Cecil;
using System.Linq;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class AttributeTypePointcutInfo : TypeNamePointcutInfo
    {
        public AttributeTypePointcutInfo(CustomAttribute pointcut)
            : base(pointcut)
        {
        }

        public override bool IsMatch(MethodReference method)
        {
            var methodDefinition = method.Resolve();
            return methodDefinition != null &&
                   methodDefinition.CustomAttributes.Any(v => IsTypeMatch(v.AttributeType, TypeName));
        }
    }
}
