using System;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a stack frame.
    /// </summary>
    public interface IStackFrameCore : IVersionedDisposable<ushort>
    {
        int GetResultLength(ushort version);
        StackFrameState MoveNext(ushort version, Waker waker);
        void TakeResults(ushort version, Span<StackValueItem> dest);

        bool DoesTakeResults(ushort version);
        void PushResults(ushort version, Span<StackValueItem> source);
    }
}