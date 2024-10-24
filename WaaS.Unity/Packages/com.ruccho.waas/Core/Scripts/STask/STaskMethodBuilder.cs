using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
            awaiter.OnCompleted(StateMachineContinuation<TStateMachine>.Get(ref stateMachine));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(StateMachineContinuation<TStateMachine>.Get(ref stateMachine));
        }

        public STaskVoid Task => new(source);
    }

    public struct STaskMethodBuilder<T>
    {
        public static STaskMethodBuilder<T> Create()
        {
            return new STaskMethodBuilder<T>()
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
            awaiter.OnCompleted(StateMachineContinuation<TStateMachine>.Get(ref stateMachine));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(StateMachineContinuation<TStateMachine>.Get(ref stateMachine));
        }

        public STask<T> Task => new(source);
    }

    internal class StateMachineContinuation<TStateMachine> where TStateMachine : IAsyncStateMachine
    {
        [ThreadStatic] private static Stack<StateMachineContinuation<TStateMachine>> pool;
        private readonly Action continuation;

        private TStateMachine stateMachine;
#if DEBUG
        private Thread thread;
#endif

        private StateMachineContinuation()
        {
            continuation = Continue;
        }

        public static Action Get(ref TStateMachine stateMachine)
        {
            pool ??= new Stack<StateMachineContinuation<TStateMachine>>();
            if (!pool.TryPop(out var pooled)) pooled = new StateMachineContinuation<TStateMachine>();

            pooled.stateMachine = stateMachine;
#if DEBUG
            pooled.thread = Thread.CurrentThread;
#endif
            return pooled.continuation;
        }

        private void Continue()
        {
            try
            {
#if DEBUG
                if (Thread.CurrentThread != thread)
                {
                    // TODO: warn
                }
#endif
                stateMachine.MoveNext();
            }
            finally
            {
                stateMachine = default;
#if DEBUG
                thread = default;
#endif
                pool ??= new Stack<StateMachineContinuation<TStateMachine>>();
                pool.Push(this);
            }
        }
    }
}