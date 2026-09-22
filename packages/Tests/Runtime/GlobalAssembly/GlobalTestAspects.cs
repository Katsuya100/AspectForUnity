using System;
using System.Collections.Generic;
using System.Reflection;

namespace Katuusagi.AspectForUnity.Tests.GlobalAssembly
{
    // Before Advice Global Aspect - 完全に具体的な正規表現を使用
    [Aspect]
    public static class GlobalBeforeAspect
    {
        public static string BeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::BeforeMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void BeforeAdvice([PointcutMethod] MethodBase method)
        {
            BeforeAdviceResult = $"Global Before: {method.Name}";
        }
    }

    // AfterReturning Advice Global Aspect
    [Aspect]
    public static class GlobalAfterReturningAspect
    {
        public static string AfterReturningAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::AfterReturningMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturningAdvice([PointcutMethod] MethodBase method, [PointcutReturned] object result)
        {
            AfterReturningAdviceResult = $"Global AfterReturning: {method.Name}, Result: {result}";
        }
    }

    // AfterThrowing Advice Global Aspect
    [Aspect]
    public static class GlobalAfterThrowingAspect
    {
        public static string AfterThrowingAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::AfterThrowingMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterThrowingAdvice([PointcutMethod] MethodBase method, [PointcutThrown] Exception exception)
        {
            AfterThrowingAdviceResult = $"Global AfterThrowing: {method.Name}, Exception: {exception.GetType().Name}";
        }
    }

    // After Advice Global Aspect
    [Aspect]
    public static class GlobalAfterAspect
    {
        public static string AfterAdviceResult = string.Empty;

        [Advice(JoinPoint.After)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::AfterMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterAdvice([PointcutMethod] MethodBase method)
        {
            AfterAdviceResult = $"Global After: {method.Name}";
        }
    }

    // Property Aspect
    [Aspect]
    public static class GlobalPropertyAspect
    {
        public static string PropertyGetterAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.PropertyTestClass::get_TestProperty$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PropertyGetterAdvice([PointcutMethod] MethodBase method)
        {
            PropertyGetterAdviceResult = $"Global Property Getter: {method.Name}";
        }

        public static string PropertySetterAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.PropertyTestClass::set_TestProperty$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PropertySetterAdvice([PointcutMethod] MethodBase method)
        {
            PropertySetterAdviceResult = $"Global Property Setter: {method.Name}";
        }

        public static string IndexerGetterAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.PropertyTestClass::get_Item$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IndexerGetterAdvice([PointcutMethod] MethodBase method)
        {
            IndexerGetterAdviceResult = $"Global Indexer Getter: {method.Name}";
        }

        public static string IndexerSetterAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.PropertyTestClass::set_Item$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void IndexerSetterAdvice([PointcutMethod] MethodBase method)
        {
            IndexerSetterAdviceResult = $"Global Indexer Setter: {method.Name}";
        }
    }

    // Constructor Aspect
    [Aspect]
    public static class GlobalConstructorAspect
    {
        public static string ConstructorAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.ConstructorTestClass::\\.ctor$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ConstructorAdvice([PointcutMethod] MethodBase method)
        {
            ConstructorAdviceResult = $"Global Constructor: {method.Name}";
        }
    }

    // JoinPoint Properties Aspect
    [Aspect]
    public static class GlobalJoinPointAspect
    {
        public static string JoinPointPropertiesAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::JoinPointTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void JoinPointPropertiesAdvice([PointcutMethod] MethodBase method, [PointcutThis] object target, [PointcutParameters] ParameterArray args)
        {
            JoinPointPropertiesAdviceResult = $"Global JoinPoint: Method: {method.Name}, Target: {target?.GetType().Name}, Args: {args.Length}, Signature: {method.ToString()}";
        }
    }

    // Out Parameter Aspect
    [Aspect]
    public static class GlobalOutParameterAspect
    {
        public static string OutParameterAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::OutParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void OutParameterAdvice([PointcutMethod] MethodBase method, ref int value)
        {
            value = 999; // アスペクトでoutパラメーターを変更
            OutParameterAdviceResult = $"Global Out Parameter Modified: {method.Name}, Value: {value}";
        }
    }

    // Individual Pointcut Tests
    [Aspect]
    public static class GlobalPointcutThisAspect
    {
        public static string PointcutThisAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.PointcutTestClass::PointcutThisTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PointcutThisAdvice([PointcutThis] object instance)
        {
            PointcutThisAdviceResult = $"Global PointcutThis: {instance?.GetType().Name}";
        }
    }

