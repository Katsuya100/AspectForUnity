using Katuusagi.AspectForUnity.Tests.OtherAssembly;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Katuusagi.AspectForUnity.Tests
{
    public class OtherAspectTest
    {
        #region Basic Tests

        [Test]
        public void BeforeAdvice()
        {
            OtherBeforeAspect.BeforeAdviceResult = string.Empty;

            // Act
            BeforeMethod();

            // Assert
            Assert.IsTrue(OtherBeforeAspect.BeforeAdviceResult.Contains($"Other Before: {nameof(BeforeMethod)}"));
        }

        #endregion

        #region After Advice Tests

        [Test]
        public void AfterReturningAdvice()
        {
            OtherAfterReturningAspect.AfterReturningResult = string.Empty;

            // Act
            var result = AfterReturningMethod("returned");

            // Assert
            Assert.IsTrue(OtherAfterReturningAspect.AfterReturningResult.Contains($"Other AfterReturning: {nameof(AfterReturningMethod)}"));
            Assert.IsTrue(OtherAfterReturningAspect.AfterReturningResult.Contains($"Result: {result}"));
        }

        [Test]
        public void AfterThrowingAdvice()
        {
            OtherAfterThrowingAspect.ThrowingAdviceResult = string.Empty;

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => AfterThrowingMethod());
            Assert.IsTrue(OtherAfterThrowingAspect.ThrowingAdviceResult.Contains($"AfterThrowing: {nameof(AfterThrowingMethod)}"));
            Assert.IsTrue(OtherAfterThrowingAspect.ThrowingAdviceResult.Contains($"Exception: {nameof(NotSupportedException)}"));
        }

        [Test]
        public void AfterAdvice()
        {
            OtherAfterAspect.AfterAdviceResult = string.Empty;

            // Act
            AfterMethod();

            // Assert
            Assert.IsTrue(OtherAfterAspect.AfterAdviceResult.Contains($"After: {nameof(AfterMethod)}"));
        }

        #endregion

        #region Event and Delegate Tests

        [Test]
        public void DelegateInvocation()
        {
            OtherDelegateAspect.DelegateAdviceResult = string.Empty;

            // Act
            var eventClass = new EventClass();
            eventClass.OnEvent("delegate");

            // Assert
            Assert.IsTrue(OtherDelegateAspect.DelegateAdviceResult.Contains($"Delegate: {nameof(EventClass.OnEvent)}"));

            OtherDelegateAspect.DelegateAdviceResult = string.Empty;

            // Act
            eventClass.InvokeDelegate("delegate");

            // Assert
            Assert.IsTrue(OtherDelegateAspect.DelegateAdviceResult.Contains($"Delegate: {nameof(EventClass.OnEvent)}"));
        }

        #endregion

        #region Global Signature Tests

        [Test]
        public void GlobalSignatureMatching()
        {
            OtherSignatureAspect.GlobalSignatureAdviceResult = string.Empty;

            // Act
            ParametersMethod(100, "global");

            // Assert
            Assert.IsTrue(OtherSignatureAspect.GlobalSignatureAdviceResult.Contains("Signature"));
            Assert.IsTrue(OtherSignatureAspect.GlobalSignatureAdviceResult.Contains(nameof(ParametersMethod)));
            Assert.IsTrue(OtherSignatureAspect.GlobalSignatureAdviceResult.Contains("ParamCount: 2"));
            Assert.IsTrue(OtherSignatureAspect.GlobalSignatureAdviceResult.Contains("Katuusagi.AspectForUnity.Tests"));
        }

        [Test]
        public void ParameterHandling()
        {
            OtherSignatureAspect.ParameterHandlingAdviceResult = string.Empty;

            // Act
            ParameterHandlingMethod(42, "param", true);

            // Assert
            Assert.IsTrue(OtherSignatureAspect.ParameterHandlingAdviceResult.Contains($"Parameter Handling: {nameof(ParameterHandlingMethod)}"));
        }

        #endregion

        #region Scope Tests

        [Test]
        public void ClassScope()
        {
            OtherScopeAspect.AssemblyScopeAdviceResult = string.Empty;

            // Act
            ScopeTestMethod();

            // Assert
            Assert.IsTrue(OtherScopeAspect.AssemblyScopeAdviceResult.Contains($"Assembly Scope: {nameof(ScopeTestMethod)}"));
            Assert.IsTrue(OtherScopeAspect.AssemblyScopeAdviceResult.Contains("FromAssembly:"));
        }

        [Test]
        public void NestedClassScope()
        {
            OtherScopeAspect.NestedClassScopeAdviceResult = string.Empty;

            // Act
            var testClass = new NestedTestClass();
            testClass.ScopeTestMethod();

            // Assert
            Assert.IsTrue(OtherScopeAspect.NestedClassScopeAdviceResult.Contains(nameof(ScopeTestMethod)));
        }

        #endregion

        #region Generic Tests

        [Test]
        public void GenericBefore()
        {
            OtherGenericBeforeAspect.GenericBeforeAdviceResult = string.Empty;

            // Act
            GenericBeforeMethod(new List<string> { "test" });

            // Assert
            Assert.IsTrue(OtherGenericBeforeAspect.GenericBeforeAdviceResult.Contains($"Generic Before Method: {nameof(GenericBeforeMethod)}"));
            Assert.IsTrue(OtherGenericBeforeAspect.GenericBeforeAdviceResult.Contains($"Type: {typeof(List<string>).Name}"));
        }

        #endregion

        #region Complex Tests

        [Test]
        public void MultipleAspects()
        {
            OtherBeforeAspect.ComplexBeforeAdviceResult = string.Empty;
            OtherComplexAspect.ComplexMethodAdviceResult = string.Empty;

            // Act
            ComplexBeforeMethod("multi", 999);

            // Assert
            Assert.IsTrue(OtherBeforeAspect.ComplexBeforeAdviceResult.Contains(nameof(ComplexBeforeMethod)));
            Assert.IsTrue(OtherComplexAspect.ComplexMethodAdviceResult.Contains(nameof(ComplexBeforeMethod)));
        }

        [Test]
        public void MuiltipleGenericAspects()
        {
            OtherGenericBeforeAspect.ComplexGenericBeforeAdviceResult = string.Empty;
            OtherComplexAspect.ComplexGenericMethodAdviceResult = string.Empty;

            // Act
            ComplexGenericBeforeMethod(new Dictionary<string, int> { { "test", 42 } });

            // Assert
            Assert.IsTrue(OtherGenericBeforeAspect.ComplexGenericBeforeAdviceResult.Contains(nameof(ComplexGenericBeforeMethod)));
            Assert.IsTrue(OtherComplexAspect.ComplexGenericMethodAdviceResult.Contains(nameof(ComplexGenericBeforeMethod)));
        }

        [Test]
        public void MultipleExceptions()
        {
            // ArgumentException
            OtherComplexAspect.ArgumentExceptionAdviceResult = string.Empty;
            OtherComplexAspect.InvalidOperationExceptionAdviceResult = string.Empty;
            Assert.Throws<ArgumentException>(() => MultipleThrowingMethod(true));
            Assert.IsTrue(OtherComplexAspect.ArgumentExceptionAdviceResult.Contains($"ArgumentException: {nameof(MultipleThrowingMethod)}"));
            Assert.IsTrue(string.IsNullOrEmpty(OtherComplexAspect.InvalidOperationExceptionAdviceResult));

            // InvalidOperationException
            OtherComplexAspect.ArgumentExceptionAdviceResult = string.Empty;
            OtherComplexAspect.InvalidOperationExceptionAdviceResult = string.Empty;
            Assert.Throws<InvalidOperationException>(() => MultipleThrowingMethod(false));
            Assert.IsTrue(string.IsNullOrEmpty(OtherComplexAspect.ArgumentExceptionAdviceResult));
            Assert.IsTrue(OtherComplexAspect.InvalidOperationExceptionAdviceResult.Contains($"InvalidOperationException: {nameof(MultipleThrowingMethod)}"));
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

        public void ParametersMethod(int number, string text)
        {
            // Method body
        }

        public void ScopeTestMethod()
        {
            // Method body
        }

        public string ComplexBeforeMethod(string input, int multiplier)
        {
            return input + multiplier;
        }

        public void ParameterHandlingMethod(int number, string text, bool flag)
        {
            // Method body
        }

        public void GenericBeforeMethod<T>(T value)
        {
            // Method body
        }

        public void ComplexGenericBeforeMethod<T>(T value) where T : class
        {
            // Method body
        }

        // New test methods for additional coverage
        public void AfterThrowingMethod()
        {
            throw new NotSupportedException("exception test");
        }

        public void AfterMethod()
        {
            // Method body for After advice test
        }

        public void MultipleThrowingMethod(bool isArgument)
        {
            if (isArgument)
            {
                throw new ArgumentException("ArgumentException test");
            }
            else
            {
                throw new InvalidOperationException("InvalidOperationException test");
            }
        }

        #endregion

        #region Test Helper Classes

        public class NestedTestClass
        {
            public void ScopeTestMethod()
            {
                // Method body
            }
        }

        public class EventClass
        {
            public void InvokeDelegate(string message)
            {
                Action<string> del = OnEvent;
                del?.Invoke(message);
            }

            public void OnEvent(string message)
            {
                // Event handler body
            }
        }

        #endregion
    }
}