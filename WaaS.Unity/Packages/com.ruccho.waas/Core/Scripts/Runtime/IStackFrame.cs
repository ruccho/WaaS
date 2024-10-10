using System;

namespace WaaS.Runtime
{
    public interface IStackFrame : IDisposable
    {
        int ResultLength { get; }
        StackFrameState MoveNext(Waker waker);
        void TakeResults(Span<StackValueItem> dest);
    }
}