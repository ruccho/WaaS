using System;
using System.Runtime.CompilerServices;

namespace STask
{
    public struct STaskMethodBuilder
    {
        public static STaskMethodBuilder Create()
        {
            return new STaskMethodBuilder
            {
                source = STaskCompletionSource<byte>.Create()
            };
        }

        private STaskCompletionSource<byte> source;

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // nop
        }

        public void SetException(Exception exception)
        {
            source.SetException(exception);
        }

        public void SetResult()
        {
            source.SetResult(0);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public STaskVoid Task => new(source.TaskSource);
    }

    public struct STaskMethodBuilder<T>
    {
        public static STaskMethodBuilder<T> Create()
        {
            return new STaskMethodBuilder<T>
            {
                source = STaskCompletionSource<T>.Create()
            };
        }

        private STaskCompletionSource<T> source;

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // nop
        }

        public void SetException(Exception exception)
        {
            source.SetException(exception);
        }

        public void SetResult(T result)
        {
            source.SetResult(result);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public STask<T> Task => new(source.TaskSource);
    }
}