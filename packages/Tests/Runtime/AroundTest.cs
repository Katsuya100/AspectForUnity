using Katuusagi.AspectForUnity.Tests.GlobalAssembly;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Katuusagi.AspectForUnity.Tests
{
    public class AroundTest
    {
        [Test]
        public void BasicAroundOrder()
        {
            AroundTestAspects.Events.Clear();
            Assert.AreEqual(5, Add(4));
            CollectionAssert.AreEqual(new[]
            {
                "before:4",
                "outer-before:4",
                "inner-before:4",
                "inner-result:5",
                "inner-after",
                "outer-result:5",
                "outer-after",
                "after-returning:5",
                "after",
            }, AroundTestAspects.Events);
        }

        public static int Add(int value)
        {
            return value + 1;
        }

        [Test]
        public void VoidReturn()
        {
            AroundTestAspects.Events.Clear();
            Touch(7);
            CollectionAssert.AreEqual(new[] { "void-before:7", "void-target", "void-after" }, AroundTestAspects.Events);
        }

        [Test]
        public void ReferenceReturnAndReadOnlyReturnValue()
        {
            AroundTestAspects.Events.Clear();
            Assert.AreEqual("value!", Echo("value"));
            CollectionAssert.AreEqual(new[] { "reference-result:value!" }, AroundTestAspects.Events);
        }

        [Test]
        public void ReturnValueMutationIsNotExposedBySafeContexts()
        {
            Assert.IsNull(typeof(ProceedingContext<int>).GetMethod("GetReturnValueReference"));
            Assert.IsNull(typeof(UnsafeProceedingContext<int>).GetMethod("GetReturnValueReference"));
            Assert.IsNull(typeof(ProceedingContext<int>).GetProperty(nameof(ProceedingContext<int>.ReturnValue)).SetMethod);
        }

        [Test]
        public void UnsafeInjectionCanRewriteReturnValue()
        {
            Assert.AreEqual(12, UnsafeAdd(5));
        }

        [Test]
        public void RefReturnPreservesReferenceAndReturnValue()
        {
            AroundTestAspects.Events.Clear();
            ref var result = ref RefTarget(11);
            Assert.AreEqual(11, result);
            CollectionAssert.AreEqual(new[] { "ref-result:11" }, AroundTestAspects.Events);
            result = 12;
            Assert.AreEqual(12, RefCell);
        }

        [Test]
        public void MissingProceedIsDetected()
        {
            Assert.Throws<InvalidOperationException>(() => NoProceed(1));
        }

        [Test]
        public void DuplicateProceedIsDetected()
        {
            Assert.Throws<InvalidOperationException>(() => DuplicateProceed(1));
        }

        [Test]
        public void FailedProceedContextCannotBeReused()
        {
            AroundTestAspects.Events.Clear();
            Assert.Throws<InvalidOperationException>(() => ThrowingTarget());
            CollectionAssert.AreEqual(new[] { "proceed-failed", "proceed-reuse-failed" }, AroundTestAspects.Events);
        }

        [Test]
        public void ExceptionStackTraceIsPreserved()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => StackTraceTarget());

            Assert.AreEqual("stack-trace-test", exception.Message);
            Assert.That(exception.StackTrace, Does.Contain(nameof(StackTraceMiddle)));
            Assert.That(exception.StackTrace, Does.Contain(nameof(StackTraceLeaf)));
        }

        [Test]
        public void InstanceAndPointcutMetadataAreAvailable()
        {
            AroundInstanceAspect.ConstructorCount = 0;
            AroundInstanceAspect.ConstructorMetadata = string.Empty;
            AroundTestAspects.Events.Clear();
            var target = new AroundInstanceTarget();
            Assert.AreEqual(6, target.Multiply(3));
            Assert.AreEqual(1, AroundInstanceAspect.ConstructorCount);
            Assert.AreEqual("Multiply:1", AroundInstanceAspect.ConstructorMetadata);
            CollectionAssert.AreEqual(new[] { "metadata:Multiply:AroundInstanceTarget:1:3" }, AroundTestAspects.Events);
        }

        [Test]
        public void StructInstanceAroundCopiesBackChangesAndReturnValue()
        {
            var target = new AroundStructTarget();

            Assert.AreEqual(4, target.AddAndReturn(4));
            Assert.AreEqual(4, target.Value);
        }

        [Test]
        public void StructInstanceAroundCopiesBackChangesOnExceptionAndPreservesStackTrace()
        {
            var target = new AroundStructTarget();

            var exception = Assert.Throws<InvalidOperationException>(() => target.MutateThenThrow());

            Assert.AreEqual("struct-target-test", exception.Message);
            Assert.AreEqual(1, target.Value);
            Assert.That(exception.StackTrace, Does.Contain(nameof(AroundStructTarget.MutateThenThrow)));
        }

        [Test]
        public void StructInstanceAroundCopiesBackRefOutArgumentsAndReturnValue()
        {
            var target = new AroundStructTarget();
            var value = 5;
            int result;

            var returned = target.MutateWithRefOut(ref value, out result);

            Assert.AreEqual(2, target.Value);
            Assert.AreEqual(8, value);
            Assert.AreEqual(10, result);
            Assert.AreEqual(10, returned);
        }

        [Test]
        public void StructInstanceAroundCopiesBackRefOutArgumentsAndTargetOnException()
        {
            var target = new AroundStructTarget();
            var value = 7;
            var result = -1;
            InvalidOperationException exception = null;

            try
            {
                target.MutateWithRefOutThenThrow(ref value, out result);
            }
            catch (InvalidOperationException caught)
            {
                exception = caught;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual("struct-ref-out-test", exception.Message);
            Assert.AreEqual(4, target.Value);
            Assert.AreEqual(12, value);
            Assert.AreEqual(16, result);
            Assert.That(exception.StackTrace, Does.Contain(nameof(AroundStructTarget.MutateWithRefOutThenThrow)));
        }

        [Test]
        public void GenericMethodAroundBindsReturnType()
        {
            AroundTestAspects.Events.Clear();
            Assert.AreEqual(9, GenericIdentity(9));
            Assert.AreEqual("abc", GenericIdentity("abc"));
            CollectionAssert.AreEqual(new[] { "generic-result:9", "generic-result:abc" }, AroundTestAspects.Events);
        }

        public static void Touch(int value)
        {
            AroundTestAspects.Events.Add("void-target");
        }

        public static string Echo(string value)
        {
            return value + "!";
        }

        public static int UnsafeAdd(int value)
        {
            return value + 2;
        }

        public static int RefCell;

        public static ref int RefTarget(int value)
        {
            RefCell = value;
            return ref RefCell;
        }

        public static int NoProceed(int value)
        {
            return value;
        }

        public static int DuplicateProceed(int value)
        {
            return value;
        }

        public static int ThrowingTarget()
        {
            throw new InvalidOperationException("target");
        }

        public static void StackTraceTarget()
        {
            StackTraceMiddle();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StackTraceMiddle()
        {
            StackTraceLeaf();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StackTraceLeaf()
        {
            throw new InvalidOperationException("stack-trace-test");
        }

        public static T GenericIdentity<T>(T value)
        {
            return value;
        }

    }

    public struct AroundStructTarget
    {
        public int Value;

        public int AddAndReturn(int amount)
        {
            Value += amount;
            return Value;
        }

        public int MutateThenThrow()
        {
            Value++;
            throw new InvalidOperationException("struct-target-test");
        }

        public int MutateWithRefOut(ref int value, out int result)
        {
            Value += 2;
            value += 3;
            result = Value + value;
            return result;
        }

        public int MutateWithRefOutThenThrow(ref int value, out int result)
        {
            Value += 4;
            value += 5;
            result = Value + value;
            throw new InvalidOperationException("struct-ref-out-test");
        }
    }

}
