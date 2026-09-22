using System;
using System.Collections.Generic;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.GlobalAssembly
{
    [Aspect]
    public static class AdviceControlFlowAspect
    {
        private const string Pattern =
            "^Katuusagi\\.AspectForUnity\\.Tests\\.AdviceControlFlowTest::" +
            "(MultiReturnMethod|CatchReturnMethod|ThrowingMethod|ThrowingAfterReturningMethod)$";

        public static readonly List<string> Events = new List<string>();
        public static int BeforeCount;
        public static int AfterReturningCount;
        public static int AfterThrowingCount;
        public static int AfterCount;
        public static bool ThrowFromAfterReturning;

        public static void Reset()
        {
            Events.Clear();
            BeforeCount = 0;
            AfterReturningCount = 0;
            AfterThrowingCount = 0;
            AfterCount = 0;
            ThrowFromAfterReturning = false;
        }

        [Advice(JoinPoint.Before)]
        [RegexPointcut(Pattern, PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void BeforeAdvice([PointcutMethod] MethodBase method, [PointcutThis] object instance, [PointcutParameters] ParameterArray parameters)
        {
            BeforeCount++;
            Events.Add($"before:{method.Name}:{instance.GetType().Name}:{parameters.Length}");
        }

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut(Pattern, PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturningAdvice([PointcutMethod] MethodBase method, [PointcutReturned] int result)
        {
            AfterReturningCount++;
            Events.Add($"after-returning:{method.Name}:{result}");
            if (ThrowFromAfterReturning)
            {
                throw new InvalidOperationException("after-returning advice failure");
            }
        }

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut(Pattern, PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterThrowingAdvice([PointcutMethod] MethodBase method, [PointcutThrown] Exception exception, [PointcutParameters] ParameterArray parameters)
        {
            AfterThrowingCount++;
            Events.Add($"after-throwing:{method.Name}:{exception.GetType().Name}:{parameters.Length}:{parameters[0]}");
        }

        [Advice(JoinPoint.After)]
        [RegexPointcut(Pattern, PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterAdvice([PointcutMethod] MethodBase method, [PointcutParameters] ParameterArray parameters)
        {
            AfterCount++;
            Events.Add($"after:{method.Name}:{parameters.Length}");
        }
    }

    [Aspect]
    public static class GenericReturnAdviceAspect
    {
        public static readonly List<object> Results = new List<object>();

        public static void Reset()
        {
            Results.Clear();
        }

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AdviceControlFlowTest::GenericReturnMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturning([PointcutReturned] object result)
        {
            Results.Add(result);
        }
    }

    [Aspect]
    public static class ConstructorPointcutAspect
    {
        public static string InstanceType;
        public static bool BodyExecuted;

        public static void Reset()
        {
            InstanceType = null;
            BodyExecuted = false;
        }

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AdviceControlFlowTest\\.ConstructorTestClass::\\.ctor$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void BeforeConstructor([PointcutThis] object instance)
        {
            InstanceType = instance.GetType().Name;
        }
    }
}
