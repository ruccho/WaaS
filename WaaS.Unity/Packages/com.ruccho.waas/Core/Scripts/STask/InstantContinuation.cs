using System;
using System.Collections.Generic;

namespace STask
{
    internal class InstantContinuation<T>
    {
        public delegate void ContinueAction(in T context);

        [ThreadStatic] private static Stack<InstantContinuation<T>> pool;
        private readonly Action continuation;

        private T context;
        private ContinueAction onContinue;

        private InstantContinuation()
        {
            continuation = Continue;
        }

        public static Action Get(in T context, ContinueAction onContinue)
        {
            pool ??= new Stack<InstantContinuation<T>>();
            if (!pool.TryPop(out var pooled)) pooled = new InstantContinuation<T>();

            pooled.context = context;
            pooled.onContinue = onContinue;
            return pooled.continuation;
        }

        private void Continue()
        {
            try
            {
                onContinue(context);
            }
            finally
            {
                context = default;
                pool ??= new Stack<InstantContinuation<T>>();
                pool.Push(this);
            }
        }
    }
}