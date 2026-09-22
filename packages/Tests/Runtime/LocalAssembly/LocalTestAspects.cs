using System;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.LocalAssembly
{
    // Before Advice Local Aspect - 具体的な正規表現パターンを使用
    [Aspect]
    public static class LocalBeforeAspect
    {
        public static string BeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::BeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void BeforeAdvice([PointcutMethod] MethodBase method)
        {
            BeforeAdviceResult = $"Local Before: {method.Name}";
        }
    }

    // AfterReturning Advice Local Aspect
    [Aspect]
    public static class LocalAfterReturningAspect
    {
        public static string AfterReturningAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::AfterReturningMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturningAdvice([PointcutMethod] MethodBase method, [PointcutReturned] object result)
        {
            AfterReturningAdviceResult = $"Local AfterReturning: {method.Name}, Result: {result}";
        }
    }

    // AfterThrowing Advice Local Aspect
    [Aspect]
    public static class LocalAfterThrowingAspect
    {
        public static string AfterThrowingAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::AfterThrowingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterThrowingAdvice([PointcutMethod] MethodBase method, [PointcutThrown] Exception exception)
        {
            AfterThrowingAdviceResult = $"Local AfterThrowing: {method.Name}, Exception: {exception.GetType().Name}";
        }
    }

    // After Advice Local Aspect
    [Aspect]
    public static class LocalAfterAspect
    {
        public static string AfterAdviceResult = string.Empty;

        [Advice(JoinPoint.After)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::AfterMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterAdvice([PointcutMethod] MethodBase method)
        {
            AfterAdviceResult = $"Local After: {method.Name}";
        }
    }

    // Generic Before Aspect
    [Aspect]
    public static class LocalGenericBeforeAspect
    {
        public static string GenericBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::StaticGenericTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        [RegexPointcut("^static$", PointcutNameFlag.MethodStaticModifier)]
        public static void GenericBeforeAdvice([PointcutMethod] MethodBase method)
        {
            GenericBeforeAdviceResult = $"Local Generic: {method.Name}";
        }
    }

    // Private and Protected Method Aspect
    [Aspect]
    public static class LocalPrivateProtectedAspect
    {
        public static string PrivateMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest\\.PrivateProtectedMethodTestClass::PrivateTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PrivateMethodAdvice([PointcutMethod] MethodBase method)
        {
            PrivateMethodAdviceResult = $"Local Private Method: {method.Name}";
        }

        public static string ProtectedMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest\\.PrivateProtectedMethodTestClass::ProtectedTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ProtectedMethodAdvice([PointcutMethod] MethodBase method)
        {
            ProtectedMethodAdviceResult = $"Local Protected Method: {method.Name}";
        }
    }

    // Operator Overload Aspect
    [Aspect]
    public static class LocalOperatorAspect
    {
        public static string OperatorAdditionAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest\\.OperatorTestClass::op_Addition$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void OperatorAdditionAdvice([PointcutMethod] MethodBase method)
        {
            OperatorAdditionAdviceResult = $"Local Operator Addition: {method.Name}";
        }

        public static string OperatorEqualityAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest\\.OperatorTestClass::op_Equality$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void OperatorEqualityAdvice([PointcutMethod] MethodBase method)
        {
            OperatorEqualityAdviceResult = $"Local Operator Equality: {method.Name}";
        }
    }

    // Parameter Aspect
    [Aspect]
    public static class LocalParameterAspect
    {
        public static string ParameterAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::ParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ParameterAdvice([PointcutMethod] MethodBase method, string parameter)
        {
            ParameterAdviceResult = $"Local Conditional True: {method.Name}, Parameter: {parameter}";
        }
    }

    // Instance Aspect for constructor testing
    [Aspect]
    public class LocalInstanceAspect
    {
        public static string LocalConstructorResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::InstanceTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public LocalInstanceAspect([PointcutMethod] MethodBase method)
        {
            LocalConstructorResult = $"Local Instance Constructor: {method.Name}";
        }

        public static string InstanceBeforeAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::InstanceTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public void InstanceBeforeAdvice([PointcutMethod] MethodBase method)
        {
            InstanceBeforeAdviceResult = $"Local Instance Before: {method.Name}";
        }
    }

    // Complex Parameter Binding Local Aspect
    [Aspect]
    public static class LocalComplexParameterAspect
    {
        public static string ComplexParameterAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::ComplexParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ComplexParameterAdvice<T>([PointcutMethod] MethodBase method, [PointcutParameters] ParameterArray parameters, [PointcutThis] object instance, T genericValue, string text, int number)
        {
            ComplexParameterAdviceResult = $"Local Complex: {method.Name}, Instance: {instance?.GetType().Name}, Generic: {typeof(T).Name}, GenericValue: {genericValue}, Text: {text}, Number: {number}, ParamCount: {parameters.Length}";
        }
    }

    // Pointcut Name Flag Test Local Aspect
    [Aspect]
    public static class LocalPointcutNameFlagAspect
    {
        public static string DeclaringTypeNameAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest\\.LocalTestClass::TestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void DeclaringTypeNameAdvice([PointcutMethod] MethodBase method)
        {
            DeclaringTypeNameAdviceResult = $"Local DeclaringTypeName: {method.Name}";
        }

        public static string ReturnTypeNameAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^System\\.String Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::ReturnStringTestMethod$", PointcutNameFlag.ReturnTypeName | PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ReturnTypeNameAdvice([PointcutMethod] MethodBase method)
        {
            ReturnTypeNameAdviceResult = $"Local ReturnTypeName: {method.Name}";
        }

        public static string ParameterNameAdviceResult = string.Empty;
        
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.LocalAspectTest::ParameterNameTestMethod\\(System\\.String uniqueTextParam\\)$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.ParameterName)]
        public static void ParameterNameAdvice([PointcutMethod] MethodBase method)
        {
            ParameterNameAdviceResult = $"Local ParameterName: {method.Name}";
        }
    }
}