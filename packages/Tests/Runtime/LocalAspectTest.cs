using Katuusagi.AspectForUnity.Tests.LocalAssembly;
using NUnit.Framework;
using System;

namespace Katuusagi.AspectForUnity.Tests
{
    public class LocalAspectTest
    {
        #region Before Tests

        [Test]
        public void BeforeAdvice()
        {
            LocalBeforeAspect.BeforeAdviceResult = string.Empty;

            // Act
            BeforeMethod();

            // Assert
            Assert.IsTrue(LocalBeforeAspect.BeforeAdviceResult.Contains($"Local Before: {nameof(BeforeMethod)}"));
        }

        #endregion

        #region AfterReturning Tests

        [Test]
        public void AfterReturningAdvice()
        {
            LocalAfterReturningAspect.AfterReturningAdviceResult = string.Empty;

            // Act
            var result = AfterReturningMethod("test");

            // Assert
            Assert.IsTrue(LocalAfterReturningAspect.AfterReturningAdviceResult.Contains($"Local AfterReturning: {nameof(AfterReturningMethod)}"));
            Assert.IsTrue(LocalAfterReturningAspect.AfterReturningAdviceResult.Contains($"Result: {result}"));
        }

        #endregion

        #region AfterThrowing Tests

        [Test]
        public void AfterThrowingAdvice()
        {
            LocalAfterThrowingAspect.AfterThrowingAdviceResult = string.Empty;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => AfterThrowingMethod());
            Assert.IsTrue(LocalAfterThrowingAspect.AfterThrowingAdviceResult.Contains($"Local AfterThrowing: {nameof(AfterThrowingMethod)}"));
            Assert.IsTrue(LocalAfterThrowingAspect.AfterThrowingAdviceResult.Contains(nameof(InvalidOperationException)));
        }

        #endregion

        #region After Tests

        [Test]
        public void AfterAdvice()
        {
            LocalAfterAspect.AfterAdviceResult = string.Empty;

            // Act
            AfterMethod();

            // Assert
            Assert.IsTrue(LocalAfterAspect.AfterAdviceResult.Contains($"Local After: {nameof(AfterMethod)}"));
        }

        #endregion

        #region Generic Before Tests

        [Test]
        public void StaticGenericMethodLocalAspect()
        {
            LocalGenericBeforeAspect.GenericBeforeAdviceResult = string.Empty;

            // Act
            StaticGenericTestMethod("test");

            // Assert
            Assert.IsTrue(LocalGenericBeforeAspect.GenericBeforeAdviceResult.Contains($"Local Generic: {nameof(StaticGenericTestMethod)}"));
        }

        #endregion

        #region Private and Protected Method Tests

        [Test]
        public void PrivateMethodAspect()
        {
            LocalPrivateProtectedAspect.PrivateMethodAdviceResult = string.Empty;

            // Act
            var testClass = new PrivateProtectedMethodTestClass();
            testClass.CallPrivateMethod();

            // Assert
            Assert.IsTrue(LocalPrivateProtectedAspect.PrivateMethodAdviceResult.Contains("Private Method"));
            Assert.IsTrue(LocalPrivateProtectedAspect.PrivateMethodAdviceResult.Contains("PrivateTestMethod"));
        }

        [Test]
        public void ProtectedMethodAspect()
        {
            LocalPrivateProtectedAspect.ProtectedMethodAdviceResult = string.Empty;

            // Act
            var testClass = new DerivedPrivateProtectedTestClass();
            testClass.CallProtectedMethod();

            // Assert
            Assert.IsTrue(LocalPrivateProtectedAspect.ProtectedMethodAdviceResult.Contains("Protected Method"));
            Assert.IsTrue(LocalPrivateProtectedAspect.ProtectedMethodAdviceResult.Contains("ProtectedTestMethod"));
        }

        #endregion

        #region Operator Overload Tests

        [Test]
        public void OperatorAdditionAspect()
        {
            LocalOperatorAspect.OperatorAdditionAdviceResult = string.Empty;

            // Act
            var obj1 = new OperatorTestClass(10);
            var obj2 = new OperatorTestClass(20);
            var result = obj1 + obj2;

            // Assert
            Assert.AreEqual(30, result.Value);
            Assert.IsTrue(LocalOperatorAspect.OperatorAdditionAdviceResult.Contains("Operator Addition"));
            Assert.IsTrue(LocalOperatorAspect.OperatorAdditionAdviceResult.Contains("op_Addition"));
        }

