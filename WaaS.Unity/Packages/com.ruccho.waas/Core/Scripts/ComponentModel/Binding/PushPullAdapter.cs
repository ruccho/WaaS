#nullable enable
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
    /// </summary>
    public class PushPullAdapter : IValuePusherCore, IPullableCore
    {
        [ThreadStatic] private static Stack<PushPullAdapter>? pool;

        private IAwaiter? nextAwaiter;

        private PushPullAdapter()
        {
        }

        public STask<T> PullValueAsync<T>()
        {
            RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
            var formatter = FormatterProvider.GetFormatter<T>();
            return formatter.PullAsync(new Pullable(this));
        }

        public STask<T> PullPrimitiveValueAsync<T>()
        {
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
            if (++Version == ushort.MaxValue) return;
            (pool ??= new Stack<PushPullAdapter>()).Push(this);
        }

        public bool TryGetNextType([NotNullWhen(true)] out IValueType? type)
        {
            type = default;
            return false;
        }

        public void Push(bool value)
        {
            PushCore(value);
        }

        public void Push(byte value)
        {
            PushCore(value);
        }

        public void Push(sbyte value)
        {
            PushCore(value);
        }

        public void Push(ushort value)
        {
            PushCore(value);
        }

        public void Push(short value)
        {
            PushCore(value);
        }

        public void Push(uint value)
        {
            PushCore(value);
        }

        public void Push(int value)
        {
            PushCore(value);
        }

        public void Push(ulong value)
        {
            PushCore(value);
        }

        public void Push(long value)
        {
            PushCore(value);
        }

        public void Push(float value)
        {
            PushCore(value);
        }

        public void Push(double value)
        {
            PushCore(value);
        }

        public void PushChar32(uint value)
        {
            PushCore(value);
        }

        public void Push(ReadOnlySpan<char> value)
        {
            PushCore(value.ToString());
        }

        public ValuePusher PushRecord()
        {
            var p = Get();
            PushCore(new RecordPrelude
            {
                BodyPullable = new Pullable(p)
            });
            return new ValuePusher(p);
        }

        public ValuePusher PushVariant(int caseIndex)
        {
            var p = Get();
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
            PushCore(new ListPrelude
            {
                ElementPullable = new Pullable(p),
                Length = length
            });
            return new ValuePusher(p);
        }

        public void PushFlags(uint flagValue)
        {
            PushCore(flagValue);
        }

        public void PushOwned(Owned handle)
        {
            PushCore(handle);
        }

        public void PushBorrowed(Borrowed handle)
        {
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

        private void PushCore<T>(T value)
        {
            if (nextAwaiter == null) throw new InvalidOperationException();
            if (nextAwaiter is not IAwaiter<T> awaiter) throw new InvalidOperationException();
            nextAwaiter = null;
            awaiter.Push(value);
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