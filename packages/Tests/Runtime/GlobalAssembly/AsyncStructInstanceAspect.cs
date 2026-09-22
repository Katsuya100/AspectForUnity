using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.GlobalAssembly
{
    [Aspect]
    public struct AsyncStructInstanceAspect
    {
        private int _instanceId;

        private static int NextInstanceId;
        public static int ConstructorCount;
        public static int AfterCount;
        public static int ConstructorInstanceId;
        public static int AfterInstanceId;
        public static string LastMethodName = string.Empty;
        public static int LastReturnedValue;

        public static void Reset()
        {
            NextInstanceId = 0;
            ConstructorCount = 0;
            AfterCount = 0;
            ConstructorInstanceId = 0;
            AfterInstanceId = 0;
            LastMethodName = string.Empty;
            LastReturnedValue = 0;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncStructSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public AsyncStructInstanceAspect([PointcutMethod] MethodBase method)
        {
            _instanceId = ++NextInstanceId;
            ConstructorCount++;
            ConstructorInstanceId = _instanceId;
            LastMethodName = method.Name;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncStructSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public void AfterReturning([PointcutReturned] int result)
        {
            AfterCount++;
            AfterInstanceId = _instanceId;
            LastReturnedValue = result;
        }
    }
}
