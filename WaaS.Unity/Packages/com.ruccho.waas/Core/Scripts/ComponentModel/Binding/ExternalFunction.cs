#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using STask;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Represents a function whose implementation is provided by the host code.
    /// </summary>
    public abstract class ExternalFunction : IFunction
    {
        public abstract IFunctionType Type { get; }

        FunctionBinder IFunction.GetBinder(ExecutionContext context)
        {
            return new FunctionBinder(Binder.Get(context, this));
        }

        protected abstract STaskVoid InvokeAsync(ExecutionContext context, PushPullAdapter adapter,
            STaskVoid frameMove, STask<ValuePusher> resultPusher);

        private class Binder : IFunctionBinderCore, IStackFrameCore, ISTaskSource<ValuePusher>
        {
            private static readonly Stack<Binder> Pool = new();
            private object? context;
            private Action<object>? continuation;

            private bool frameMoveCompleted;
            private STaskCompletionSource<byte> frameMoveSource;
            private ValuePusher? resultPusher;
            private int resultPusherRequestedState;
            private Waker? waker;

            public ushort Version { get; private set; }

            public ValuePusher ArgumentPusher { get; private set; }

            public void Dispose(ushort version)
            {
                if (Version != version) return;
                if (++Version == ushort.MaxValue) return;
                ArgumentPusher.Dispose();
                Pool.Push(this);
            }

            public StackFrame CreateFrame()
            {
                return new StackFrame(this);
            }

            public void TakeResults(ValuePusher resultValuePusher)
            {
                resultPusher = resultValuePusher;
                continuation?.Invoke(context!);
            }

            public static Binder Get(ExecutionContext context, ExternalFunction function)
            {
                if (!Pool.TryPop(out var pooled)) pooled = new Binder();
                var pusher = PushPullAdapter.Get();
                pooled.ArgumentPusher = new ValuePusher(pusher);

                pooled.frameMoveSource = STaskCompletionSource<byte>.Create();
                pooled.frameMoveCompleted = false;
                pooled.continuation = null;
                pooled.context = null;
                pooled.resultPusher = default;
                pooled.waker = default;
                pooled.resultPusherRequestedState = 0;

                function.InvokeAsync(
                    context,
                    pusher,
                    new STaskVoid(pooled.frameMoveSource.TaskSource),
                    new STask<ValuePusher>(new STaskSource<ValuePusher>(pooled, pooled.Version))).Forget();

                return pooled;
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (version != Version) throw new InvalidOperationException();
            }

            #region IStackFrame implementation

            int IStackFrameCore.GetResultLength(ushort version)
            {
                return 0;
            }

            StackFrameState IStackFrameCore.MoveNext(ushort version, Waker waker)
            {
                ThrowIfOutdated(version);
                if (!frameMoveCompleted)
                {
                    // first
                    frameMoveCompleted = true;
                    frameMoveSource.SetResult(0);

                    this.waker = waker;

                    if (Interlocked.CompareExchange(ref resultPusherRequestedState, 2, 0) == 0)
                        return StackFrameState.Pending;
                }

                return StackFrameState.Completed;
            }

            void IStackFrameCore.TakeResults(ushort version, Span<StackValueItem> dest)
            {
            }

            public bool DoesTakeResults(ushort version)
            {
                return false;
            }

            public void PushResults(ushort version, Span<StackValueItem> source)
            {
                throw new InvalidOperationException();
            }

            #endregion

            #region ISTaskSource<ValuePusher> implementation

            public void OnCompleted(ushort version, Action<object> continuation, object context)
            {
                // on await
                ThrowIfOutdated(version);

                if (Interlocked.CompareExchange(ref this.continuation, continuation, null) != null)
                    throw new InvalidOperationException();
                this.context = context;

                switch (Interlocked.CompareExchange(ref resultPusherRequestedState, 1, 0))
                {
                    case 0:
                    {
                        // 0 -> 1
                        // this is earlier than pending check
                        break;
                    }
                    case 2:
                    {
                        // pending is returned already
                        waker?.Wake();
                        break;
                    }
                    default:
                        throw new InvalidOperationException();
                }
            }

            public ValuePusher GetResult(ushort version)
            {
                ThrowIfOutdated(version);
                return resultPusher!.Value;
            }

            public bool IsCompleted => resultPusher.HasValue;

            #endregion
        }
    }

    public class ExternalFunctionDelegate : ExternalFunction
    {
        public delegate STaskVoid InvokeAsyncDelegate(ExecutionContext context, PushPullAdapter adapter,
            STaskVoid frameMove,
            STask<ValuePusher> resultPusher);

        private readonly InvokeAsyncDelegate invokeAsync;

        public ExternalFunctionDelegate(IFunctionType type, InvokeAsyncDelegate invokeAsync)
        {
            this.invokeAsync = invokeAsync;
            Type = type;
        }

        public override IFunctionType Type { get; }

        protected override STaskVoid InvokeAsync(ExecutionContext context, PushPullAdapter adapter, STaskVoid frameMove,
            STask<ValuePusher> resultPusher)
        {
            return invokeAsync(context, adapter, frameMove, resultPusher);
        }
    }
}