    [Aspect]
    public static class GlobalPointcutReturnedAspect
    {
        public static string PointcutReturnedAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::PointcutReturnedTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PointcutReturnedAdvice([PointcutReturned] object result)
        {
            PointcutReturnedAdviceResult = $"Global PointcutReturned: {result}";
        }
    }

    [Aspect]
    public static class GlobalPointcutThrownAspect
    {
        public static string PointcutThrownAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::PointcutThrownTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PointcutThrownAdvice([PointcutThrown] Exception exception)
        {
            PointcutThrownAdviceResult = $"Global PointcutThrown: {exception.GetType().Name}";
        }
    }

    [Aspect]
    public static class GlobalPointcutParametersAspect
    {
        public static string PointcutParametersAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::PointcutParametersTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PointcutParametersAdvice([PointcutParameters] ParameterArray parameters)
        {
            PointcutParametersAdviceResult = $"Global PointcutParameters: Length: {parameters.Length}";
        }
    }

    [Aspect]
    public static class GlobalPointcutMethodAspect
    {
        public static string PointcutMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::PointcutMethodTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void PointcutMethodAdvice([PointcutMethod] MethodBase method)
        {
            PointcutMethodAdviceResult = $"Global PointcutMethod: {method.Name}";
        }
    }

    [Aspect]
    public static class GlobalPointcutNameFlagAspect
    {
        public static string AllFlagsAdviceResult = string.Empty;

        const string Pattern = 
            @"^Katuusagi\.AspectForUnity\.Tests.*Version=.*Culture=.*PublicKeyToken=.*" +
            @"\[assembly:.*Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub.*\]" +
            @"\[module:.*Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub.*\]" +
            @"\[declaring:.*Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestDeclaringType.*Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub.*\]" +
            @"\[return:.*Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub.*\]" +
            @"\[Katuusagi\.AspectForUnity\.OutputPointcutMethodName\(Katuusagi\.AspectForUnity\.PointcutNameFlag\.All\),Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTest\(""hoge"",22,Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestEnum\.ValueA,Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags\|Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags,\{Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags,12\},\{44,55,66\},Flags1=Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags,Flags2=12,Flags3=Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags\|Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.TestFlags\)\]" +
            @"public sealed override System\.Collections\.Generic\.List<System\.Int32\[\]>Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.PointcutNameFlagTestClass<\[Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub\]T>::AllFlagsTestMethod<\[Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub\]T2>\(\[Katuusagi\.AspectForUnity\.Tests\.GlobalAspectTest\.AllFlagsTestSub\]System\.String param1,\[System\.Runtime\.CompilerServices\.IsReadOnlyAttribute\]in T\[\] param2,T2 param3\)$";

        [Advice(JoinPoint.Before)]
        [RegexPointcut(Pattern, PointcutNameFlag.All)]
        public static void AllFlagsAdvice([PointcutMethod] MethodBase method)
        {
            AllFlagsAdviceResult = $"Global All Flags Matched: {method.Name}";
        }
    }

    // Parameter Binding Global Aspect
    [Aspect]
    public static class GlobalParameterBindingAspect
    {
        public static string ParameterBindingAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void ParameterBindingAdvice([PointcutMethod] MethodBase method, [PointcutParameters] ParameterArray parameters, int value, string text)
        {
            ParameterBindingAdviceResult = $"Global Parameter: {method.Name}, Params: {parameters.Length}, Value: {value}, Text: {text}";
        }
    }

    // Regex Pointcut Global Aspect - 各フラグを個別にテスト
    [Aspect]
    public static class GlobalRegexPointcutAspect
    {
        public static string RegexMethodNameAdviceResult = string.Empty;

        // MethodNameフラグ単体テスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName)]
        [RegexPointcut("^RegexMethodNameTest$", PointcutNameFlag.MethodName)]
        public static void RegexMethodNameAdvice([PointcutMethod] MethodBase method)
        {
            RegexMethodNameAdviceResult = $"Global Regex MethodName: {method.Name}";
        }

        public static string RegexClassNameAdviceResult = string.Empty;

        // DeclaringTypeNameフラグ単体テスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.TestClassForRegex::InnerMethodTest$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        [RegexPointcut("^TestClassForRegex$", PointcutNameFlag.DeclaringTypeName)]
        public static void RegexClassNameAdvice([PointcutMethod] MethodBase method)
        {
            RegexClassNameAdviceResult = $"Global Regex ClassName: {method.DeclaringType.Name}";
        }

