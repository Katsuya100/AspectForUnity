using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.LocalAssembly
{
    [Aspect]
    public struct LocalStructInstanceAspect
    {
        private int _instanceId;

        private static int NextInstanceId;
        public static string LocalConstructorResult = string.Empty;
        public static string InstanceBeforeAdviceResult = string.Empty;
        public static int ConstructorInstanceId;
        public static int BeforeInstanceId;

        public static void Reset()
        {
            NextInstanceId = 0;
            LocalConstructorResult = string.Empty;
            InstanceBeforeAdviceResult = string.Empty;
            ConstructorInstanceId = 0;
            BeforeInstanceId = 0;
        }

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::StructInstanceTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public LocalStructInstanceAspect([PointcutMethod] MethodBase method)
        {
            _instanceId = ++NextInstanceId;
            ConstructorInstanceId = _instanceId;
            LocalConstructorResult = $"Local Struct Instance Constructor: {method.Name}";
        }

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::StructInstanceTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public void InstanceBeforeAdvice([PointcutMethod] MethodBase method)
        {
            BeforeInstanceId = _instanceId;
            InstanceBeforeAdviceResult = $"Local Struct Instance Before: {method.Name}";
        }
    }
}
