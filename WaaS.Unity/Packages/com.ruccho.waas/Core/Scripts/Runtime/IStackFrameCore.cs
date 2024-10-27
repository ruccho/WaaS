using System;

namespace WaaS.Runtime
{
    public interface IStackFrameCore : IVersionedDisposable<ushort>
    {
        int GetResultLength(ushort version);
        StackFrameState MoveNext(ushort version, Waker waker);
        void TakeResults(ushort version, Span<StackValueItem> dest);
    }
}