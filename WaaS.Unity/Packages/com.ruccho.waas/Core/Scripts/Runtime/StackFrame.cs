using System;
using System.Runtime.CompilerServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     A wrapper of IStackFrameCore.
    /// </summary>
    public readonly struct StackFrame : IDisposable, IEquatable<StackFrame>
    {
        private readonly IStackFrameCore core;
        private readonly ushort version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackFrame(IStackFrameCore core)
        {
            this.core = core;
            version = core.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            core?.Dispose(version);
        }

        public int ResultLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => core.GetResultLength(version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackFrameState MoveNext(Waker waker)
        {
            return core.MoveNext(version, waker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TakeResults(Span<StackValueItem> dest)
        {
            core.TakeResults(version, dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoesTakeResults()
        {
            return core.DoesTakeResults(version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushResults(Span<StackValueItem> source)
        {
            core.PushResults(version, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StackFrame other)
        {
            return Equals(core, other.core) && version == other.version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is StackFrame other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(core, version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(StackFrame left, StackFrame right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StackFrame left, StackFrame right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return core.ToString();
        }
    }
}