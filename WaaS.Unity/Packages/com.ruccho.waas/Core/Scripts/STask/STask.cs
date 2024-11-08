using System.Runtime.CompilerServices;

namespace STask
{
    /// <summary>
    ///     Task-like without SynchronizationContext-awareness nor ThreadPool usage.
    ///     It is mainly for single-threaded use. 
    /// </summary>
    [AsyncMethodBuilder(typeof(STaskMethodBuilder))]
    public readonly struct STaskVoid
    {
        internal readonly STaskSource<byte> source;

        internal STaskVoid(STaskSource<byte> source)
        {
            this.source = source;
        }

        public STaskAwaiter GetAwaiter()
        {
            return new STaskAwaiter(source);
        }

        public void Forget()
        {
            var awaiter = GetAwaiter();
            if (awaiter.IsCompleted)
                awaiter.GetResult();
            else
                awaiter.OnCompleted(InstantContinuation<STaskAwaiter>.Get(awaiter,
                    static (in STaskAwaiter awaiter) => awaiter.GetResult()));
        }
    }

    /// <summary>
    ///     Task-like without SynchronizationContext-awareness nor ThreadPool usage.
    ///     It is mainly for single-threaded use. 
    /// </summary>
    [AsyncMethodBuilder(typeof(STaskMethodBuilder<>))]
    public readonly struct STask<TResult>
    {
        internal readonly STaskSource<TResult> source;

        internal STask(STaskSource<TResult> source)
        {
            this.source = source;
        }

        public STaskAwaiter<TResult> GetAwaiter()
        {
            return new STaskAwaiter<TResult>(source);
        }

        public void Forget()
        {
            var awaiter = GetAwaiter();
            if (awaiter.IsCompleted)
                awaiter.GetResult();
            else
                awaiter.OnCompleted(InstantContinuation<STaskAwaiter<TResult>>.Get(awaiter,
                    static (in STaskAwaiter<TResult> awaiter) => awaiter.GetResult()));
        }
    }
}