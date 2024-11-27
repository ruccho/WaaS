using System;
using System.Collections.Generic;
using System.Threading;

namespace STask
{
    internal interface ISTaskSource<out TResult>
    {
        bool IsCompleted { get; }
        void OnCompleted(ushort version, Action<object> continuation, object context);
        TResult GetResult(ushort version);
    }

    internal readonly struct STaskSource<TResult>
    {
        internal readonly ISTaskSource<TResult> core;
        private readonly ushort version;

        public void OnCompleted(Action<object> continuation, object context)
        {
            core.OnCompleted(version, continuation, context);
        }

        public TResult GetResult()
        {
            return core.GetResult(version);
        }

        public bool IsCompleted => core.IsCompleted;


        public STaskSource(ISTaskSource<TResult> core, ushort version)
        {
            this.core = core;
            this.version = version;
        }
    }

    internal readonly struct STaskCompletionSource<TResult>
    {
        private readonly Core core;
        private readonly ushort version;

        public STaskSource<TResult> TaskSource => new(core, version);

        public void SetResult(TResult result)
        {
            core.SetResult(version, result);
        }

        public void SetException(Exception ex)
        {
            core.SetException(version, ex);
        }

        public static STaskCompletionSource<TResult> Create()
        {
            return new STaskCompletionSource<TResult>(Core.Get());
        }

        private STaskCompletionSource(Core core)
        {
            this.core = core;
            version = core.Version;
        }

        private class Core : ISTaskSource<TResult>
        {
            [ThreadStatic] private static Stack<Core> pool;

            private Action<object> continuation;
            private object continuationContext;
            private Exception exception;

            private bool isCompleted;
            private TResult result;

            private Thread thread;
            private ushort version;

            public ushort Version => version;
            public bool IsCompleted => isCompleted;

            public void OnCompleted(ushort version, Action<object> continuation, object context)
            {
                ThrowIfDifferentThread();
                ThrowIfOutdated(version);

                if (this.continuation != null) throw new InvalidOperationException("");

                if (isCompleted)
                {
                    continuation?.Invoke(context);
                }
                else
                {
                    this.continuation = continuation;
                    continuationContext = context;
                }
            }

            public TResult GetResult(ushort version)
            {
                ThrowIfDifferentThread();
                ThrowIfOutdated(version);

                if (!isCompleted) throw new InvalidOperationException();

                try
                {
                    if (exception != null)
                        throw exception;

                    return result;
                }
                finally
                {
                    thread = default;
                    isCompleted = default;
                    result = default;
                    exception = default;
                    continuation = default;
                    continuationContext = default;

                    if (++this.version != ushort.MaxValue)
                    {
                        pool ??= new Stack<Core>();
                        pool.Push(this);
                    }
                }
            }

            public static Core Get()
            {
                pool ??= new Stack<Core>();
                if (!pool.TryPop(out var pooled)) pooled = new Core();
                pooled.thread = Thread.CurrentThread;
                return pooled;
            }

            private void ThrowIfDifferentThread()
            {
                if (thread != Thread.CurrentThread)
                    throw new InvalidOperationException("STask can only be used on the thread that created it.");
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (this.version != version)
                    throw new InvalidOperationException();
            }

            public void SetResult(ushort version, TResult result)
            {
                ThrowIfDifferentThread();
                ThrowIfOutdated(version);

                this.result = result;
                isCompleted = true;

                if (continuation != null)
                {
                    var c = continuation;
                    continuation = default;
                    c?.Invoke(continuationContext);
                }
            }

            public void SetException(ushort version, Exception ex)
            {
                ThrowIfDifferentThread();
                ThrowIfOutdated(version);

                exception = ex;
                isCompleted = true;

                if (continuation != null)
                {
                    var c = continuation;
                    continuation = default;
                    c?.Invoke(continuationContext);
                }
            }
        }
    }
}