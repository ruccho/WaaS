﻿using System;

namespace WaaS.Runtime
{
    public readonly struct StackFrame : IDisposable, IEquatable<StackFrame>
    {
        internal readonly IStackFrameCore core;
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
    }
}