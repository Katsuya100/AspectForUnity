using Katuusagi.AspectForUnity;
using System;

namespace Katuusagi.AspectForUnity.Tests.GlobalAssembly
{
    [Aspect]
    public static class StateMachineParameterTestAspect
    {
        public static int BeforeCount;
        public static int LastValue;
        public static string LastText;
        public static bool BodyStarted;
        public static bool BodyHadStarted;
        public static bool ThrowNext;
        public static int AfterCount;
        public static int AfterValue;
        public static string AfterText;
        public static bool AfterHadStarted;
        public static int IteratorAfterCount;
        public static int IteratorAfterValue;

        public static void Reset()
        {
            BeforeCount = 0;
            LastValue = 0;
            LastText = string.Empty;
            BodyStarted = false;
            BodyHadStarted = false;
            ThrowNext = false;
            AfterCount = 0;
            AfterValue = 0;
            AfterText = string.Empty;
            AfterHadStarted = false;
            IteratorAfterCount = 0;
            IteratorAfterValue = 0;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncPointcutParameters$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void Before([PointcutParameters] ParameterArray parameters)
        {
            BeforeCount++;
            LastValue = (int)parameters[0];
            LastText = (string)parameters[1];
            BodyHadStarted = BodyStarted;
            if (ThrowNext)
            {
                ThrowNext = false;
                throw new InvalidOperationException("state machine advice failure");
            }
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncPointcutParameters$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturning([PointcutParameters] ParameterArray parameters)
        {
            AfterCount++;
            AfterValue = (int)parameters[0];
            AfterText = (string)parameters[1];
            AfterHadStarted = BodyStarted;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutinePointcutParameters$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CoroutineAfter([PointcutParameters] ParameterArray parameters)
        {
            IteratorAfterCount++;
            IteratorAfterValue = (int)parameters[0];
        }
    }
}
