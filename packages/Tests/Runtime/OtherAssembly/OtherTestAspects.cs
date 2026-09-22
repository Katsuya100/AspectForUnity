using System;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.OtherAssembly
{
    // Before Aspect
    [Aspect]
    public static class OtherBeforeAspect
    {
        public static string BeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::BeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void BeforeAdvice([PointcutMethod] MethodBase method)
        {
            BeforeAdviceResult = $"Other Before: {method.Name}";
        }

        public static string ComplexBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ComplexBeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ComplexBeforeAdvice([PointcutMethod] MethodBase method)
        {
            ComplexBeforeAdviceResult = $"Other Before Complex: {method.Name}";
        }
    }

    // AfterReturning Aspect
    [Aspect]
    public static class OtherAfterReturningAspect
    {
        public static string AfterReturningResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::AfterReturningMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturning([PointcutMethod] MethodBase method, [PointcutReturned] object result)
        {
            AfterReturningResult = $"Other AfterReturning: {method.Name}, Result: {result}, Assembly: {method.Module.Assembly.GetName().Name}";
        }

    }

    // AfterThrowing Aspect - êVãKí«â¡
    [Aspect]
    public static class OtherAfterThrowingAspect
    {
        public static string ThrowingAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::AfterThrowingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ThrowingAdvice([PointcutMethod] MethodBase method, [PointcutThrown] Exception exception)
        {
            ThrowingAdviceResult = $"AfterThrowing: {method.Name}, Exception: {exception.GetType().Name}";
        }
    }

    // After Aspect - êVãKí«â¡
    [Aspect]
    public static class OtherAfterAspect
    {
        public static string AfterAdviceResult = string.Empty;

        [Advice(JoinPoint.After)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::AfterMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterAdvice([PointcutMethod] MethodBase method)
        {
            AfterAdviceResult = $"After: {method.Name}";
        }
    }

    // Signature Aspect
    [Aspect]
    public static class OtherSignatureAspect
    {
        public static string GlobalSignatureAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ParametersMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GlobalSignatureAdvice([PointcutMethod] MethodBase method, [PointcutParameters] ParameterArray parameters)
        {
            GlobalSignatureAdviceResult = $"Other Signature: {method.Module.Assembly.GetName().Name}.{method.DeclaringType.FullName}.{method.Name}, ParamCount: {parameters.Length}";
        }

        public static string ParameterHandlingAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ParameterHandlingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ParameterHandlingAdvice([PointcutMethod] MethodBase method, [PointcutParameters] ParameterArray parameters)
        {
            ParameterHandlingAdviceResult = $"Other Parameter Handling: {method.Name}, ParamCount: {parameters.Length}";
        }
    }

    // Complex Aspect
    [Aspect]
    public static class OtherComplexAspect
    {
        public static string ComplexMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ComplexBeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ComplexMethodAdvice([PointcutMethod] MethodBase method)
        {
            ComplexMethodAdviceResult = $"Other Complex Method: {method.Name}";
        }

        public static string ComplexGenericMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ComplexGenericBeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ComplexGenericMethodAdvice([PointcutMethod] MethodBase method)
        {
            ComplexGenericMethodAdviceResult = $"Other Before Complex Generic: {method.Name}";
        }

        public static string ArgumentExceptionAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::MultipleThrowingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ArgumentExceptionAdvice([PointcutMethod] MethodBase method, [PointcutThrown] ArgumentException exception)
        {
            ArgumentExceptionAdviceResult = $"ArgumentException: {method.Name}, Message: {exception.Message}";
        }

        public static string InvalidOperationExceptionAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::MultipleThrowingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void InvalidOperationExceptionAdvice([PointcutMethod] MethodBase method, [PointcutThrown] InvalidOperationException exception)
        {
            InvalidOperationExceptionAdviceResult = $"InvalidOperationException: {method.Name}, Message: {exception.Message}";
        }
    }

    // Generic Test Aspect
    [Aspect]
    public static class OtherGenericBeforeAspect
    {
        public static string GenericBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::GenericBeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GenericBeforeAdvice<T>([PointcutMethod] MethodBase method)
        {
            GenericBeforeAdviceResult = $"Other Generic Before Method: {method.Name} Type: {typeof(T).Name}";
        }

        public static string ComplexGenericBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ComplexGenericBeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ComplexGenericMethodAdvice([PointcutMethod] MethodBase method)
        {
            ComplexGenericBeforeAdviceResult = $"Other Complex Generic Before Method: {method.Name}";
        }
    }

    // Scope Test Aspect
    [Aspect]
    public static class OtherScopeAspect
    {
        public static string AssemblyScopeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest::ScopeTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AssemblyScopeAdvice([PointcutMethod] MethodBase method)
        {
            var assembly = method.Module.Assembly.GetName().Name;
            AssemblyScopeAdviceResult = $"Other Assembly Scope: {method.Name}, FromAssembly: {assembly}";
        }

        public static string NestedClassScopeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest\\.NestedTestClass::ScopeTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void NestedClassScopeAdvice([PointcutMethod] MethodBase method)
        {
            var assembly = method.Module.Assembly.GetName().Name;
            NestedClassScopeAdviceResult = $"Other Nested Class Scope: {method.Name}, FromAssembly: {assembly}";
        }
    }

    // Delegate Aspect - êVãKí«â¡
    [Aspect]
    public static class OtherDelegateAspect
    {
        public static string DelegateAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.OtherAspectTest\\.EventClass::OnEvent$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void DelegateAdvice([PointcutMethod] MethodBase method)
        {
            DelegateAdviceResult = $"Delegate: {method.Name}";
        }
    }
}