        public static string RegexFullSignatureAdviceResult = string.Empty;

        // FullTypeName + MethodNameフラグテスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest\\.TestClassForRegex::InnerMethodTest2$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void RegexFullSignatureAdvice([PointcutMethod] MethodBase method)
        {
            RegexFullSignatureAdviceResult = $"Global Regex FullSignature: {method.DeclaringType.FullName}.{method.Name}";
        }

        public static string RegexReturnTypeAdviceResult = string.Empty;

        // ReturnTypeNameフラグ単体テスト（具体的なメソッドに限定）
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^System\\.String$", PointcutNameFlag.TypeFullName | PointcutNameFlag.ReturnTypeName)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ReturnStringTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void RegexReturnTypeAdvice([PointcutMethod] MethodBase method)
        {
            RegexReturnTypeAdviceResult = $"Global Regex ReturnType: {method.Name}";
        }

        public static string RegexParameterTypeAdviceResult = string.Empty;

        // ParameterTypeNameフラグ単体テスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::IntParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        [RegexPointcut("^\\(System\\.Int32\\)$", PointcutNameFlag.TypeFullName | PointcutNameFlag.ParameterTypeName)]
        public static void RegexParameterTypeAdvice([PointcutMethod] MethodBase method)
        {
            RegexParameterTypeAdviceResult = $"Global Regex ParameterType: {method.Name}";
        }

        public static string RegexParameterNameAdviceResult = string.Empty;

        // ParameterNameフラグ単体テスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ParameterNameTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        [RegexPointcut("^\\(uniqueParamName\\)$", PointcutNameFlag.TypeFullName | PointcutNameFlag.ParameterName)]
        public static void RegexParameterNameAdvice([PointcutMethod] MethodBase method)
        {
            RegexParameterNameAdviceResult = $"Global Regex ParameterName: {method.Name}";
        }

        public static string RegexStaticModifierAdviceResult = string.Empty;

        // StaticModifierフラグテスト
        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::StaticTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        [RegexPointcut("^static$", PointcutNameFlag.MethodStaticModifier)]
        public static void RegexStaticModifierAdvice([PointcutMethod] MethodBase method)
        {
            RegexStaticModifierAdviceResult = $"Global Regex Static: {method.Name}";
        }
    }

    // Generic Method Global Aspect
    [Aspect]
    public static class GlobalGenericAspect
    {
        public static string GenericMethodAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::GenericTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GenericMethodAdvice<T>([PointcutMethod] MethodBase method, T value)
        {
            GenericMethodAdviceResult = $"Global Generic: {method.Name}, Type: {typeof(T).Name}, Value: {value}";
        }

        public static string GenericParameterTypeAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::GenericParameterTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void GenericParameterTypeAdvice<[PointcutGenericBind(GenericBinding.ParameterType)] TSelf, [PointcutGenericBind(GenericBinding.ParameterType)] TResult, [PointcutGenericBind(GenericBinding.ParameterType)] T>([PointcutThis] TSelf self, [PointcutMethod] MethodBase method, [PointcutReturned] IEnumerable<TResult> result, IEnumerable<object> strs, T value)
        {
            GenericParameterTypeAdviceResult = $"Global Generic ParameterType: {method.Name}, Type: {typeof(T).Name}, ResultType: {typeof(TResult).Name}, SelfType: {typeof(TSelf).Name}, Value: {value}";
        }

