using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class MethodNamePointcutInfo : IPointcutInfo
    {
        private readonly string _name;

        public MethodNamePointcutInfo(CustomAttribute pointcut)
        {
            _name = pointcut.ConstructorArguments[0].Value as string;
        }

        public bool IsMatch(MethodReference method)
        {
            return method.Name == _name;
        }
    }
}
