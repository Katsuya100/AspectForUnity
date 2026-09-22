using Katuusagi.AspectForUnity.Tests;
using Katuusagi.AspectForUnity.Tests.GlobalAssembly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

[assembly: GlobalAspectTest.AllFlagsTestSub]
[module: GlobalAspectTest.AllFlagsTestSub]
namespace Katuusagi.AspectForUnity.Tests
{
    [AllFlagsTestDeclaringType]
    public class GlobalAspectTest
    {
        #region Basic Advice Tests

        [Test]
        public void BeforeAdvice()
        {
            GlobalBeforeAspect.BeforeAdviceResult = string.Empty;
            // Act
            BeforeMethod("test");

            // Assert
            Assert.IsTrue(GlobalBeforeAspect.BeforeAdviceResult.Contains($"Before: {nameof(BeforeMethod)}"));
        }

        [Test]
        public void AfterReturningAdvice()
        {
            GlobalAfterReturningAspect.AfterReturningAdviceResult = string.Empty;

            // Act
            var result = AfterReturningMethod("test");

            // Assert
            Assert.IsTrue(GlobalAfterReturningAspect.AfterReturningAdviceResult.Contains($"AfterReturning: {nameof(AfterReturningMethod)}"));
            Assert.IsTrue(GlobalAfterReturningAspect.AfterReturningAdviceResult.Contains($"Result: {result}"));
        }

        [Test]
        public void AfterThrowingAdvice()
        {
            GlobalAfterThrowingAspect.AfterThrowingAdviceResult = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AfterThrowingMethod());
            Assert.IsTrue(GlobalAfterThrowingAspect.AfterThrowingAdviceResult.Contains($"AfterThrowing: {nameof(AfterThrowingMethod)}"));
            Assert.IsTrue(GlobalAfterThrowingAspect.AfterThrowingAdviceResult.Contains($"Exception: {nameof(ArgumentException)}"));
        }

        [Test]
        public void AfterAdviceExecution()
        {
            GlobalAfterAspect.AfterAdviceResult = string.Empty;

            // Act
            AfterMethod("test");

            // Assert
            Assert.IsTrue(GlobalAfterAspect.AfterAdviceResult.Contains($"After: {nameof(AfterMethod)}"));
        }

        #endregion

        #region Property and Indexer Tests

        [Test]
        public void PropertyGetterAspect()
        {
            GlobalPropertyAspect.PropertyGetterAdviceResult = string.Empty;

            // Act
            var testClass = new PropertyTestClass();
            var value = testClass.TestProperty;

            // Assert
            Assert.IsTrue(GlobalPropertyAspect.PropertyGetterAdviceResult.Contains("Property Getter"));
            Assert.IsTrue(GlobalPropertyAspect.PropertyGetterAdviceResult.Contains($"get_{nameof(PropertyTestClass.TestProperty)}"));
        }

        [Test]
        public void PropertySetterAspect()
        {
            GlobalPropertyAspect.PropertySetterAdviceResult = string.Empty;

            // Act
            var testClass = new PropertyTestClass();
            testClass.TestProperty = "test";

            // Assert
            Assert.IsTrue(GlobalPropertyAspect.PropertySetterAdviceResult.Contains("Property Setter"));
            Assert.IsTrue(GlobalPropertyAspect.PropertySetterAdviceResult.Contains($"set_{nameof(PropertyTestClass.TestProperty)}"));
        }