        public static string GenericNoneConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.None\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericNoneConstraintsAdvice<T>([PointcutMethod] MethodBase method)
        {
            GenericNoneConstraintsAdviceResult = $"Global Generic None Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericNewConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.New\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericNewConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : new()
        {
            GenericNewConstraintsAdviceResult = $"Global Generic New Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericClassConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.Class\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericClassConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : class
        {
            GenericClassConstraintsAdviceResult = $"Global Generic Class Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericUnmanagedConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.Unmanaged\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericUnmanagedConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : unmanaged
        {
            GenericUnmanagedConstraintsAdviceResult = $"Global Generic Unmanaged Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericNotNullConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.NotNull\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericNotNullConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : notnull
        {
            GenericNotNullConstraintsAdviceResult = $"Global Generic NotNull Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericStructConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.Struct\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericStructConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : struct
        {
            GenericStructConstraintsAdviceResult = $"Global Generic Struct Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericInterfaceBaseConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.InterfaceBase\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericInterfaceBaseConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : ICustomAttributeProvider
        {
            GenericInterfaceBaseConstraintsAdviceResult = $"Global Generic InterfaceBase Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericClassBaseConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.ClassBase\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericClassBaseConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : MemberInfo
        {
            GenericClassBaseConstraintsAdviceResult = $"Global Generic ClassBase Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericEnumConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.Enum\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericEnumConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : Enum
        {
            GenericEnumConstraintsAdviceResult = $"Global Generic Enum Constraint: {method.Name}, Type: {typeof(T).Name}";
        }

        public static string GenericDelegateConstraintsAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut(@"Katuusagi\.AspectForUnity\.Tests\.AllowGenericConstraintTest\(Katuusagi\.AspectForUnity\.Tests\.GenericConstraintFlag\.Delegate\)", PointcutNameFlag.MethodAttribute | PointcutNameFlag.AttributeArguments | PointcutNameFlag.TypeFullName)]
        public static void GenericDelegateConstraintsAdvice<T>([PointcutMethod] MethodBase method)
            where T : Delegate
        {
            GenericDelegateConstraintsAdviceResult = $"Global Generic Delegate Constraint: {method.Name}, Type: {typeof(T).Name}";
        }
    }

    // Unsafe Injection Global Aspect
    [Aspect]
    public static class GlobalUnsafeInjectionAspect
    {
        public static string UnsafeReturnAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::UnsafeTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnsafeReturnAdvice([PointcutMethod] MethodBase method, [PointcutReturned] ref int result)
        {
            result = 999;
            UnsafeReturnAdviceResult = $"Global Unsafe: {method.Name}, Modified Result: {result}";
        }

        public static string UnsafeRefAdviceResult = string.Empty;

        [Advice(JoinPoint.Before, unsafeInjection: true)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::RefTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void UnsafeRefAdvice([PointcutMethod] MethodBase method, ref int value)
        {
            value = value * 2;
            UnsafeRefAdviceResult = $"Global Unsafe Ref: {method.Name}, Modified Value: {value}";
        }
    }

    // Multiple Advice Global Aspect
    [Aspect]
    public static class GlobalMultipleAdviceAspect
    {
        public static string FirstBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::MultipleTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void FirstBeforeAdvice([PointcutMethod] MethodBase method)
        {
            FirstBeforeAdviceResult = $"Global Multiple Before 1: {method.Name}";
        }

        public static string SecondBeforeAdviceResult = string.Empty;

        [Advice(JoinPoint.Before)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::MultipleTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void SecondBeforeAdvice([PointcutMethod] MethodBase method)
        {
            SecondBeforeAdviceResult = $"Global Multiple Before 2: {method.Name}";
        }

        public static string AfterReturningAdviceResult = string.Empty;

        [Advice(JoinPoint.AfterReturning)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::MultipleTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterReturningAdvice([PointcutMethod] MethodBase method)
        {
            AfterReturningAdviceResult = $"Global Multiple AfterReturning: {method.Name}";
        }

        public static string AfterAdviceResult = string.Empty;

        [Advice(JoinPoint.After)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::MultipleTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void AfterAdvice([PointcutMethod] MethodBase method)
        {
            AfterAdviceResult = $"Global Multiple After: {method.Name}";
        }
    }

    // Exception Handling Global Aspect
    [Aspect]
    public static class GlobalExceptionHandlingAspect
    {
        public static string HandleArgumentExceptionResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ExceptionTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void HandleArgumentException([PointcutMethod] MethodBase method, [PointcutThrown] ArgumentException exception)
        {
            HandleArgumentExceptionResult = $"Global ArgumentException: {method.Name}, Message: {exception.Message}";
        }

        public static string HandleInvalidOperationExceptionResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ExceptionTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void HandleInvalidOperationException([PointcutMethod] MethodBase method, [PointcutThrown] InvalidOperationException exception)
        {
            HandleInvalidOperationExceptionResult = $"Global InvalidOperationException: {method.Name}, Message: {exception.Message}";
        }

        public static string HandleGeneralExceptionResult = string.Empty;

        [Advice(JoinPoint.AfterThrowing)]
        [RegexPointcut("^Katuusagi\\.AspectForUnity\\.Tests\\.GlobalAspectTest::ExceptionTestMethod$", PointcutNameFlag.TypeFullName | PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
        public static void HandleGeneralException([PointcutMethod] MethodBase method, [PointcutThrown] Exception exception)
        {
            HandleGeneralExceptionResult = $"Global General Exception: {method.Name}, Type: {exception.GetType().Name}";
        }
    }
}