        [Test]
        public void OperatorEqualityAspect()
        {
            LocalOperatorAspect.OperatorEqualityAdviceResult = string.Empty;

            // Act
            var obj1 = new OperatorTestClass(10);
            var obj2 = new OperatorTestClass(10);
            var isEqual = obj1 == obj2;

            // Assert
            Assert.IsTrue(isEqual);
            Assert.IsTrue(LocalOperatorAspect.OperatorEqualityAdviceResult.Contains("Operator Equality"));
            Assert.IsTrue(LocalOperatorAspect.OperatorEqualityAdviceResult.Contains("op_Equality"));
        }

        #endregion

        #region Parameter Aspect Tests

        [Test]
        public void ConditionalAspectTrueCondition()
        {
            LocalParameterAspect.ParameterAdviceResult = string.Empty;

            // Act
            ParameterTestMethod("hoge");

            // Assert
            Assert.IsTrue(LocalParameterAspect.ParameterAdviceResult.Contains("hoge"));

            LocalParameterAspect.ParameterAdviceResult = string.Empty;

            // Act
            ParameterTestMethod("fuga");

            // Assert
            Assert.IsTrue(LocalParameterAspect.ParameterAdviceResult.Contains("fuga"));

        }

        #endregion

        #region Instance Aspect Tests

        [Test]
        public void InstanceAspectConstructor()
        {
            LocalInstanceAspect.LocalConstructorResult = string.Empty;

            // Act
            InstanceTestMethod();

            // Assert
            Assert.IsTrue(LocalInstanceAspect.LocalConstructorResult.Contains($"Instance Constructor: {nameof(InstanceTestMethod)}"));
            Assert.IsTrue(LocalInstanceAspect.InstanceBeforeAdviceResult.Contains($"Instance Before: {nameof(InstanceTestMethod)}"));
        }

        [Test]
        public void StructInstanceAspectConstructor()
        {
            LocalStructInstanceAspect.Reset();

            // Act
            StructInstanceTestMethod();

            // Assert
            Assert.IsTrue(LocalStructInstanceAspect.LocalConstructorResult.Contains($"Struct Instance Constructor: {nameof(StructInstanceTestMethod)}"));
            Assert.IsTrue(LocalStructInstanceAspect.InstanceBeforeAdviceResult.Contains($"Struct Instance Before: {nameof(StructInstanceTestMethod)}"));
            Assert.AreNotEqual(0, LocalStructInstanceAspect.ConstructorInstanceId);
            Assert.AreEqual(LocalStructInstanceAspect.ConstructorInstanceId, LocalStructInstanceAspect.BeforeInstanceId);
        }

        #endregion

        #region Complex Parameter Binding Tests

