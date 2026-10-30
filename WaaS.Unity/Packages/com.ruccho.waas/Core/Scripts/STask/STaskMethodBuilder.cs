using System;
using System.Runtime.CompilerServices;

namespace STask
{
    public struct STaskMethodBuilder
    {
        public static STaskMethodBuilder Create()
        {
            return default;
        }

        // created on the first suspension or failure, so that methods completing synchronously never use it
        private STaskCompletionSource<byte> source;
        private bool hasSource;

        private void EnsureSource()
        {
            if (hasSource) return;
            source = STaskCompletionSource<byte>.Create();
            hasSource = true;
        }

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
            EnsureSource();
            source.SetException(exception);
        }

        public void SetResult()
        {
            if (hasSource) source.SetResult(0);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // the state machine is copied below, so the source must be created before it
            EnsureSource();
            awaiter.OnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // the state machine is copied below, so the source must be created before it
            EnsureSource();
            awaiter.UnsafeOnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public STaskVoid Task => hasSource ? new STaskVoid(source.TaskSource) : default;
    }

    public struct STaskMethodBuilder<T>
    {
        public static STaskMethodBuilder<T> Create()
        {
            return default;
        }

        // created on the first suspension or failure, so that methods completing synchronously never use it
        private STaskCompletionSource<T> source;
        private bool hasSource;
        private T result;

        private void EnsureSource()
        {
            if (hasSource) return;
            source = STaskCompletionSource<T>.Create();
            hasSource = true;
        }

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
            EnsureSource();
            source.SetException(exception);
        }

        public void SetResult(T result)
        {
            if (hasSource)
                source.SetResult(result);
            else
                this.result = result;
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // the state machine is copied below, so the source must be created before it
            EnsureSource();
            awaiter.OnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // the state machine is copied below, so the source must be created before it
            EnsureSource();
            awaiter.UnsafeOnCompleted(InstantContinuation<TStateMachine>.Get(in stateMachine,
                static (in TStateMachine stateMachine) => stateMachine.MoveNext()));
        }

        public STask<T> Task => hasSource ? new STask<T>(source.TaskSource) : new STask<T>(result);
    }
}