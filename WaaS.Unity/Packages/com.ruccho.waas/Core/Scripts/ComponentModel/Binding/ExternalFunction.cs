#nullable enable
using System;
using System.Collections.Generic;
using STask;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Binding
{
    public abstract class ExternalFunction : IFunction
    {
        public abstract IFunctionType Type { get; }

        FunctionBinder IFunction.GetBinder(ExecutionContext context)
        {
            return new FunctionBinder(Binder.Get(context, this));
        }

        protected abstract STaskVoid PullArgumentsAsync(ExecutionContext context, PushPullAdapter adapter,
            STaskVoid frameMove, STask<ValuePusher> resultPusher);

        private class Binder : IFunctionBinderCore, IStackFrame
        {
            private static readonly Stack<Binder> Pool = new();

            private STaskVoid executeTask;

            private bool frameMoveCompleted;
            private STaskCompletionSource<byte> frameMoveSource;

            private STaskCompletionSource<ValuePusher> resultPusherSource;

            public ushort Version { get; private set; }

            public ValuePusher ArgumentPusher { get; private set; }

            public void Dispose(ushort version)
            {
                if (Version != version) return;
                if (++Version == ushort.MaxValue) return;
                Pool.Push(this);
            }

            public IStackFrame CreateFrame()
            {
                return this;
            }

            public void TakeResults(ValuePusher resultValuePusher)
            {
                resultPusherSource.SetResult(resultValuePusher);
            }

            public static Binder Get(ExecutionContext context, ExternalFunction function)
            {
                if (!Pool.TryPop(out var pooled)) pooled = new Binder();
                var pusher = PushPullAdapter.Get();
                pooled.ArgumentPusher = new ValuePusher(pusher);

                pooled.resultPusherSource = STaskCompletionSource<ValuePusher>.Create();
                pooled.frameMoveSource = STaskCompletionSource<byte>.Create();
                pooled.frameMoveCompleted = false;

                pooled.executeTask = function.PullArgumentsAsync(
                    context,
                    pusher,
                    new STaskVoid(pooled.frameMoveSource),
                    new STask<ValuePusher>(pooled.resultPusherSource));

                return pooled;
            }

            #region IStackFrame implementation

            int IStackFrame.ResultLength => 0;

            StackFrameState IStackFrame.MoveNext(Waker waker)
            {
                static async void WaitForCompletionAsync(Waker waker, STaskVoid task)
                {
                    await task;
                    waker.Wake();
                }

                if (!frameMoveCompleted)
                {
                    // first
                    frameMoveCompleted = true;
                    frameMoveSource.SetResult(0);
                }

                if (!executeTask.source.IsCompleted)
                {
                    WaitForCompletionAsync(waker, executeTask);
                    return StackFrameState.Pending;
                }

                return StackFrameState.Completed;
            }

            void IStackFrame.TakeResults(Span<StackValueItem> dest)
            {
            }

            void IDisposable.Dispose()
            {
            }

            #endregion
        }
    }
}