        [Test]
        public void ComplexParameterBindingString()
        {
            LocalComplexParameterAspect.ComplexParameterAdviceResult = string.Empty;

            // Act
            ComplexParameterTestMethod<string>("hello", "world", 42);

            // Assert
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains(nameof(ComplexParameterTestMethod)));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains($"Instance: {nameof(LocalAspectTest)}"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains($"Generic: {nameof(String)}"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains("GenericValue: hello"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains("Text: world"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains("Number: 42"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains("ParamCount: 3"));
        }

        [Test]
        public void ComplexParameterBindingInt()
        {
            LocalComplexParameterAspect.ComplexParameterAdviceResult = string.Empty;

            // Act
            ComplexParameterTestMethod<int>(100, "test", 200);

            // Assert
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains(nameof(ComplexParameterTestMethod)));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains($"Generic: {nameof(Int32)}"));
            Assert.IsTrue(LocalComplexParameterAspect.ComplexParameterAdviceResult.Contains("GenericValue: 100"));
        }

        #endregion

        #region Pointcut Name Flag Tests

        [Test]
        public void DeclaringTypeNameFlag()
        {
            LocalPointcutNameFlagAspect.DeclaringTypeNameAdviceResult = string.Empty;

            // Act
            var testClass = new LocalTestClass();
            testClass.TestMethod();

            // Assert
            Assert.IsTrue(LocalPointcutNameFlagAspect.DeclaringTypeNameAdviceResult.Contains($"DeclaringTypeName: {nameof(LocalTestClass.TestMethod)}"));
        }

        [Test]
        public void ReturnTypeNameFlag()
        {
            LocalPointcutNameFlagAspect.ReturnTypeNameAdviceResult = string.Empty;

            // Act
            ReturnStringTestMethod();

            // Assert
            Assert.IsTrue(LocalPointcutNameFlagAspect.ReturnTypeNameAdviceResult.Contains($"ReturnTypeName: {nameof(ReturnStringTestMethod)}"));
        }

        [Test]
        public void ParameterNameFlag()
        {
            LocalPointcutNameFlagAspect.ParameterNameAdviceResult = string.Empty;

            // Act
            ParameterNameTestMethod("test");

            // Assert
            Assert.IsTrue(LocalPointcutNameFlagAspect.ParameterNameAdviceResult.Contains($"ParameterName: {nameof(ParameterNameTestMethod)}"));
        }

        #endregion

        #region Simple Pointcut Tests

        [Test]
        public void SimplePointcutsMatchAndRejectExpectedMethods()
        {
            var target = new SimplePointcutTarget();
            SimplePointcutAspect.Reset();
            var result = target.MatchingMethod<int, string>("text", 42);

            Assert.AreEqual(42, result);
            Assert.AreEqual(1, SimplePointcutAspect.AttributeTypeTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.AttributeTypeStringCount);
            Assert.AreEqual(0, SimplePointcutAspect.AttributeTypeMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.DeclaringAttributeTypeTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.DeclaringAttributeTypeStringCount);
            Assert.AreEqual(0, SimplePointcutAspect.DeclaringAttributeTypeMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.MethodNameCount);
            Assert.AreEqual(0, SimplePointcutAspect.MethodNameMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.DeclaringTypeTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.DeclaringTypeStringCount);
            Assert.AreEqual(0, SimplePointcutAspect.DeclaringTypeMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.ReturnTypeTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.ReturnTypeStringCount);
            Assert.AreEqual(0, SimplePointcutAspect.ReturnTypeMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameFirstCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameSecondCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameLastCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameSecondLastCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameRepeatedCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNamePositiveRepeatedCount);
            Assert.AreEqual(1, SimplePointcutAspect.GenericParameterNameAnyCount);
            Assert.AreEqual(0, SimplePointcutAspect.GenericParameterNameMismatchCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeStringCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeLastCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeSecondLastCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeRepeatedCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypePositiveRepeatedCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeAnyTypeCount);
            Assert.AreEqual(1, SimplePointcutAspect.ParameterTypeAnyStringCount);
            Assert.AreEqual(0, SimplePointcutAspect.ParameterTypeMismatchCount);
        }

        #endregion

        #region Test Methods

        public void BeforeMethod()
        {
            // Method body
        }

        public string AfterReturningMethod(string input)
        {
            return input;
        }

        public void AfterThrowingMethod()
        {
            throw new InvalidOperationException("Local test exception");
        }

        public void AfterMethod()
        {
            // Method body for After advice test
        }

        public static void StaticGenericTestMethod<T>(T value)
        {
            // Static generic method body
        }

        public void ParameterTestMethod(string parameter)
        {
            // Method body for conditional aspect test
        }

        public void InstanceTestMethod()
        {
            // Method body
        }

        public void StructInstanceTestMethod()
        {
            // Method body
        }

        public void ComplexParameterTestMethod<T>(T genericValue, string text, int number)
        {
            // Method body
        }

        public string ReturnStringTestMethod()
        {
            return "test";
        }

        public void ParameterNameTestMethod(string uniqueTextParam)
        {
            // Method body
        }

        #endregion

        #region Test Helper Classes

        public class LocalTestClass
        {
            public void TestMethod()
            {
                // Method body
            }
        }

        public class PrivateProtectedMethodTestClass
        {
            public void CallPrivateMethod()
            {
                PrivateTestMethod();
            }

            private void PrivateTestMethod()
            {
                // Private method body
            }

            protected virtual void ProtectedTestMethod()
            {
                // Protected method body
            }
        }

        public class DerivedPrivateProtectedTestClass : PrivateProtectedMethodTestClass
        {
            public void CallProtectedMethod()
            {
                ProtectedTestMethod();
            }
        }

        public class OperatorTestClass
        {
            public int Value { get; }

            public OperatorTestClass(int value)
            {
                Value = value;
            }

            public static OperatorTestClass operator +(OperatorTestClass a, OperatorTestClass b)
            {
                return new OperatorTestClass(a.Value + b.Value);
            }

            public static bool operator ==(OperatorTestClass a, OperatorTestClass b)
            {
                if (ReferenceEquals(a, b))
                    return true;
                if (a is null || b is null)
                    return false;
                return a.Value == b.Value;
            }

            public static bool operator !=(OperatorTestClass a, OperatorTestClass b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                return obj is OperatorTestClass other && Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }

        #endregion
    }
}
