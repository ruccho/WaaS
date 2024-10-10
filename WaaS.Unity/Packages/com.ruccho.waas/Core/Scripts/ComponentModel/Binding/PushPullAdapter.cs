#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
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

        public ValueTask<T> PullValueAsync<T>()
        {
            var formatter = FormatterProvider.GetFormatter<T>();
            return formatter.PullAsync(new Pullable(this));
        }

        public ValueTask<T> PullPrimitiveValueAsync<T>()
        {
            if (nextAwaiter is Awaiter<T> preservedAwaiter)
            {
                nextAwaiter = null;
                return new ValueTask<T>(preservedAwaiter.GetResult(preservedAwaiter.Version));
            }

            nextAwaiter?.Cancel();
            nextAwaiter = null;

            var awaiter = Awaiter<T>.Get(out var token);
            nextAwaiter = awaiter;
            return new ValueTask<T>(awaiter, token);
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

        public void PushOwned<THandle, T1>(THandle handle) where THandle : IOwned<T1> where T1 : IResourceType
        {
            PushCore(handle);
        }

        public void PushBorrowed<THandle, T1>(THandle handle)
            where THandle : IBorrowed<T1> where T1 : IResourceType
        {
            PushCore(handle);
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
            if (nextAwaiter == null)
            {
                var preservedAwaiter = Awaiter<T>.Get(out _);
                nextAwaiter = preservedAwaiter;
                preservedAwaiter.Push(value);
                return;
            }

            if (nextAwaiter is not IAwaiter<T> awaiter) throw new InvalidOperationException();
            awaiter.Push(value);
            nextAwaiter = null;
        }

        private interface IAwaiter
        {
            void Cancel();
        }

        private interface IAwaiter<in T> : IAwaiter
        {
            void Push(T value);
        }

        private class Awaiter<T> : IAwaiter<T>, IValueTaskSource<T>
        {
            [ThreadStatic] public static Stack<Awaiter<T>> pool;

            private ManualResetValueTaskSourceCore<T> core;
            public short Version => core.Version;

            public void Push(T value)
            {
                core.SetResult(value);
            }

            public void Cancel()
            {
                core.SetException(new OperationCanceledException());
            }

            public T GetResult(short token)
            {
                try
                {
                    return core.GetResult(token);
                }
                finally
                {
                    core.Reset();
                    pool ??= new Stack<Awaiter<T>>();
                    pool.Push(this);
                }
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token,
                ValueTaskSourceOnCompletedFlags flags)
            {
                core.OnCompleted(continuation, state, token, flags);
            }

            public static Awaiter<T> Get(out short token)
            {
                pool ??= new Stack<Awaiter<T>>();
                if (!pool.TryPop(out var pooled)) pooled = new Awaiter<T>();
                token = pooled.core.Version;
                return pooled;
            }
        }
    }
}