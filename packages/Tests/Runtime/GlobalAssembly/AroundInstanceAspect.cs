using Katuusagi.AspectForUnity;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests
{
    [Aspect]
    public sealed class AroundInstanceAspect
    {
        public static int ConstructorCount;
        public static string ConstructorMetadata;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundInstanceTarget::Multiply$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public AroundInstanceAspect([PointcutMethod] MethodBase method,
                                    [PointcutParameters] ParameterArray parameters)
        {
            ConstructorCount++;
            ConstructorMetadata = $"{method.Name}:{parameters.Length}";
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundInstanceTarget::Multiply$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public void Around([PointcutProceed] ProceedingContext<int> context)
        {
            context.Proceed();
        }
    }
}
