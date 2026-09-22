using Katuusagi.AspectForUnity;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests
{
    [Aspect]
    public static class AroundTestAspects
    {
        public static readonly List<string> Events = new List<string>();

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Add$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void Before(int value)
        {
            Events.Add($"before:{value}");
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Add$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void Outer([PointcutProceed] ProceedingContext<int> context, int value)
        {
            Events.Add($"outer-before:{value}");
            context.Proceed();
            Events.Add($"outer-result:{context.ReturnValue}");
            Events.Add("outer-after");
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Add$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void Inner([PointcutProceed] ProceedingContext<int> context, int value)
        {
            Events.Add($"inner-before:{value}");
            context.Proceed();
            Events.Add($"inner-result:{context.ReturnValue}");
            Events.Add("inner-after");
        }

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Add$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturning([PointcutReturned] int value)
        {
            Events.Add($"after-returning:{value}");
        }

        [Advice(JoinPoint.After)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Add$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void After()
        {
            Events.Add("after");
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Touch$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void VoidAround([PointcutProceed] ProceedingContext context, int value)
        {
            Events.Add($"void-before:{value}");
            context.Proceed();
            Events.Add("void-after");
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::Echo$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ReferenceAround([PointcutProceed] ProceedingContext<string> context, string value)
        {
            context.Proceed();
            Events.Add($"reference-result:{context.ReturnValue}");
        }

        [Advice(JoinPoint.Around, unsafeInjection: true)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::UnsafeAdd$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnsafeAround([PointcutProceed] UnsafeProceedingContext<int> context, int value)
        {
            context.Proceed();
            context.ReturnValue += value;
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::RefTarget$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void RefAround([PointcutProceed] ref ProceedingContext<int> context, int value)
        {
            context.Proceed();
            Events.Add($"ref-result:{context.ReturnValue}");
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::NoProceed$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void NoProceedAround([PointcutProceed] ProceedingContext<int> context)
        {
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::DuplicateProceed$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void DuplicateProceedAround([PointcutProceed] ProceedingContext<int> context)
        {
            context.Proceed();
            context.Proceed();
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::ThrowingTarget$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void FailedProceedAround([PointcutProceed] ProceedingContext<int> context)
        {
            try
            {
                context.Proceed();
            }
            catch (InvalidOperationException)
            {
                Events.Add("proceed-failed");
                AssertSecondProceedFails(context);
                throw;
            }
        }

        private static void AssertSecondProceedFails(ProceedingContext<int> context)
        {
            try
            {
                context.Proceed();
            }
            catch (InvalidOperationException)
            {
                Events.Add("proceed-reuse-failed");
            }
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundInstanceTarget::Multiply$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void MetadataAround([PointcutProceed] ProceedingContext<int> context,
                                          [PointcutMethod] MethodBase method,
                                          [PointcutThis] object target,
                                          [PointcutParameters] ParameterArray parameters,
                                          int value)
        {
            Events.Add($"metadata:{method.Name}:{target.GetType().Name}:{parameters.Length}:{value}");
            context.Proceed();
        }

        [Advice(JoinPoint.Around)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.AroundTest::GenericIdentity$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GenericAround<[PointcutGenericBind(GenericBinding.ReturnType)] T>([PointcutProceed] ProceedingContext<T> context)
        {
            context.Proceed();
            Events.Add($"generic-result:{context.ReturnValue}");
        }
    }

}
