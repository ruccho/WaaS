﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     An adapter for pushing and pulling values.
    ///     Values pushed while no one is pulling are buffered, and pulled later without suspension.
    /// </summary>
    public class PushPullAdapter : IValuePusherCore, IPullableCore
    {
        private const int MaxRetainedChars = 4096;

        [ThreadStatic] private static Stack<PushPullAdapter>? pool;

        private Entry[] buffer = Array.Empty<Entry>();
        private int bufferCount;
        private int bufferHead;
        private char[] chars = Array.Empty<char>();
        private int charsLength;
        private bool disposeRequested;
        private bool isOwned;

        private IAwaiter? nextAwaiter;

        private PushPullAdapter()
        {
        }

        public STask<T> PullValueAsync<T>()
        {
            // the class constructor of T is run by FormatterProvider to register its formatter
            var formatter = FormatterProvider.GetFormatter<T>();
            return formatter.PullAsync(new Pullable(this));
        }

        public STask<T> PullPrimitiveValueAsync<T>()
        {
            if (bufferCount > 0) return new STask<T>(Dequeue<T>());

            if (nextAwaiter is Awaiter<T> preservedAwaiter)
            {
                nextAwaiter = null;
                return new STask<T>(preservedAwaiter.Core.TaskSource);
            }

            nextAwaiter?.Cancel();
            nextAwaiter = null;

            var awaiter = Awaiter<T>.Get();
            nextAwaiter = awaiter;
            return new STask<T>(awaiter.Core.TaskSource);
        }

        public ushort Version { get; private set; }

        public void Dispose(ushort version)
        {
            if (Version != version) return;

            if (bufferCount > 0)
            {
                // the buffered values can still be pulled. it is disposed when all of them are pulled.
                disposeRequested = true;
                return;
            }

            DisposeCore();
        }

        public bool TryGetNextType([NotNullWhen(true)] out IValueType? type)
        {
            type = default;
            return false;
        }

        public void Push(bool value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.Bool, value ? 1UL : 0UL);
            else
                PushCore(value);
        }

        public void Push(byte value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.U8, value);
            else
                PushCore(value);
        }

        public void Push(sbyte value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.S8, unchecked((ulong)value));
            else
                PushCore(value);
        }

        public void Push(ushort value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.U16, value);
            else
                PushCore(value);
        }

        public void Push(short value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.S16, unchecked((ulong)value));
            else
                PushCore(value);
        }

        public void Push(uint value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.U32, value);
            else
                PushCore(value);
        }

        public void Push(int value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.S32, unchecked((ulong)value));
            else
                PushCore(value);
        }

        public void Push(ulong value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.U64, value);
            else
                PushCore(value);
        }

        public void Push(long value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.S64, unchecked((ulong)value));
            else
                PushCore(value);
        }

        public void Push(float value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.F32, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
            else
                PushCore(value);
        }

        public void Push(double value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.F64, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
            else
                PushCore(value);
        }

        public void PushChar32(uint value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.Char32, value);
            else
                PushCore(value);
        }

        public void Push(ReadOnlySpan<char> value)
        {
            if (nextAwaiter != null)
            {
                PushCore(value.ToString());
                return;
            }

            // the characters are copied so that no string is allocated unless it is pulled as a string
            var start = charsLength;
            var length = value.Length;
            if (chars.Length < start + length)
                Array.Resize(ref chars, Math.Max(start + length, Math.Max(16, chars.Length * 2)));
            value.CopyTo(chars.AsSpan(start, length));
            charsLength += length;
            Enqueue(EntryKind.String, ((ulong)(uint)start << 32) | (uint)length);
        }

        /// <summary>
        ///     Pushes a string without copying it.
        /// </summary>
        internal void PushString(string value)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.StringObject, 0, value);
            else
                PushCore(value);
        }

        public ValuePusher PushRecord()
        {
            var p = Get();
            if (nextAwaiter == null)
                Enqueue(EntryKind.Record, p.Version, p);
            else
                PushCore(new RecordPrelude
                {
                    BodyPullable = new Pullable(p)
                });
            return new ValuePusher(p);
        }

        public ValuePusher PushVariant(int caseIndex)
        {
            var p = Get();
            if (nextAwaiter == null)
                Enqueue(EntryKind.Variant, ((ulong)(uint)caseIndex << 32) | p.Version, p);
            else
                PushCore(new VariantPrelude
                {
                    BodyPullable = new Pullable(p),
                    CaseIndex = caseIndex
                });
            return new ValuePusher(p);
        }

        public ValuePusher PushList(int length)
        {
            var p = Get();
            if (nextAwaiter == null)
                Enqueue(EntryKind.List, ((ulong)(uint)length << 32) | p.Version, p);
            else
                PushCore(new ListPrelude
                {
                    ElementPullable = new Pullable(p),
                    Length = length
                });
            return new ValuePusher(p);
        }

        public void PushFlags(uint flagValue)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.Flags, flagValue);
            else
                PushCore(flagValue);
        }

        public void PushOwned(Owned handle)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.Owned, unchecked((uint)handle.Version), handle.Ownership);
            else
                PushCore(handle);
        }

        public void PushBorrowed(Borrowed handle)
        {
            if (nextAwaiter == null)
                Enqueue(EntryKind.Borrowed, unchecked((uint)handle.Owned.Version), handle.Owned.Ownership);
            else
                PushCore(handle);
        }

        internal static PushPullAdapter Get()
        {
            if (!(pool ??= new Stack<PushPullAdapter>()).TryPop(out var pooled)) pooled = new PushPullAdapter();
            return pooled;
        }

        public static void Get(out Pullable pullable, out ValuePusher pusher)
        {
            var core = Get();
            pullable = new Pullable(core);
            pusher = new ValuePusher(core);
        }

        /// <summary>
        ///     Pushes the buffered values to another pusher in order, and removes them from the buffer.
        /// </summary>
        internal void TransferBufferedValues(ValuePusher pusher)
        {
            while (bufferCount > 0)
            {
                var entry = buffer[bufferHead];
                buffer[bufferHead] = default;
                bufferHead++;
                bufferCount--;

                switch (entry.Kind)
                {
                    case EntryKind.Bool:
                        pusher.Push(entry.Bits != 0);
                        break;
                    case EntryKind.U8:
                        pusher.Push(unchecked((byte)entry.Bits));
                        break;
                    case EntryKind.S8:
                        pusher.Push(unchecked((sbyte)entry.Bits));
                        break;
                    case EntryKind.U16:
                        pusher.Push(unchecked((ushort)entry.Bits));
                        break;
                    case EntryKind.S16:
                        pusher.Push(unchecked((short)entry.Bits));
                        break;
                    case EntryKind.U32:
                        pusher.Push(unchecked((uint)entry.Bits));
                        break;
                    case EntryKind.S32:
                        pusher.Push(unchecked((int)entry.Bits));
                        break;
                    case EntryKind.U64:
                        pusher.Push(entry.Bits);
                        break;
                    case EntryKind.S64:
                        pusher.Push(unchecked((long)entry.Bits));
                        break;
                    case EntryKind.F32:
                        pusher.Push(BitConverter.Int32BitsToSingle(unchecked((int)entry.Bits)));
                        break;
                    case EntryKind.F64:
                        pusher.Push(BitConverter.Int64BitsToDouble(unchecked((long)entry.Bits)));
                        break;
                    case EntryKind.Char32:
                        pusher.PushChar32(unchecked((uint)entry.Bits));
                        break;
                    case EntryKind.String:
                        pusher.Push(GetChars(entry));
                        break;
                    case EntryKind.StringObject:
                        pusher.Push(((string)entry.Object!).AsSpan());
                        break;
                    case EntryKind.Record:
                    {
                        using var nested = pusher.PushRecord();
                        TransferNested(entry, nested);
                        break;
                    }
                    case EntryKind.Variant:
                    {
                        using var nested = pusher.PushVariant(unchecked((int)(entry.Bits >> 32)));
                        TransferNested(entry, nested);
                        break;
                    }
                    case EntryKind.List:
                    {
                        using var nested = pusher.PushList(unchecked((int)(entry.Bits >> 32)));
                        TransferNested(entry, nested);
                        break;
                    }
                    case EntryKind.Flags:
                        pusher.PushFlags(unchecked((uint)entry.Bits));
                        break;
                    case EntryKind.Owned:
                        pusher.PushOwned(GetOwned(entry));
                        break;
                    case EntryKind.Borrowed:
                        pusher.PushBorrowed(new Borrowed(GetOwned(entry)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            OnBufferDrained();

            static void TransferNested(in Entry entry, ValuePusher nested)
            {
                var adapter = (PushPullAdapter)entry.Object!;
                // the nested adapter is already disposed if nothing has been pushed to it
                var version = unchecked((ushort)entry.Bits);
                if (adapter.Version != version) return;
                adapter.TransferBufferedValues(nested);
                adapter.Dispose(version);
            }
        }

        /// <summary>
        ///     Creates an adapter which is not returned to the pool, and reused by its owner with <see cref="Recycle" />.
        /// </summary>
        internal static PushPullAdapter CreateOwned()
        {
            return new PushPullAdapter { isOwned = true };
        }

        /// <summary>
        ///     Discards the buffered values and invalidates the pullables and the pushers of this adapter, so that the
        ///     owner can reuse it. Returns false if it cannot be reused anymore.
        /// </summary>
        internal bool Recycle()
        {
            ForceDispose(Version);
            return Version != ushort.MaxValue;
        }

        /// <summary>
        ///     Disposes this adapter even if buffered values remain.
        /// </summary>
        internal void ForceDispose(ushort version)
        {
            if (Version != version) return;

            while (bufferCount > 0)
            {
                var entry = buffer[bufferHead];
                buffer[bufferHead] = default;
                bufferHead++;
                bufferCount--;

                // nested adapters which are never pulled
                if (entry.Kind is EntryKind.Record or EntryKind.Variant or EntryKind.List)
                    ((PushPullAdapter)entry.Object!).ForceDispose(unchecked((ushort)entry.Bits));
            }

            DisposeCore();
        }

        private void DisposeCore()
        {
            bufferHead = 0;
            charsLength = 0;
            disposeRequested = false;
            if (chars.Length > MaxRetainedChars) chars = Array.Empty<char>();
            nextAwaiter = null;

            if (++Version == ushort.MaxValue || isOwned) return;
            (pool ??= new Stack<PushPullAdapter>()).Push(this);
        }

        private void Enqueue(EntryKind kind, ulong bits, object? obj = null)
        {
            var tail = bufferHead + bufferCount;
            if (tail == buffer.Length)
            {
                if (bufferHead > 0)
                {
                    Array.Copy(buffer, bufferHead, buffer, 0, bufferCount);
                    Array.Clear(buffer, bufferCount, bufferHead);
                    bufferHead = 0;
                }
                else
                {
                    Array.Resize(ref buffer, Math.Max(4, buffer.Length * 2));
                }

                tail = bufferCount;
            }

            buffer[tail] = new Entry(kind, bits, obj);
            bufferCount++;
        }

        private T Dequeue<T>()
        {
            var entry = buffer[bufferHead];
            var value = Read<T>(entry);

            buffer[bufferHead] = default;
            bufferHead++;
            bufferCount--;

            if (bufferCount == 0) OnBufferDrained();
            return value;
        }

        private void OnBufferDrained()
        {
            bufferHead = 0;
            charsLength = 0;
            if (disposeRequested) DisposeCore();
        }

        private T Read<T>(in Entry entry)
        {
            // the branches are resolved at JIT time for value types
            if (typeof(T) == typeof(bool)) return As<bool, T>(Expect(entry, EntryKind.Bool) != 0);
            if (typeof(T) == typeof(byte)) return As<byte, T>(unchecked((byte)Expect(entry, EntryKind.U8)));
            if (typeof(T) == typeof(sbyte)) return As<sbyte, T>(unchecked((sbyte)Expect(entry, EntryKind.S8)));
            if (typeof(T) == typeof(ushort)) return As<ushort, T>(unchecked((ushort)Expect(entry, EntryKind.U16)));
            if (typeof(T) == typeof(short)) return As<short, T>(unchecked((short)Expect(entry, EntryKind.S16)));
            if (typeof(T) == typeof(uint))
            {
                // chars and flags are pulled as uint
                if (entry.Kind is not (EntryKind.U32 or EntryKind.Char32 or EntryKind.Flags))
                    throw new InvalidOperationException();
                return As<uint, T>(unchecked((uint)entry.Bits));
            }

            if (typeof(T) == typeof(int)) return As<int, T>(unchecked((int)Expect(entry, EntryKind.S32)));
            if (typeof(T) == typeof(ulong)) return As<ulong, T>(Expect(entry, EntryKind.U64));
            if (typeof(T) == typeof(long)) return As<long, T>(unchecked((long)Expect(entry, EntryKind.S64)));
            if (typeof(T) == typeof(float))
                return As<float, T>(BitConverter.Int32BitsToSingle(unchecked((int)Expect(entry, EntryKind.F32))));
            if (typeof(T) == typeof(double))
                return As<double, T>(BitConverter.Int64BitsToDouble(unchecked((long)Expect(entry, EntryKind.F64))));

            if (typeof(T) == typeof(RecordPrelude))
            {
                Expect(entry, EntryKind.Record);
                return As<RecordPrelude, T>(new RecordPrelude { BodyPullable = GetNestedPullable(entry) });
            }

            if (typeof(T) == typeof(VariantPrelude))
            {
                var bits = Expect(entry, EntryKind.Variant);
                return As<VariantPrelude, T>(new VariantPrelude
                {
                    BodyPullable = GetNestedPullable(entry),
                    CaseIndex = unchecked((int)(bits >> 32))
                });
            }

            if (typeof(T) == typeof(ListPrelude))
            {
                var bits = Expect(entry, EntryKind.List);
                return As<ListPrelude, T>(new ListPrelude
                {
                    ElementPullable = GetNestedPullable(entry),
                    Length = unchecked((int)(bits >> 32))
                });
            }

            if (typeof(T) == typeof(Owned))
            {
                Expect(entry, EntryKind.Owned);
                return As<Owned, T>(GetOwned(entry));
            }

            if (typeof(T) == typeof(Borrowed))
            {
                Expect(entry, EntryKind.Borrowed);
                return As<Borrowed, T>(new Borrowed(GetOwned(entry)));
            }

            if (typeof(T) == typeof(string))
            {
                if (entry.Kind == EntryKind.StringObject) return (T)entry.Object!;
                Expect(entry, EntryKind.String);
                return (T)(object)GetChars(entry).ToString();
            }

            throw new InvalidOperationException();

            static ulong Expect(in Entry entry, EntryKind kind)
            {
                if (entry.Kind != kind) throw new InvalidOperationException();
                return entry.Bits;
            }

            static TTo As<TFrom, TTo>(TFrom value)
            {
                return Unsafe.As<TFrom, TTo>(ref value);
            }

            static Pullable GetNestedPullable(in Entry entry)
            {
                return new Pullable((PushPullAdapter)entry.Object!, unchecked((ushort)entry.Bits));
            }
        }

        private ReadOnlySpan<char> GetChars(in Entry entry)
        {
            return chars.AsSpan(unchecked((int)(entry.Bits >> 32)), unchecked((int)(uint)entry.Bits));
        }

        private static Owned GetOwned(in Entry entry)
        {
            return new Owned((Ownership)entry.Object!, unchecked((int)(uint)entry.Bits));
        }

        private void PushCore<T>(T value)
        {
            if (nextAwaiter == null) throw new InvalidOperationException();
            if (nextAwaiter is not IAwaiter<T> awaiter) throw new InvalidOperationException();
            nextAwaiter = null;
            awaiter.Push(value);
        }

        private enum EntryKind : byte
        {
            Bool,
            U8,
            S8,
            U16,
            S16,
            U32,
            S32,
            U64,
            S64,
            F32,
            F64,
            Char32,
            String,
            StringObject,
            Record,
            Variant,
            List,
            Flags,
            Owned,
            Borrowed
        }

        private readonly struct Entry
        {
            public readonly object? Object;
            public readonly ulong Bits;
            public readonly EntryKind Kind;

            public Entry(EntryKind kind, ulong bits, object? obj)
            {
                Kind = kind;
                Bits = bits;
                Object = obj;
            }
        }

        private interface IAwaiter
        {
            void Cancel();
        }

        private interface IAwaiter<in T> : IAwaiter
        {
            void Push(T value);
        }

        private class Awaiter<T> : IAwaiter<T>
        {
            [ThreadStatic] private static Stack<Awaiter<T>>? pool;

            private STaskCompletionSource<T> core;

            public STaskCompletionSource<T> Core => core;

            public void Push(T value)
            {
                core.SetResult(value);

                // succeeded
                core = default;
                pool ??= new Stack<Awaiter<T>>();
                pool.Push(this);
            }

            public void Cancel()
            {
                core.SetException(new OperationCanceledException());
            }

            public static Awaiter<T> Get()
            {
                pool ??= new Stack<Awaiter<T>>();
                if (!pool.TryPop(out var pooled)) pooled = new Awaiter<T>();
                pooled.core = STaskCompletionSource<T>.Create();
                return pooled;
            }
        }
    }
}
