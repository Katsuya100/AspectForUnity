using Katuusagi.AspectForUnity.Tests.GlobalAssembly;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Katuusagi.AspectForUnity.Tests
{
    public class AsyncExecutionSequenceTest
    {
        [Test]
        public void AsyncPointcutParametersPreserveValuesBoxingTimingAndAdviceExceptions()
        {
            StateMachineParameterTestAspect.Reset();
            StateMachineParameterTestAspect.ThrowNext = true;

            var exception = Assert.Throws<InvalidOperationException>(() =>
                AsyncPointcutParameters(17, "first").GetAwaiter().GetResult());

            Assert.AreEqual("state machine advice failure", exception.Message);
            Assert.AreEqual(1, StateMachineParameterTestAspect.BeforeCount);
            Assert.AreEqual(17, StateMachineParameterTestAspect.LastValue);
            Assert.AreEqual("first", StateMachineParameterTestAspect.LastText);
            Assert.IsFalse(StateMachineParameterTestAspect.BodyHadStarted);
            Assert.AreEqual(0, StateMachineParameterTestAspect.AfterCount);

            var result = AsyncPointcutParameters(23, "second").GetAwaiter().GetResult();

            Assert.AreEqual(23, result);
            Assert.AreEqual(2, StateMachineParameterTestAspect.BeforeCount);
            Assert.AreEqual(23, StateMachineParameterTestAspect.LastValue);
            Assert.AreEqual("second", StateMachineParameterTestAspect.LastText);
            Assert.IsFalse(StateMachineParameterTestAspect.BodyHadStarted);
            Assert.IsTrue(StateMachineParameterTestAspect.BodyStarted);
            Assert.AreEqual(1, StateMachineParameterTestAspect.AfterCount);
            Assert.AreEqual(23, StateMachineParameterTestAspect.AfterValue);
            Assert.AreEqual("second", StateMachineParameterTestAspect.AfterText);
            Assert.IsTrue(StateMachineParameterTestAspect.AfterHadStarted);
        }

        [Test]
        public void IteratorPointcutParametersAreAvailableAtCompletion()
        {
            StateMachineParameterTestAspect.Reset();

            var enumerator = CoroutinePointcutParameters(31);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(31, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            Assert.AreEqual(1, StateMachineParameterTestAspect.IteratorAfterCount);
            Assert.AreEqual(31, StateMachineParameterTestAspect.IteratorAfterValue);
        }

        [Test]
        public void AsyncAdviceRunsOnceAcrossAwaitAndUsesLogicalResult()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var result = AsyncSuccess().GetAwaiter().GetResult();

            Assert.AreEqual(7, result);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.AsyncBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.AsyncAfterReturningCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.AsyncAfterCount);
            Assert.AreEqual(nameof(AsyncSuccess), AsyncExecutionSequenceTestAspect.LastMethodName);
            Assert.AreEqual(7, AsyncExecutionSequenceTestAspect.LastReturnedValue);
        }

        [Test]
        public void AsyncThrowingAdviceRunsAtLogicalFailure()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            Assert.Throws<InvalidOperationException>(() => AsyncFailure().GetAwaiter().GetResult());

            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.AsyncAfterThrowingCount);
            Assert.AreEqual(nameof(InvalidOperationException), AsyncExecutionSequenceTestAspect.LastThrownType);
        }

        [Test]
        public void CustomTaskLikeBuilderRunsAcrossAsyncExecution()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var result = CustomTaskLikeSuccess().GetAwaiter().GetResult();

            Assert.AreEqual(11, result);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomAfterReturningCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomAfterCount);
            Assert.AreEqual(11, AsyncExecutionSequenceTestAspect.CustomReturnedValue);
        }

        [Test]
        public void CustomTaskLikeVoidBuilderRunsAcrossAsyncExecution()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            CustomTaskLikeVoidSuccess().GetAwaiter().GetResult();

            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomVoidBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomVoidAfterCount);
        }

        [Test]
        public void CustomTaskLikeBuilderPropagatesException()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            Assert.Throws<InvalidOperationException>(() => CustomTaskLikeFailure().GetAwaiter().GetResult());

            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CustomAfterThrowingCount);
            Assert.AreEqual(nameof(InvalidOperationException), AsyncExecutionSequenceTestAspect.CustomThrownType);
        }

        [Test]
        public void TaskCompletionSourceSetResultIsNotAsyncCompletion()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var source = new TaskCompletionSource<int>();
            var result = TaskCompletionSourceSetResultBeforeBuilderCompletion(source).GetAwaiter().GetResult();

            Assert.AreEqual(7, result);
            Assert.AreEqual(3, source.Task.GetAwaiter().GetResult());
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.TaskSourceAfterReturningCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.TaskSourceAfterCount);
            Assert.AreEqual(7, AsyncExecutionSequenceTestAspect.TaskSourceReturnedValue);
        }

        [Test]
        public void TaskCompletionSourceSetExceptionIsNotAsyncCompletion()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var source = new TaskCompletionSource<int>();
            TaskCompletionSourceSetExceptionBeforeBuilderCompletion(source).GetAwaiter().GetResult();

            Assert.AreEqual(0, AsyncExecutionSequenceTestAspect.TaskSourceAfterThrowingCount);
        }

        [Test]
        public void TaskCompletionSourceCanSetResult()
        {
            var source = new TaskCompletionSource<int>();
            source.SetResult(3);

            Assert.AreEqual(3, source.Task.GetAwaiter().GetResult());
        }

        [Test]
        public void UnityAwaitableBuilderRunsAcrossAsyncExecution()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var source = new AwaitableCompletionSource<int>();
            var awaitable = UnityAwaitableSuccess(source.Awaitable);
            var value = 17;
            source.SetResult(value);

            Assert.AreEqual(18, awaitable.GetAwaiter().GetResult());
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableAfterReturningCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableAfterCount);
            Assert.AreEqual(18, AsyncExecutionSequenceTestAspect.UnityAwaitableReturnedValue);
        }

        [Test]
        public void UnityAwaitableWithoutAdviceCanComplete()
        {
            var source = new AwaitableCompletionSource<int>();
            var awaitable = UnityAwaitableRawSuccess(source.Awaitable);
            source.SetResult(17);

            Assert.AreEqual(17, awaitable.GetAwaiter().GetResult());
        }

        [Test]
        public void UnityAwaitableVoidBuilderRunsAcrossAsyncExecution()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var source = new AwaitableCompletionSource();
            var awaitable = UnityAwaitableVoid(source.Awaitable);
            source.SetResult();

            awaitable.GetAwaiter().GetResult();
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableVoidBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableVoidAfterCount);
        }

        [Test]
        public void UnityAwaitableBuilderPropagatesException()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var source = new AwaitableCompletionSource<int>();
            var awaitable = UnityAwaitableFailure(source.Awaitable);
            source.SetException(new InvalidOperationException());

            Assert.Throws<InvalidOperationException>(() => awaitable.GetAwaiter().GetResult());
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.UnityAwaitableAfterThrowingCount);
        }

        [Test]
        public void StructInstanceAspectRunsAcrossAsyncExecution()
        {
            AsyncStructInstanceAspect.Reset();

            var result = AsyncStructSuccess().GetAwaiter().GetResult();

            Assert.AreEqual(7, result);
            Assert.AreEqual(1, AsyncStructInstanceAspect.ConstructorCount);
            Assert.AreEqual(1, AsyncStructInstanceAspect.AfterCount);
            Assert.AreEqual(nameof(AsyncStructSuccess), AsyncStructInstanceAspect.LastMethodName);
            Assert.AreEqual(7, AsyncStructInstanceAspect.LastReturnedValue);
            Assert.AreNotEqual(0, AsyncStructInstanceAspect.ConstructorInstanceId);
            Assert.AreEqual(AsyncStructInstanceAspect.ConstructorInstanceId, AsyncStructInstanceAspect.AfterInstanceId);
        }

        [Test]
        public void CoroutineAdviceRunsOnceAcrossYieldAndAtCompletion()
        {
            AsyncExecutionSequenceTestAspect.Reset();

            var enumerator = CoroutineSequence();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());

            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CoroutineBeforeCount);
            Assert.AreEqual(1, AsyncExecutionSequenceTestAspect.CoroutineAfterCount);
        }

        public async Task<int> AsyncSuccess()
        {
            await Task.Delay(1).ConfigureAwait(false);
            await Task.Delay(1).ConfigureAwait(false);
            return 7;
        }

        public async Task<int> AsyncPointcutParameters(int value, string text)
        {
            await Task.Delay(1).ConfigureAwait(false);
            StateMachineParameterTestAspect.BodyStarted = true;
            return value;
        }

        public IEnumerator CoroutinePointcutParameters(int value)
        {
            yield return value;
        }

        public async Task AsyncFailure()
        {
            await Task.Delay(1).ConfigureAwait(false);
            throw new InvalidOperationException();
        }

        public async TestTask<int> CustomTaskLikeSuccess()
        {
            await Task.Delay(1).ConfigureAwait(false);
            return 11;
        }

        public async TestTask CustomTaskLikeVoidSuccess()
        {
            await Task.Delay(1).ConfigureAwait(false);
        }

        public async TestTask<int> CustomTaskLikeFailure()
        {
            await Task.Delay(1).ConfigureAwait(false);
            throw new InvalidOperationException();
        }

        public async Task<int> TaskCompletionSourceSetResultBeforeBuilderCompletion(TaskCompletionSource<int> source)
        {
            source.SetResult(3);
            await Task.Delay(1).ConfigureAwait(false);
            return 7;
        }

        public async Task TaskCompletionSourceSetExceptionBeforeBuilderCompletion(TaskCompletionSource<int> source)
        {
            source.SetException(new InvalidOperationException("unused"));
            _ = source.Task.Exception;
            await Task.Delay(1).ConfigureAwait(false);
        }

        public async Awaitable<int> UnityAwaitableSuccess(Awaitable<int> source)
        {
            var value = await source;
            return value + 1;
        }

        public async Awaitable<int> UnityAwaitableRawSuccess(Awaitable<int> source)
        {
            return await source;
        }

        public async Awaitable UnityAwaitableVoid(Awaitable source)
        {
            await source;
        }

        public async Awaitable<int> UnityAwaitableFailure(Awaitable<int> source)
        {
            await source;
            throw new InvalidOperationException();
        }

        public async Task<int> AsyncStructSuccess()
        {
            await Task.Delay(1).ConfigureAwait(false);
            return 7;
        }

        public IEnumerator CoroutineSequence()
        {
            yield return null;
            yield return null;
        }
    }
}
