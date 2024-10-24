using System.Runtime.CompilerServices;

namespace STask
{
    [AsyncMethodBuilder(typeof(STaskMethodBuilder))]
    public struct STaskVoid
    {
        internal readonly STaskCompletionSource<byte> source;

        internal STaskVoid(STaskCompletionSource<byte> source)
        {
            this.source = source;
        }

        public STaskAwaiter GetAwaiter()
        {
            return new STaskAwaiter(source);
        }
    }

    [AsyncMethodBuilder(typeof(STaskMethodBuilder<>))]
    public struct STask<TResult>
    {
        internal readonly STaskCompletionSource<TResult> source;

        internal STask(STaskCompletionSource<TResult> source)
        {
            this.source = source;
        }

        public STaskAwaiter<TResult> GetAwaiter()
        {
            return new STaskAwaiter<TResult>(source);
        }
    }
}