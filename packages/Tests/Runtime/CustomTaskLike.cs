using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Katuusagi.AspectForUnity.Tests
{
    [AsyncMethodBuilder(typeof(TestTaskMethodBuilder))]
    public readonly struct TestTask
    {
        private readonly Task _task;

        internal TestTask(Task task)
        {
            _task = task;
        }

        public TestTaskAwaiter GetAwaiter()
        {
            return new TestTaskAwaiter(_task.GetAwaiter());
        }
    }

    public readonly struct TestTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly TaskAwaiter _awaiter;

        public TestTaskAwaiter(TaskAwaiter awaiter)
        {
            _awaiter = awaiter;
        }

        public bool IsCompleted => _awaiter.IsCompleted;

        public void GetResult()
        {
            _awaiter.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            _awaiter.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _awaiter.UnsafeOnCompleted(continuation);
        }
    }

    public struct TestTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder _builder;

        public static TestTaskMethodBuilder Create()
        {
            return new TestTaskMethodBuilder
            {
                _builder = AsyncTaskMethodBuilder.Create(),
            };
        }

        public TestTask Task => new TestTask(_builder.Task);

        public void SetResult()
        {
            _builder.SetResult();
        }

        public void SetException(Exception exception)
        {
            _builder.SetException(exception);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _builder.SetStateMachine(stateMachine);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            _builder.Start(ref stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
                                                               ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
                                                                    ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }

    [AsyncMethodBuilder(typeof(TestTaskMethodBuilder<>))]
    public readonly struct TestTask<T>
    {
        private readonly Task<T> _task;

        internal TestTask(Task<T> task)
        {
            _task = task;
        }

        public TestTaskAwaiter<T> GetAwaiter()
        {
            return new TestTaskAwaiter<T>(_task.GetAwaiter());
        }
    }

    public readonly struct TestTaskAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly TaskAwaiter<T> _awaiter;

        public TestTaskAwaiter(TaskAwaiter<T> awaiter)
        {
            _awaiter = awaiter;
        }

        public bool IsCompleted => _awaiter.IsCompleted;

        public T GetResult()
        {
            return _awaiter.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            _awaiter.OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _awaiter.UnsafeOnCompleted(continuation);
        }
    }

    public struct TestTaskMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _builder;

        public static TestTaskMethodBuilder<T> Create()
        {
            return new TestTaskMethodBuilder<T>
            {
                _builder = AsyncTaskMethodBuilder<T>.Create(),
            };
        }

        public TestTask<T> Task => new TestTask<T>(_builder.Task);

        public void SetResult(T result)
        {
            _builder.SetResult(result);
        }

        public void SetException(Exception exception)
        {
            _builder.SetException(exception);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _builder.SetStateMachine(stateMachine);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            _builder.Start(ref stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
                                                               ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
                                                                    ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}
