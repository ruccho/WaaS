using System;
using System.Threading.Tasks;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Waits for a host function called directly, which has not completed synchronously.
    /// </summary>
    internal sealed class DirectInvocationStackFrame : IStackFrameCore
    {
        private readonly int numResults;
        private readonly ValueTask<StackValueItem> task;
        private bool completed;
        private bool registered;
        private StackValueItem result;
        private bool suspended;
        private Waker waker;

        public DirectInvocationStackFrame(ValueTask<StackValueItem> task, int numResults)
        {
            this.task = task;
            this.numResults = numResults;
        }

        public ushort Version { get; private set; }

        public void Dispose(ushort version)
        {
            if (version == Version) Version++;
        }

        public int GetResultLength(ushort version)
        {
            ThrowIfOutdated(version);
            return numResults;
        }

        public StackFrameState MoveNext(ushort version, Waker waker)
        {
            ThrowIfOutdated(version);
            if (!registered)
            {
                registered = true;
                if (!task.IsCompleted)
                {
                    task.GetAwaiter().OnCompleted(OnCompleted);
                    if (!completed)
                    {
                        this.waker = waker;
                        suspended = true;
                        return StackFrameState.Pending;
                    }
                }
            }
            else if (!completed && !task.IsCompleted)
            {
                this.waker = waker;
                return StackFrameState.Pending;
            }

            result = task.GetAwaiter().GetResult();
            return StackFrameState.Completed;
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            if (numResults != 0) dest[0] = result;
        }

        public bool DoesTakeResults(ushort version)
        {
            return false;
        }

        public void PushResults(ushort version, Span<StackValueItem> source)
        {
            throw new InvalidOperationException();
        }

        private void OnCompleted()
        {
            completed = true;
            // it may be called synchronously while the continuation is registered
            if (suspended) waker.Wake();
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }
    }
}
