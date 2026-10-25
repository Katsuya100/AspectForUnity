using Katuusagi.AspectForUnity;
using System;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.GlobalAssembly
{
    [Aspect]
    public static class AsyncExecutionSequenceTestAspect
    {
        public static int AsyncBeforeCount;
        public static int AsyncAfterReturningCount;
        public static int AsyncAfterThrowingCount;
        public static int AsyncAfterCount;
        public static int CustomBeforeCount;
        public static int CustomAfterReturningCount;
        public static int CustomAfterCount;
        public static int CustomReturnedValue;
        public static int CustomAfterThrowingCount;
        public static string CustomThrownType;
        public static int CustomVoidBeforeCount;
        public static int CustomVoidAfterCount;
        public static int TaskSourceAfterReturningCount;
        public static int TaskSourceAfterCount;
        public static int TaskSourceAfterThrowingCount;
        public static int TaskSourceReturnedValue;
        public static int UnityAwaitableBeforeCount;
        public static int UnityAwaitableAfterReturningCount;
        public static int UnityAwaitableAfterThrowingCount;
        public static int UnityAwaitableAfterCount;
        public static int UnityAwaitableReturnedValue;
        public static int UnityAwaitableVoidBeforeCount;
        public static int UnityAwaitableVoidAfterCount;
        public static int CoroutineBeforeCount;
        public static int CoroutineAfterCount;
        public static int IteratorAfterOnlyCount;
        public static int IteratorAfterThrowingCount;
        public static int IteratorAfterThrowingAfterCount;
        public static int IteratorAfterReturningCount;
        public static int IteratorAfterReturningAfterCount;
        public static InvalidOperationException IteratorAfterReturningException;
        public static int AsyncBeforeFailureCount;
        public static readonly System.Collections.Generic.List<string> IteratorCompletionEvents = new System.Collections.Generic.List<string>();
        public static readonly System.Collections.Generic.List<string> ReturnTypeBindingResults = new System.Collections.Generic.List<string>();
        public static int GenericParameterNameBindingCount;
        public static string GenericParameterNameBindingResult;
        public static int ParameterTypeBindingCount;
        public static string ParameterTypeBindingResult;
        public static string LastMethodName;
        public static int LastReturnedValue;
        public static string LastThrownType;

        public static void Reset()
        {
            AsyncBeforeCount = 0;
            AsyncAfterReturningCount = 0;
            AsyncAfterThrowingCount = 0;
            AsyncAfterCount = 0;
            CustomBeforeCount = 0;
            CustomAfterReturningCount = 0;
            CustomAfterCount = 0;
            CustomReturnedValue = 0;
            CustomAfterThrowingCount = 0;
            CustomThrownType = string.Empty;
            CustomVoidBeforeCount = 0;
            CustomVoidAfterCount = 0;
            TaskSourceAfterReturningCount = 0;
            TaskSourceAfterCount = 0;
            TaskSourceAfterThrowingCount = 0;
            TaskSourceReturnedValue = 0;
            UnityAwaitableBeforeCount = 0;
            UnityAwaitableAfterReturningCount = 0;
            UnityAwaitableAfterThrowingCount = 0;
            UnityAwaitableAfterCount = 0;
            UnityAwaitableReturnedValue = 0;
            UnityAwaitableVoidBeforeCount = 0;
            UnityAwaitableVoidAfterCount = 0;
            CoroutineBeforeCount = 0;
            CoroutineAfterCount = 0;
            IteratorAfterOnlyCount = 0;
            IteratorAfterThrowingCount = 0;
            IteratorAfterThrowingAfterCount = 0;
            IteratorAfterReturningCount = 0;
            IteratorAfterReturningAfterCount = 0;
            IteratorAfterReturningException = null;
            AsyncBeforeFailureCount = 0;
            IteratorCompletionEvents.Clear();
            ReturnTypeBindingResults.Clear();
            GenericParameterNameBindingCount = 0;
            GenericParameterNameBindingResult = string.Empty;
            ParameterTypeBindingCount = 0;
            ParameterTypeBindingResult = string.Empty;
            LastMethodName = string.Empty;
            LastReturnedValue = 0;
            LastThrownType = string.Empty;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AsyncBefore([PointcutMethod] MethodBase method)
        {
            AsyncBeforeCount++;
            LastMethodName = method.Name;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AsyncAfterReturning([PointcutReturned] int result)
        {
            AsyncAfterReturningCount++;
            LastReturnedValue = result;
        }

        [Advice(JoinPoint.AfterThrowing, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AsyncAfterThrowing([PointcutThrown] Exception exception)
        {
            AsyncAfterThrowingCount++;
            LastThrownType = exception.GetType().Name;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AsyncAfter()
        {
            AsyncAfterCount++;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomBefore()
        {
            CustomBeforeCount++;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomAfterReturning([PointcutReturned] int result)
        {
            CustomAfterReturningCount++;
            CustomReturnedValue = result;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomAfter()
        {
            CustomAfterCount++;
        }

        [Advice(JoinPoint.AfterThrowing, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomAfterThrowing([PointcutThrown] Exception exception)
        {
            CustomAfterThrowingCount++;
            CustomThrownType = exception.GetType().Name;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeVoidSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomVoidBefore()
        {
            CustomVoidBeforeCount++;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CustomTaskLikeVoidSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CustomVoidAfter()
        {
            CustomVoidAfterCount++;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::TaskCompletionSourceSetResultBeforeBuilderCompletion$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void TaskSourceAfterReturning([PointcutReturned] int result)
        {
            TaskSourceAfterReturningCount++;
            TaskSourceReturnedValue = result;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::TaskCompletionSourceSetResultBeforeBuilderCompletion$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void TaskSourceAfter()
        {
            TaskSourceAfterCount++;
        }

        [Advice(JoinPoint.AfterThrowing, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::TaskCompletionSourceSetExceptionBeforeBuilderCompletion$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void TaskSourceAfterThrowing([PointcutThrown] Exception exception)
        {
            TaskSourceAfterThrowingCount++;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableBefore()
        {
            UnityAwaitableBeforeCount++;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableAfterReturning([PointcutReturned] int result)
        {
            UnityAwaitableAfterReturningCount++;
            UnityAwaitableReturnedValue = result;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableSuccess$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableAfter()
        {
            UnityAwaitableAfterCount++;
        }

        [Advice(JoinPoint.AfterThrowing, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableAfterThrowing([PointcutThrown] Exception exception)
        {
            UnityAwaitableAfterThrowingCount++;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableVoid$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableVoidBefore()
        {
            UnityAwaitableVoidBeforeCount++;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::UnityAwaitableVoid$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnityAwaitableVoidAfter()
        {
            UnityAwaitableVoidAfterCount++;
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineSequence$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CoroutineBefore()
        {
            CoroutineBeforeCount++;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineSequence$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void CoroutineAfter()
        {
            CoroutineAfterCount++;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterOnly(Normal|Failure)$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterOnly()
        {
            IteratorAfterOnlyCount++;
        }

        [Advice(JoinPoint.AfterThrowing, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterThrowingFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterThrowing([PointcutThrown] Exception exception)
        {
            IteratorAfterThrowingCount++;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterThrowingFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterThrowingAfter()
        {
            IteratorAfterThrowingAfterCount++;
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterReturning(Normal|Failure)?$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterReturningAdvice()
        {
            IteratorAfterReturningCount++;
            IteratorCompletionEvents.Add("after-returning");
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterReturningThrows$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterReturningThrows()
        {
            IteratorAfterReturningCount++;
            IteratorCompletionEvents.Add("after-returning");
            throw IteratorAfterReturningException;
        }

        [Advice(JoinPoint.After, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::CoroutineAfterReturning(Normal|Failure|Throws)?$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IteratorAfterReturningAfter()
        {
            IteratorAfterReturningAfterCount++;
            IteratorCompletionEvents.Add("after");
        }

        [Advice(JoinPoint.Before, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncBeforeFailure$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ThrowFromAsyncBefore()
        {
            AsyncBeforeFailureCount++;
            throw new InvalidOperationException("async-before-failure");
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncReturnType(Int|String)$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ReturnTypeBinding<[PointcutGenericBind(GenericBinding.ReturnType)] T>([PointcutMethod] MethodBase method, [PointcutReturned] T result)
        {
            ReturnTypeBindingResults.Add($"{method.Name}:{typeof(T).FullName}:{result}");
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncGenericParameterName$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GenericParameterNameBinding<[PointcutGenericBind(GenericBinding.GenericParameterName)] T>([PointcutReturned] T result)
        {
            GenericParameterNameBindingCount++;
            GenericParameterNameBindingResult = $"{typeof(T).FullName}:{result}";
        }

        [Advice(JoinPoint.AfterReturning, scope: AdviceScope.AsyncExecutionSequence)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AsyncExecutionSequenceTest::AsyncParameterTypeBinding$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ParameterTypeBinding<[PointcutGenericBind(GenericBinding.ParameterType)] T>(T value)
        {
            ParameterTypeBindingCount++;
            ParameterTypeBindingResult = $"{typeof(T).FullName}:{value}";
        }
    }

}
