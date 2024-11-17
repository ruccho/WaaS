using System;

namespace WaaS.Runtime
{
    /// <summary>
    ///     A wrapper of IStackFrameCore.
    /// </summary>
    public readonly struct StackFrame : IDisposable, IEquatable<StackFrame>
    {
        private readonly IStackFrameCore core;
        private readonly ushort version;

        public StackFrame(IStackFrameCore core)
        {
            this.core = core;
            version = core.Version;
        }

        public void Dispose()
        {
            core?.Dispose(version);
        }

        public int ResultLength => core.GetResultLength(version);

        public StackFrameState MoveNext(Waker waker)
        {
            return core.MoveNext(version, waker);
        }

        public void TakeResults(Span<StackValueItem> dest)
        {
            core.TakeResults(version, dest);
        }

        public bool DoesTakeResults() => core.DoesTakeResults(version);
        public void PushResults(Span<StackValueItem> source) => core.PushResults(version, source);

        public bool Equals(StackFrame other)
        {
            return Equals(core, other.core) && version == other.version;
        }

        public override bool Equals(object obj)
        {
            return obj is StackFrame other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(core, version);
        }

        public static bool operator ==(StackFrame left, StackFrame right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StackFrame left, StackFrame right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return core.ToString();
        }
    }
}