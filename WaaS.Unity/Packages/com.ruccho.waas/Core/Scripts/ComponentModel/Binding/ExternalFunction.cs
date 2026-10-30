#nullable enable
using System;
using System.Collections.Generic;
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

        /// <summary>
        ///     Invokes the host implementation.
        ///     It is called when the frame of the function is first moved, after all the arguments are pushed, so that
        ///     <paramref name="arguments" /> can be pulled, and <paramref name="frameMove" /> and
        ///     <paramref name="resultPusher" /> can be awaited without suspension.
        /// </summary>
        protected abstract STaskVoid InvokeAsync(ExecutionContext context, Pullable arguments,
            STaskVoid frameMove, STask<ValuePusher> resultPusher);

        private class Binder : IFunctionBinderCore, IStackFrameCore
        {
            private static readonly Stack<Binder> Pool = new();

            private readonly Action onInvocationCompleted;
            private bool abandoned;
            private PushPullAdapter arguments;
            private ExecutionContext? context;
            private ExternalFunction? function;
            private STaskVoid invocation;
            private InvocationState state;
            private PushPullAdapter results;
            private bool suspended;
            private Waker waker;

            private Binder()
            {
                onInvocationCompleted = OnInvocationCompleted;
                arguments = PushPullAdapter.CreateOwned();
                results = PushPullAdapter.CreateOwned();
            }

            public ushort Version { get; private set; }

            public ValuePusher ArgumentPusher { get; private set; }

            public void Dispose(ushort version)
            {
                if (Version != version) return;

                if (state == InvocationState.Pending)
                {
                    // The host function is still running (e.g. the execution is canceled while suspended).
                    // It may still use the arguments and the results, so this binder is never reused.
                    abandoned = true;
                    Version++;
                    return;
                }

                // the adapters are kept for reuse
                if (!arguments.Recycle()) arguments = PushPullAdapter.CreateOwned();
                if (!results.Recycle()) results = PushPullAdapter.CreateOwned();
                context = null;
                function = null;
                invocation = default;

                if (++Version == ushort.MaxValue) return;
                Pool.Push(this);
            }

            public StackFrame CreateFrame()
            {
                return new StackFrame(this);
            }

            public void TakeResults(ValuePusher resultValuePusher)
            {
                results.TransferBufferedValues(resultValuePusher);
            }

            public static Binder Get(ExecutionContext context, ExternalFunction function)
            {
                if (!Pool.TryPop(out var pooled)) pooled = new Binder();
                pooled.context = context;
                pooled.function = function;
                pooled.ArgumentPusher = new ValuePusher(pooled.arguments);
                pooled.invocation = default;
                pooled.state = InvocationState.NotStarted;
                pooled.waker = default;
                pooled.suspended = false;
                pooled.abandoned = false;
                return pooled;
            }

            private void OnInvocationCompleted()
            {
                if (abandoned)
                {
                    ObserveAbandonedInvocation(invocation);
                    return;
                }

                state = InvocationState.Completed;
                // it may be called synchronously while the continuation is registered
                if (suspended) waker.Wake();
            }

            private static void ObserveAbandonedInvocation(STaskVoid invocation)
            {
                try
                {
                    invocation.GetAwaiter().GetResult();
                }
                catch
                {
                    // the invocation is abandoned and no one observes the result
                }
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (version != Version) throw new InvalidOperationException();
            }

            private enum InvocationState : byte
            {
                NotStarted,
                Pending,
                Completed,
                Finished
            }

            #region IStackFrame implementation

            int IStackFrameCore.GetResultLength(ushort version)
            {
                return 0;
            }

            StackFrameState IStackFrameCore.MoveNext(ushort version, Waker waker)
            {
                ThrowIfOutdated(version);
                switch (state)
                {
                    case InvocationState.NotStarted:
                    {
                        var invocation = function!.InvokeAsync(context!, new Pullable(arguments), default,
                            new STask<ValuePusher>(new ValuePusher(results)));
                        var awaiter = invocation.GetAwaiter();
                        if (awaiter.IsCompleted)
                        {
                            // completed synchronously
                            state = InvocationState.Finished;
                            awaiter.GetResult();
                            return StackFrameState.Completed;
                        }

                        this.invocation = invocation;
                        state = InvocationState.Pending;
                        awaiter.OnCompleted(onInvocationCompleted);
                        if (state != InvocationState.Pending) return StackFrameState.Ready;

                        this.waker = waker;
                        suspended = true;
                        return StackFrameState.Pending;
                    }
                    case InvocationState.Pending:
                        this.waker = waker;
                        suspended = true;
                        return StackFrameState.Pending;
                    case InvocationState.Completed:
                    {
                        state = InvocationState.Finished;
                        var invocation = this.invocation;
                        this.invocation = default;
                        invocation.GetAwaiter().GetResult();
                        return StackFrameState.Completed;
                    }
                    default:
                        return StackFrameState.Completed;
                }
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
        }
    }

    public class ExternalFunctionDelegate : ExternalFunction
    {
        public delegate STaskVoid InvokeAsyncDelegate(ExecutionContext context, Pullable arguments,
            STaskVoid frameMove,
            STask<ValuePusher> resultPusher);

        private readonly InvokeAsyncDelegate invokeAsync;

        public ExternalFunctionDelegate(IFunctionType type, InvokeAsyncDelegate invokeAsync)
        {
            this.invokeAsync = invokeAsync;
            Type = type;
        }

        public override IFunctionType Type { get; }

        protected override STaskVoid InvokeAsync(ExecutionContext context, Pullable arguments, STaskVoid frameMove,
            STask<ValuePusher> resultPusher)
        {
            return invokeAsync(context, arguments, frameMove, resultPusher);
        }
    }
}