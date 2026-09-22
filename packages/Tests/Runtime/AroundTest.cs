using Katuusagi.AspectForUnity.Tests.GlobalAssembly;
using NUnit.Framework;
using System;

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

        public static T GenericIdentity<T>(T value)
        {
            return value;
        }

    }

}
