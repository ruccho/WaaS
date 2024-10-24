#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public class PushPullAdapter : IValuePusherCore, IPullableCore
    {
        private static readonly Stack<PushPullAdapter> Pool = new();

        private IAwaiter? nextAwaiter;

        private PushPullAdapter()
        {
        }

        public STask<T> PullValueAsync<T>()
        {
            var formatter = FormatterProvider.GetFormatter<T>();
            return formatter.PullAsync(new Pullable(this));
        }

        public STask<T> PullPrimitiveValueAsync<T>()
        {
            if (nextAwaiter is Awaiter<T> preservedAwaiter)
            {
                nextAwaiter = null;
                return new STask<T>(preservedAwaiter.Core);
            }

            nextAwaiter?.Cancel();
            nextAwaiter = null;

            var awaiter = Awaiter<T>.Get();
            nextAwaiter = awaiter;
            return new STask<T>(awaiter.Core);
        }

        public ushort Version { get; private set; }

        public void Dispose(ushort version)
        {
            if (Version != version) return;
            if (++Version == ushort.MaxValue) return;
            Pool.Push(this);
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

        public void PushOwned<T1>(Owned<T1> handle) where T1 : class, IResourceType
        {
            // despecialize (need T1 to be a class)
            ref var handleTyped = ref Unsafe.As<Owned<T1>, Owned<IResourceType>>(ref handle);
            PushCore(handleTyped);
        }

        public void PushBorrowed<T1>(Borrowed<T1> handle)
            where T1 : class, IResourceType
        {
            // despecialize (need T1 to be a class)
            ref var handleTyped = ref Unsafe.As<Borrowed<T1>, Borrowed<IResourceType>>(ref handle);
            PushCore(handleTyped);
        }

        internal static PushPullAdapter Get()
        {
            if (!Pool.TryPop(out var pooled)) pooled = new PushPullAdapter();
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
            [ThreadStatic] public static Stack<Awaiter<T>> pool;

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