        [Test]
        public void IndexerAspect()
        {
            GlobalPropertyAspect.IndexerGetterAdviceResult = string.Empty;
            GlobalPropertyAspect.IndexerSetterAdviceResult = string.Empty;
            // Act
            var testClass = new PropertyTestClass();
            testClass[0] = "test";
            var value = testClass[0];

            // Assert
            Assert.IsTrue(GlobalPropertyAspect.IndexerGetterAdviceResult.Contains("Indexer Getter"));
            Assert.IsTrue(GlobalPropertyAspect.IndexerGetterAdviceResult.Contains("get_Item"));
            Assert.IsTrue(GlobalPropertyAspect.IndexerSetterAdviceResult.Contains("Indexer Setter"));
            Assert.IsTrue(GlobalPropertyAspect.IndexerSetterAdviceResult.Contains("set_Item"));
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void ConstructorAspect()
        {
            GlobalConstructorAspect.ConstructorAdviceResult = string.Empty;

            // Act
            var testClass = new ConstructorTestClass("test");

            // Assert
            Assert.IsTrue(GlobalConstructorAspect.ConstructorAdviceResult.Contains("Constructor"));
            Assert.IsTrue(GlobalConstructorAspect.ConstructorAdviceResult.Contains(".ctor"));
        }

        #endregion

        #region JoinPoint Properties Tests

        [Test]
        public void JoinPointPropertiesAccess()
        {
            GlobalJoinPointAspect.JoinPointPropertiesAdviceResult = string.Empty;

            // Act
            JoinPointTestMethod("test", 42);

            // Assert
            Assert.IsTrue(GlobalJoinPointAspect.JoinPointPropertiesAdviceResult.Contains($"Target: {GetType().Name}"));
            Assert.IsTrue(GlobalJoinPointAspect.JoinPointPropertiesAdviceResult.Contains("Args: 2"));
            Assert.IsTrue(GlobalJoinPointAspect.JoinPointPropertiesAdviceResult.Contains("Signature:"));
            Assert.IsTrue(GlobalJoinPointAspect.JoinPointPropertiesAdviceResult.Contains("JoinPointTestMethod"));
        }

        #endregion

        #region Out Parameter Tests

        [Test]
        public void OutParameterModification()
        {
            GlobalOutParameterAspect.OutParameterAdviceResult = string.Empty;
            // Act
            int outValue;
            var success = OutParameterTestMethod(out outValue);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(999, outValue); // アスペクトで変更された値
            Assert.IsTrue(GlobalOutParameterAspect.OutParameterAdviceResult.Contains("Out Parameter Modified"));
            Assert.IsTrue(GlobalOutParameterAspect.OutParameterAdviceResult.Contains($"Value: {outValue}"));
        }

        #endregion

        #region Individual Pointcut Tests

        [Test]
        public void PointcutThisIndividual()
        {
            GlobalPointcutThisAspect.PointcutThisAdviceResult = string.Empty;

            // Act
            var testClass = new PointcutTestClass();
            testClass.PointcutThisTestMethod();

            // Assert
            Assert.IsTrue(GlobalPointcutThisAspect.PointcutThisAdviceResult.Contains("PointcutThis"));
            Assert.IsTrue(GlobalPointcutThisAspect.PointcutThisAdviceResult.Contains(nameof(PointcutTestClass)));
        }

        [Test]
        public void PointcutReturnedIndividual()
        {
            GlobalPointcutReturnedAspect.PointcutReturnedAdviceResult = string.Empty;

            // Act
            var result = PointcutReturnedTestMethod("test");

            // Assert
            Assert.AreEqual("test", result);
            Assert.IsTrue(GlobalPointcutReturnedAspect.PointcutReturnedAdviceResult.Contains($"PointcutReturned: {result}"));
        }

        [Test]
        public void PointcutThrownIndividual()
        {
            GlobalPointcutThrownAspect.PointcutThrownAdviceResult = string.Empty;

            // Act & Assert
            Assert.Throws<CustomTestException>(() => PointcutThrownTestMethod());
            Assert.IsTrue(GlobalPointcutThrownAspect.PointcutThrownAdviceResult.Contains($"PointcutThrown: {nameof(CustomTestException)}"));
        }

        [Test]
        public void PointcutParametersIndividual()
        {
            GlobalPointcutParametersAspect.PointcutParametersAdviceResult = string.Empty;

            // Act
            PointcutParametersTestMethod(42, "test", true);

            // Assert
            Assert.IsTrue(GlobalPointcutParametersAspect.PointcutParametersAdviceResult.Contains("PointcutParameters"));
            Assert.IsTrue(GlobalPointcutParametersAspect.PointcutParametersAdviceResult.Contains("Length: 3"));
        }

        [Test]
        public void PointcutMethodIndividual()
        {
            GlobalPointcutMethodAspect.PointcutMethodAdviceResult = string.Empty;

            // Act
            PointcutMethodTestMethod();

            // Assert
            Assert.IsTrue(GlobalPointcutMethodAspect.PointcutMethodAdviceResult.Contains($"PointcutMethod: {nameof(PointcutMethodTestMethod)}"));
        }

        #endregion

        #region PointcutNameFlag Combination Tests

        [Test]
        public void PointcutNameFlagAllCombinations()
        {
            GlobalPointcutNameFlagAspect.AllFlagsAdviceResult = string.Empty;
            // Act
            var testClass = new PointcutNameFlagTestClass<int>();
            testClass.AllFlagsTestMethod<float>("param1", new[] { 42 }, 3.4f);

            // Assert
            Assert.IsTrue(GlobalPointcutNameFlagAspect.AllFlagsAdviceResult.Contains("All Flags Matched"));
        }

        #endregion

        #region Parameter Binding Tests

        [Test]
        public void ParameterBindingWithArguments()
        {
            GlobalParameterBindingAspect.ParameterBindingAdviceResult = string.Empty;

            // Act
            ParameterTestMethod(42, "hello");

            // Assert
            Assert.IsTrue(GlobalParameterBindingAspect.ParameterBindingAdviceResult.Contains("ParameterTestMethod"));
            Assert.IsTrue(GlobalParameterBindingAspect.ParameterBindingAdviceResult.Contains("Params: 2"));
            Assert.IsTrue(GlobalParameterBindingAspect.ParameterBindingAdviceResult.Contains("Value: 42"));
            Assert.IsTrue(GlobalParameterBindingAspect.ParameterBindingAdviceResult.Contains("Text: hello"));
        }

        #endregion

        #region Specific Regex Pointcut Tests

        [Test]
        public void RegexMethodNameFlag()
        {
            GlobalRegexPointcutAspect.RegexMethodNameAdviceResult = string.Empty;

            // Act
            RegexMethodNameTest();

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexMethodNameAdviceResult.Contains($"Regex MethodName: {nameof(RegexMethodNameTest)}"));
        }

