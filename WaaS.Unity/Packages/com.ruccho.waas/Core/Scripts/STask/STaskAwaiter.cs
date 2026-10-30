using System;
using System.Runtime.CompilerServices;

namespace STask
{
    public readonly struct STaskAwaiter : ICriticalNotifyCompletion
    {
        internal readonly STaskSource<byte> source;

        internal STaskAwaiter(STaskSource<byte> source)
        {
            this.source = source;
        }

        public bool IsCompleted => source.core == null || source.IsCompleted;

        public void GetResult()
        {
            if (source.core != null) source.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            if (source.core == null)
                continuation();
            else
                source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }

    public readonly struct STaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        internal readonly STaskSource<TResult> source;
        private readonly TResult result;

        internal STaskAwaiter(STaskSource<TResult> source, TResult result)
        {
            this.source = source;
            this.result = result;
        }

        public bool IsCompleted => source.core == null || source.IsCompleted;

        public TResult GetResult()
        {
            return source.core == null ? result : source.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            if (source.core == null)
                continuation();
            else
                source.OnCompleted(static ctx => ((Action)ctx).Invoke(), continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }
}