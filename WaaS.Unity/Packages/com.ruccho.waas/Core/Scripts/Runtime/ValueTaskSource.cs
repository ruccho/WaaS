using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace WaaS.Runtime
{
    internal class ValueTaskSource : IValueTaskSource
    {
        private static readonly Stack<ValueTaskSource> Pool = new();
        private ManualResetValueTaskSourceCore<bool> core;
        private int isAlive = 1;

        private ValueTaskSource()
        {
        }

        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (Interlocked.CompareExchange(ref isAlive, 0, 1) == 1) Pool.Push(this);
            }
        }

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
            core.OnCompleted(continuation, state, token, flags);
        }

        public static ValueTaskSource Create()
        {
            if (!Pool.TryPop(out var popped)) popped = new ValueTaskSource();
            popped.core.Reset();
            popped.isAlive = 1;
            return popped;
        }

        public ValueTask AsValueTask()
        {
            return new ValueTask(this, core.Version);
        }

        public void SetResult()
        {
            core.SetResult(false);
        }

        public void SetException(Exception ex)
        {
            core.SetException(ex);
        }
    }
}