        [Test]
        public void RegexDeclaringTypeNameFlag()
        {
            GlobalRegexPointcutAspect.RegexClassNameAdviceResult = string.Empty;

            // Act
            var testClass = new TestClassForRegex();
            testClass.InnerMethodTest();

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexClassNameAdviceResult.Contains($"Regex ClassName: {nameof(TestClassForRegex)}"));
        }

        [Test]
        public void RegexFullTypeNamePlusMethodName()
        {
            GlobalRegexPointcutAspect.RegexFullSignatureAdviceResult = string.Empty;

            // Act
            var testClass = new TestClassForRegex();
            testClass.InnerMethodTest2();

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexFullSignatureAdviceResult.Contains($"Regex FullSignature: {typeof(TestClassForRegex).FullName}"));
        }

        [Test]
        public void RegexReturnTypeNameFlag()
        {
            GlobalRegexPointcutAspect.RegexReturnTypeAdviceResult = string.Empty;
            // Act
            ReturnStringTestMethod();

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexReturnTypeAdviceResult.Contains($"Regex ReturnType: {nameof(ReturnStringTestMethod)}"));
        }

        [Test]
        public void RegexParameterTypeNameFlag()
        {
            GlobalRegexPointcutAspect.RegexParameterTypeAdviceResult = string.Empty;

            // Act
            IntParameterTestMethod(42);

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexParameterTypeAdviceResult.Contains($"Regex ParameterType: {nameof(IntParameterTestMethod)}"));
        }

        [Test]
        public void RegexParameterNameFlag()
        {
            GlobalRegexPointcutAspect.RegexParameterNameAdviceResult = string.Empty;

            // Act
            ParameterNameTestMethod(42);

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexParameterNameAdviceResult.Contains($"Regex ParameterName: {nameof(ParameterNameTestMethod)}"));
        }

        [Test]
        public void RegexStaticModifierFlag()
        {
            GlobalRegexPointcutAspect.RegexStaticModifierAdviceResult = string.Empty;

            // Act
            StaticTestMethod();

            // Assert
            Assert.IsTrue(GlobalRegexPointcutAspect.RegexStaticModifierAdviceResult.Contains($"Regex Static: {nameof(StaticTestMethod)}"));
        }

        #endregion

        #region Generic Method Tests

        [Test]
        public void GenericMethodString()
        {
            GlobalGenericAspect.GenericMethodAdviceResult = string.Empty;

            // Act
            GenericTestMethod<string, int>("hello");

            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains($"Generic: {nameof(GenericTestMethod)}"));
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains("Type: String"));
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains("Value: hello"));
        }

        [Test]
        public void GenericMethodInt()
        {
            GlobalGenericAspect.GenericMethodAdviceResult = string.Empty;

            // Act
            GenericTestMethod<int, int>(42);

            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains($"Generic: {nameof(GenericTestMethod)}"));
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains("Type: Int32"));
            Assert.IsTrue(GlobalGenericAspect.GenericMethodAdviceResult.Contains("Value: 42"));
        }

        [Test]
        public void GenericParameter()
        {
            GlobalGenericAspect.GenericParameterTypeAdviceResult = string.Empty;
            // Act
            GenericParameterTestMethod(10, new[] { "hoge" });
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericParameterTypeAdviceResult.Contains($"Generic ParameterType: {nameof(GenericParameterTestMethod)}"));
            Assert.IsTrue(GlobalGenericAspect.GenericParameterTypeAdviceResult.Contains("Type: Int32"));
            Assert.IsTrue(GlobalGenericAspect.GenericParameterTypeAdviceResult.Contains("ResultType: Int32"));
            Assert.IsTrue(GlobalGenericAspect.GenericParameterTypeAdviceResult.Contains($"SelfType: {nameof(GlobalAspectTest)}"));
            Assert.IsTrue(GlobalGenericAspect.GenericParameterTypeAdviceResult.Contains("Value: 10"));
        }

        [Test]
        public void GenericConstraintNone()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;

            // Act
            GenericNoneConstraintTestMethod<object>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericNoneConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintNew()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNewConstraintsAdviceResult = string.Empty;

            // Act
            GenericNewConstraintTestMethod<int>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericNewConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains("Global Generic New Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains(nameof(GenericNewConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintClass()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericClassConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;

            // Act
            GenericClassConstraintTestMethod<string>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericClassConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains("Global Generic Class Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains(nameof(GenericClassConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericClassConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintUnmanaged()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNewConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericUnmanagedConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericStructConstraintsAdviceResult = string.Empty;

            // Act
            GenericUnmanagedConstraintTestMethod<int>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericUnmanagedConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains("Global Generic New Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains(nameof(GenericUnmanagedConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericUnmanagedConstraintsAdviceResult.Contains("Global Generic Unmanaged Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericUnmanagedConstraintsAdviceResult.Contains(nameof(GenericUnmanagedConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericUnmanagedConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericStructConstraintsAdviceResult.Contains("Global Generic Struct Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericStructConstraintsAdviceResult.Contains(nameof(GenericUnmanagedConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintNotNull()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;

            // Act
            GenericNotNullConstraintTestMethod<string>();
            // Assert
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericNotNullConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericNotNullConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintStruct()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNewConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericStructConstraintsAdviceResult = string.Empty;
            // Act
            GenericStructConstraintTestMethod<int>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericStructConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains("Global Generic New Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNewConstraintsAdviceResult.Contains(nameof(GenericStructConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericStructConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericStructConstraintsAdviceResult.Contains("Global Generic Struct Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericStructConstraintsAdviceResult.Contains(nameof(GenericStructConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintInterfaceBase()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult = string.Empty;

            // Act
            GenericInterfaceBaseConstraintTestMethod1<PropertyInfo>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod1)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod1)));
            Assert.IsTrue(GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult.Contains("Global Generic InterfaceBase Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod1)));

            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult = string.Empty;
            // Act
            GenericInterfaceBaseConstraintTestMethod2<PropertyInfo>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod2)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod2)));
            Assert.IsTrue(GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult.Contains("Global Generic InterfaceBase Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericInterfaceBaseConstraintsAdviceResult.Contains(nameof(GenericInterfaceBaseConstraintTestMethod2)));
        }

        [Test]
        public void GenericConstraintClassBase()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericClassConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericClassBaseConstraintsAdviceResult = string.Empty;

            // Act
            GenericClassBaseConstraintTestMethod<PropertyInfo>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericClassBaseConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains("Global Generic Class Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains(nameof(GenericClassBaseConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericClassBaseConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericClassBaseConstraintsAdviceResult.Contains("Global Generic ClassBase Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericClassBaseConstraintsAdviceResult.Contains(nameof(GenericClassBaseConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintEnum()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericEnumConstraintsAdviceResult = string.Empty;

            // Act
            GenericEnumConstraintTestMethod<TestEnum>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericEnumConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericEnumConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericEnumConstraintsAdviceResult.Contains("Global Generic Enum Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericEnumConstraintsAdviceResult.Contains(nameof(GenericEnumConstraintTestMethod)));
        }

        [Test]
        public void GenericConstraintDelegate()
        {
            GlobalGenericAspect.GenericNoneConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericClassConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericNotNullConstraintsAdviceResult = string.Empty;
            GlobalGenericAspect.GenericDelegateConstraintsAdviceResult = string.Empty;

            // Act
            GenericDelegateConstraintTestMethod<Action>();
            // Assert
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains("Global Generic None Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNoneConstraintsAdviceResult.Contains(nameof(GenericDelegateConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains("Global Generic Class Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericClassConstraintsAdviceResult.Contains(nameof(GenericDelegateConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains("Global Generic NotNull Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericNotNullConstraintsAdviceResult.Contains(nameof(GenericDelegateConstraintTestMethod)));
            Assert.IsTrue(GlobalGenericAspect.GenericDelegateConstraintsAdviceResult.Contains("Global Generic Delegate Constraint"));
            Assert.IsTrue(GlobalGenericAspect.GenericDelegateConstraintsAdviceResult.Contains(nameof(GenericDelegateConstraintTestMethod)));
        }

        #endregion

        #region Unsafe Injection Tests

        [Test]
        public void UnsafeReturnModification()
        {
            GlobalUnsafeInjectionAspect.UnsafeReturnAdviceResult = string.Empty;
            // Act
            var result = UnsafeTestMethod();

            // Assert
            Assert.AreEqual(999, result); // Should be modified by aspect
            Assert.IsTrue(GlobalUnsafeInjectionAspect.UnsafeReturnAdviceResult.Contains($"Unsafe: {nameof(UnsafeTestMethod)}"));
            Assert.IsTrue(GlobalUnsafeInjectionAspect.UnsafeReturnAdviceResult.Contains($"Modified Result: {result}"));
        }

        [Test]
        public void UnsafeRefParameterModification()
        {
            GlobalUnsafeInjectionAspect.UnsafeRefAdviceResult = string.Empty;

            // Arrange
            int value = 10;

            // Act
            RefTestMethod(ref value);

            // Assert
            Assert.AreEqual(20, value); // Should be doubled by aspect
            Assert.IsTrue(GlobalUnsafeInjectionAspect.UnsafeRefAdviceResult.Contains($"Unsafe Ref: {nameof(RefTestMethod)}"));
            Assert.IsTrue(GlobalUnsafeInjectionAspect.UnsafeRefAdviceResult.Contains("Modified Value: 20"));
        }

        #endregion

        #region Multiple Advice Tests

        [Test]
        public void MultipleAdviceExecutionOrder()
        {
            GlobalMultipleAdviceAspect.FirstBeforeAdviceResult = string.Empty;
            GlobalMultipleAdviceAspect.SecondBeforeAdviceResult = string.Empty;
            GlobalMultipleAdviceAspect.AfterReturningAdviceResult = string.Empty;
            GlobalMultipleAdviceAspect.AfterAdviceResult = string.Empty;

            // Act
            MultipleTestMethod();

            // Assert
            Assert.IsTrue(GlobalMultipleAdviceAspect.FirstBeforeAdviceResult.Contains($"Multiple Before 1: {nameof(MultipleTestMethod)}"));
            Assert.IsTrue(GlobalMultipleAdviceAspect.SecondBeforeAdviceResult.Contains($"Multiple Before 2: {nameof(MultipleTestMethod)}"));
            Assert.IsTrue(GlobalMultipleAdviceAspect.AfterReturningAdviceResult.Contains($"Multiple AfterReturning: {nameof(MultipleTestMethod)}"));
            Assert.IsTrue(GlobalMultipleAdviceAspect.AfterAdviceResult.Contains($"Multiple After: {nameof(MultipleTestMethod)}"));
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public void SpecificExceptionHandlingArgumentException()
        {
            GlobalExceptionHandlingAspect.HandleArgumentExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleGeneralExceptionResult = string.Empty;
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ExceptionTestMethod(0));
            Assert.IsTrue(GlobalExceptionHandlingAspect.HandleArgumentExceptionResult.Contains($"ArgumentException: {nameof(ExceptionTestMethod)}"));
            Assert.IsTrue(string.IsNullOrEmpty(GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult));
            Assert.IsTrue(GlobalExceptionHandlingAspect.HandleGeneralExceptionResult.Contains($"General Exception: {nameof(ExceptionTestMethod)}"));


            GlobalExceptionHandlingAspect.HandleArgumentExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleGeneralExceptionResult = string.Empty;
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ExceptionTestMethod(1));
            Assert.IsTrue(string.IsNullOrEmpty(GlobalExceptionHandlingAspect.HandleArgumentExceptionResult));
            Assert.IsTrue(GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult.Contains($"InvalidOperationException: {nameof(ExceptionTestMethod)}"));
            Assert.IsTrue(GlobalExceptionHandlingAspect.HandleGeneralExceptionResult.Contains($"General Exception: {nameof(ExceptionTestMethod)}"));


            GlobalExceptionHandlingAspect.HandleArgumentExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult = string.Empty;
            GlobalExceptionHandlingAspect.HandleGeneralExceptionResult = string.Empty;
            // Act & Assert
            Assert.Throws<NotImplementedException>(() => ExceptionTestMethod(2));
            Assert.IsTrue(string.IsNullOrEmpty(GlobalExceptionHandlingAspect.HandleArgumentExceptionResult));
            Assert.IsTrue(string.IsNullOrEmpty(GlobalExceptionHandlingAspect.HandleInvalidOperationExceptionResult));
            Assert.IsTrue(GlobalExceptionHandlingAspect.HandleGeneralExceptionResult.Contains($"General Exception: {nameof(ExceptionTestMethod)}"));
        }

        #endregion

        #region Test Methods

        public void BeforeMethod(string input)
        {
        }

        public string AfterReturningMethod(string input)
        {
            return input;
        }

        public void AfterThrowingMethod()
        {
            throw new ArgumentException("Test exception");
        }

        public void AfterMethod(string input)
        {
        }

        // JoinPoint Test Methods
        public void JoinPointTestMethod(string param1, int param2)
        {
            // Method body
        }

        // Out Parameter Test Methods
        public bool OutParameterTestMethod(out int value)
        {
            value = 42; // Will be modified by aspect
            return true;
        }

        // Individual Pointcut Test Methods
        public string PointcutReturnedTestMethod(string input)
        {
            return input;
        }

        public void PointcutThrownTestMethod()
        {
            throw new CustomTestException("Test exception");
        }

        public void PointcutParametersTestMethod(int param1, string param2, bool param3)
        {
            // Method body
        }

        public void PointcutMethodTestMethod()
        {
            // Method body
        }

        public void ParameterTestMethod(int value, string text)
        {
            // Method body
        }

        public void RegexMethodNameTest()
        {
            // Method body
        }

        public string ReturnStringTestMethod()
        {
            return "test";
        }

        public void IntParameterTestMethod(int parameter)
        {
            // Method body
        }

        public void ParameterNameTestMethod(int uniqueParamName)
        {
            // Method body
        }

        public static void StaticTestMethod()
        {
            // Static method for testing StaticModifier flag
        }

        public void GenericTestMethod<T, TUnmanaged>(T value)
            where TUnmanaged : unmanaged
        {
            // Method body
        }

        public int[] GenericParameterTestMethod(int value, string[] strs)
        {
            // Method body
            return new int[] { 10 + value };
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        public void GenericNoneConstraintTestMethod<T>()
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.New)]
        public void GenericNewConstraintTestMethod<T>()
            where T : new()
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Class)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        public void GenericClassConstraintTestMethod<T>()
            where T : class
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.New)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Unmanaged)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Struct)]
        public void GenericUnmanagedConstraintTestMethod<T>()
            where T : unmanaged
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        public void GenericNotNullConstraintTestMethod<T>()
            where T : notnull
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.New)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Struct)]
        public void GenericStructConstraintTestMethod<T>()
            where T : struct
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.InterfaceBase)]
        public void GenericInterfaceBaseConstraintTestMethod1<T>()
            where T : ICustomAttributeProvider
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.InterfaceBase)]
        public void GenericInterfaceBaseConstraintTestMethod2<T>()
            where T : MemberInfo
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Class)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.ClassBase)]
        public void GenericClassBaseConstraintTestMethod<T>()
            where T : MemberInfo
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Enum)]
        public void GenericEnumConstraintTestMethod<T>()
            where T : Enum
        {
        }

        [AllowGenericConstraintTest(GenericConstraintFlag.None)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Class)]
        [AllowGenericConstraintTest(GenericConstraintFlag.NotNull)]
        [AllowGenericConstraintTest(GenericConstraintFlag.Delegate)]
        public void GenericDelegateConstraintTestMethod<T>()
            where T : Delegate
        {
        }

        public int UnsafeTestMethod()
        {
            return 42; // Will be modified by aspect to 999
        }

        public void RefTestMethod(ref int value)
        {
            // Value will be modified by aspect before this executes
        }

        public void MultipleTestMethod()
        {
            // Method body
        }

        public void ExceptionTestMethod(int n)
        {
            switch (n)
            {
                case 0:
                    throw new ArgumentException("Argument exception message");
                case 1:
                    throw new InvalidOperationException("Invalid operation exception message");
                case 2:
                    throw new NotImplementedException("Not implemented exception message");
            }
        }

        #endregion

        #region Test Helper Classes and Interfaces

        public class PropertyTestClass
        {
            private string testProperty;
            private Dictionary<int, string> items = new Dictionary<int, string>();

            public string TestProperty
            {
                get { return testProperty; }
                set { testProperty = value; }
            }

            public string this[int index]
            {
                get { return items.ContainsKey(index) ? items[index] : null; }
                set { items[index] = value; }
            }
        }

        public class ConstructorTestClass
        {
            public ConstructorTestClass(string parameter)
            {
                // Constructor body
            }
        }

        public class PointcutTestClass
        {
            public void PointcutThisTestMethod()
            {
                // Method body
            }
        }

        [AllFlagsTestSub]
        public class PointcutNameFlagTestClass<[AllFlagsTestSub] T> : PointcutNameFlagTestClassBase<T>
            where T : struct
        {
            [return: AllFlagsTestSub]
            [OutputPointcutMethodName(PointcutNameFlag.All)]
            [AllFlagsTest("hoge", 22, TestEnum.ValueA, TestFlags.FlagA | TestFlags.FlagB, new TestFlags[] { TestFlags.FlagA, TestFlags.FlagD | AllFlagsTest.FlagC }, 44, 55, 66, Flags1=TestFlags.FlagA, Flags2=TestFlags.FlagD|AllFlagsTest.FlagC, Flags3=TestFlags.FlagA|TestFlags.FlagD)]
            public sealed override List<int[]> AllFlagsTestMethod<[AllFlagsTestSub] T2>([AllFlagsTestSub] string param1, in T[] param2, T2 param3)
                where T2 : struct
            {
                // Method body
                return null;
            }
        }

        public abstract class PointcutNameFlagTestClassBase<T>
        {
            public abstract List<int[]> AllFlagsTestMethod<[AllFlagsTestSub] T2>([AllFlagsTestSub] string param1, in T[] param2, T2 param3)
                where T2 : struct;
        }

        public enum TestEnum
        {
            ValueA,
            ValueB,
        }

        [Flags]
        public enum TestFlags
        {
            FlagA = 1 << 0,
            FlagB = 1 << 1,
            FlagD = 1 << 3,

            Hoge = FlagB | FlagD,
        }

        public class AllFlagsTest : Attribute
        {
            public const TestFlags FlagC = (TestFlags)(1 << 2);
            public TestFlags Flags1;
            public TestFlags Flags2;
            public TestFlags Flags3;

            public AllFlagsTest(string stringValue, int intValue, TestEnum enumValue, TestFlags flagsValue, TestFlags[] flagsValues, params int[] parameters)
            {
                // Attribute constructor
            }
        }

        public class AllFlagsTestSub : Attribute
        {
        }

        public class AllFlagsTestDeclaringType : Attribute
        {
        }

        public class TestClassForRegex
        {
            public void InnerMethodTest()
            {
                // Method body
            }
            public void InnerMethodTest2()
            {
                // Method body
            }
        }

        public class CustomTestException : Exception
        {
            public CustomTestException(string message) : base(message) { }
        }

        #endregion
    }

    public enum GenericConstraintFlag
    {
        None,
        New,
        Class,
        Unmanaged,
        NotNull,
        Struct,
        InterfaceBase,
        ClassBase,
        Enum,
        Delegate,
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllowGenericConstraintTest : Attribute
    {
        public AllowGenericConstraintTest(GenericConstraintFlag flag)
        {
        }
    }
}