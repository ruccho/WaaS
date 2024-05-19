using System;

namespace WaaS.Runtime
{
    public abstract class StackFrame : IDisposable
    {
        public abstract int ResultLength { get; }

        public abstract void Dispose();
        public abstract StackFrameState MoveNext(Waker waker);
        public abstract void TakeResults(Span<StackValueItem> dest);
    }
}