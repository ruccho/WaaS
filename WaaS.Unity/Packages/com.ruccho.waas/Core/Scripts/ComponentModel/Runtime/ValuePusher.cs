#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace WaaS.ComponentModel.Runtime
{
    public readonly struct ValuePusher : IDisposable
    {
        private readonly IValuePusherCore? core;
        private readonly ushort version;

        public bool IsDisposed => core!.Version != version;

        public ValuePusher(IValuePusherCore core)
        {
            this.core = core;
            version = core.Version;
        }

        public bool TryGetNextType(out IValueType? type)
        {
            ThrowIfDisposed();
            return core!.TryGetNextType(out type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (IsDisposed) throw new InvalidOperationException();
        }

        public void Dispose()
        {
            core?.Dispose(version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(bool value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(byte value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(sbyte value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ushort value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(short value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(uint value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(int value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ulong value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(long value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(float value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(double value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushChar32(uint value)
        {
            ThrowIfDisposed();
            core!.PushChar32(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ReadOnlySpan<char> value)
        {
            ThrowIfDisposed();
            core!.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValuePusher PushRecord()
        {
            ThrowIfDisposed();
            return core!.PushRecord();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValuePusher PushVariant(int caseIndex)
        {
            ThrowIfDisposed();
            return core!.PushVariant(caseIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValuePusher PushList(int length)
        {
            ThrowIfDisposed();
            return core!.PushList(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFlags(uint flagValue)
        {
            ThrowIfDisposed();
            core!.PushFlags(flagValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushOwned(Owned handle)
        {
            ThrowIfDisposed();
            core!.PushOwned(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushBorrowed(Borrowed handle)
        {
            ThrowIfDisposed();
            core!.PushBorrowed(handle);
        }
    }
}