using Katuusagi.AspectForUnity.Tests.GlobalAssembly;
using NUnit.Framework;
using System;

namespace Katuusagi.AspectForUnity.Tests
{
    public class AdviceControlFlowTest
    {
        [TestCase(-1, -1)]
        [TestCase(0, 0)]
        [TestCase(2, 4)]
        public void MultipleReturnsPreserveResultAndAdviceOrder(int input, int expected)
        {
            AdviceControlFlowAspect.Reset();

            var result = MultiReturnMethod(input);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(1, AdviceControlFlowAspect.BeforeCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"before:{nameof(MultiReturnMethod)}:{nameof(AdviceControlFlowTest)}:1",
                    "body-finally",
                    $"after-returning:{nameof(MultiReturnMethod)}:{expected}",
                    $"after:{nameof(MultiReturnMethod)}:1",
                },
                AdviceControlFlowAspect.Events);
        }

        [TestCase(5, 6)]
        [TestCase(-1, 100)]
        public void ReturnsInsideTryAndCatchRunAfterOnce(int input, int expected)
        {
            AdviceControlFlowAspect.Reset();

            var result = CatchReturnMethod(input);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"before:{nameof(CatchReturnMethod)}:{nameof(AdviceControlFlowTest)}:1",
                    "catch-finally",
                    $"after-returning:{nameof(CatchReturnMethod)}:{expected}",
                    $"after:{nameof(CatchReturnMethod)}:1",
                },
                AdviceControlFlowAspect.Events);
        }

        [Test]
        public void ExceptionRunsAfterThrowingAndAfterButNotAfterReturning()
        {
            AdviceControlFlowAspect.Reset();

            var exception = Assert.Throws<InvalidOperationException>(() => ThrowingMethod(41));

            Assert.AreEqual("body exception", exception.Message);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"before:{nameof(ThrowingMethod)}:{nameof(AdviceControlFlowTest)}:1",
                    "throwing-finally",
                    $"after-throwing:{nameof(ThrowingMethod)}:{nameof(InvalidOperationException)}:1:41",
                    $"after:{nameof(ThrowingMethod)}:1",
                },
                AdviceControlFlowAspect.Events);
        }

        [Test]
        public void ExceptionFilterMissStillRunsOuterAdvice()
        {
            AdviceControlFlowAspect.Reset();

            var exception = Assert.Throws<ArgumentException>(() => CatchReturnMethod(-2));

            Assert.AreEqual("catch branch", exception.Message);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"before:{nameof(CatchReturnMethod)}:{nameof(AdviceControlFlowTest)}:1",
                    "catch-finally",
                    $"after-throwing:{nameof(CatchReturnMethod)}:{nameof(ArgumentException)}:1:-2",
                    $"after:{nameof(CatchReturnMethod)}:1",
                },
                AdviceControlFlowAspect.Events);
        }

        [Test]
        public void ExceptionFromAfterReturningStillRunsAfterAndEscapes()
        {
            AdviceControlFlowAspect.Reset();
            AdviceControlFlowAspect.ThrowFromAfterReturning = true;

            var exception = Assert.Throws<InvalidOperationException>(() => ThrowingAfterReturningMethod());

            Assert.AreEqual("after-returning advice failure", exception.Message);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(1, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    $"before:{nameof(ThrowingAfterReturningMethod)}:{nameof(AdviceControlFlowTest)}:0",
                    $"after-returning:{nameof(ThrowingAfterReturningMethod)}:7",
                    $"after:{nameof(ThrowingAfterReturningMethod)}:0",
                },
                AdviceControlFlowAspect.Events);
        }

        [Test]
        public void UnmatchedMethodIsNotWrapped()
        {
            AdviceControlFlowAspect.Reset();

            var result = UnmatchedMethod(3);

            Assert.AreEqual(6, result);
            Assert.AreEqual(0, AdviceControlFlowAspect.BeforeCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterReturningCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterThrowingCount);
            Assert.AreEqual(0, AdviceControlFlowAspect.AfterCount);
            CollectionAssert.AreEqual(new[] { "unmatched-body" }, AdviceControlFlowAspect.Events);
        }

        [Test]
        public void GenericMultipleReturnsPreserveClosedReturnValue()
        {
            GenericReturnAdviceAspect.Reset();

            var intResult = GenericReturnMethod(42, true);
            var stringResult = GenericReturnMethod("value", false);

            Assert.AreEqual(42, intResult);
            Assert.AreEqual(default(string), stringResult);
            CollectionAssert.AreEqual(new object[] { 42, null }, GenericReturnAdviceAspect.Results);
        }

        [Test]
        public void ConstructorPointcutThisSeesConstructedInstance()
        {
            ConstructorPointcutAspect.Reset();

            var instance = new ConstructorTestClass();

            Assert.IsNotNull(instance);
            Assert.AreEqual(nameof(ConstructorTestClass), ConstructorPointcutAspect.InstanceType);
            Assert.IsTrue(ConstructorPointcutAspect.BodyExecuted);
        }

        public int MultiReturnMethod(int value)
        {
            try
            {
                if (value < 0)
                {
                    return -1;
                }

                if (value == 0)
                {
                    return 0;
                }

                return value * 2;
            }
            finally
            {
                AdviceControlFlowAspect.Events.Add("body-finally");
            }
        }

        public int CatchReturnMethod(int value)
        {
            try
            {
                if (value < 0)
                {
                    throw new ArgumentException("catch branch");
                }

                return value + 1;
            }
            catch (ArgumentException) when (value == -1)
            {
                return 100;
            }
            finally
            {
                AdviceControlFlowAspect.Events.Add("catch-finally");
            }
        }

        public int ThrowingMethod(int value)
        {
            try
            {
                throw new InvalidOperationException("body exception");
            }
            finally
            {
                AdviceControlFlowAspect.Events.Add("throwing-finally");
            }
        }

        public int ThrowingAfterReturningMethod()
        {
            return 7;
        }

        public int UnmatchedMethod(int value)
        {
            AdviceControlFlowAspect.Events.Add("unmatched-body");
            return value * 2;
        }

        public T GenericReturnMethod<T>(T value, bool useValue)
        {
            if (useValue)
            {
                return value;
            }

            return default;
        }

        public class ConstructorTestClass
        {
            public ConstructorTestClass()
            {
                ConstructorPointcutAspect.BodyExecuted = true;
            }
        }
    }
}
