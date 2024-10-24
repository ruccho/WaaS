using System;
using System.Runtime.CompilerServices;

namespace STask
{
    public readonly struct STaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly STaskCompletionSource<byte> source;

        internal STaskAwaiter(STaskCompletionSource<byte> source)
        {
            this.source = source;
        }

        public bool IsCompleted => source.IsCompleted;

        public void GetResult()
        {
            source.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }
    }

    public readonly struct STaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        private readonly STaskCompletionSource<TResult> source;

        internal STaskAwaiter(STaskCompletionSource<TResult> source)
        {
            this.source = source;
        }

        public bool IsCompleted => source.IsCompleted;

        public TResult GetResult()
        {
            return source.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }
    }
}