using System.Runtime.CompilerServices;

namespace STask
{
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