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
        private int isCompleted;

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
            popped.isCompleted = 0;
            return popped;
        }

        public ValueTask AsValueTask()
        {
            return new ValueTask(this, core.Version);
        }

        public void TrySetResult()
        {
            if (Interlocked.Exchange(ref isCompleted, 1) != 0) return;
            core.SetResult(false);
        }

        public void TrySetException(Exception ex)
        {
            if (Interlocked.Exchange(ref isCompleted, 1) != 0) return;
            core.SetException(ex);
        }

        public void TrySetCanceled(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref isCompleted, 1) != 0) return;

            // the pending operation may still try to complete this source later,
            // so it must not be returned to the pool and reused.
            Volatile.Write(ref isAlive, 0);
            core.SetException(new OperationCanceledException(cancellationToken));
        }
    }
}