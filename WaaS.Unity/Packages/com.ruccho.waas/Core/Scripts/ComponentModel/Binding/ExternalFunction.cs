#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public abstract class ExternalFunction : IFunction
    {
        public abstract IFunctionType Type { get; }

        FunctionBinder IFunction.GetBinder(ExecutionContext context)
        {
            return new FunctionBinder(Binder.Get(context, this));
        }

        protected abstract ValueTask PullArgumentsAsync(ExecutionContext context, PushPullAdapter adapter,
            ValueTask frameMove, ValueTask<ValuePusher> resultPusher);

        private class Binder : IFunctionBinderCore, IValueTaskSource<ValuePusher>, IValueTaskSource, IStackFrame
        {
            private static readonly Stack<Binder> Pool = new();

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
                resultPusher = resultValuePusher;
                resultPusherContinuation?.Invoke(resultPusherState!);
            }

            public static Binder Get(ExecutionContext context, ExternalFunction function)
            {
                if (!Pool.TryPop(out var pooled)) pooled = new Binder();
                var pusher = PushPullAdapter.Get();
                pooled.ArgumentPusher = new ValuePusher(pusher);
                pooled.resultPusherContinuation = null;
                pooled.resultPusherState = null;

                pooled.frameMoveCompleted = default;
                pooled.frameMoveContinuation = default;
                pooled.frameMoveState = default;

                function.PullArgumentsAsync(
                    context,
                    pusher,
                    new ValueTask(pooled, unchecked((short)pooled.Version)),
                    new ValueTask<ValuePusher>(pooled, unchecked((short)pooled.Version)));

                return pooled;
            }

            #region IValueTaskSource<ValuePusher> implementation

            private ValuePusher? resultPusher;
            private Action<object?>? resultPusherContinuation;
            private object? resultPusherState;

            ValuePusher IValueTaskSource<ValuePusher>.GetResult(short token)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
                return resultPusher!.Value;
            }

            ValueTaskSourceStatus IValueTaskSource<ValuePusher>.GetStatus(short token)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
                return resultPusher != null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;
            }

            void IValueTaskSource<ValuePusher>.OnCompleted(Action<object?> continuation, object? state, short token,
                ValueTaskSourceOnCompletedFlags flags)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
                if (resultPusher.HasValue)
                {
                    continuation.Invoke(state);
                    return;
                }

                if (continuation != null) throw new InvalidOperationException();
                resultPusherContinuation = continuation;
                resultPusherState = state;
            }

            #endregion


            #region IValueTaskSource implementation

            private bool frameMoveCompleted;
            private Action<object?>? frameMoveContinuation;
            private object? frameMoveState;

            void IValueTaskSource.GetResult(short token)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
            }

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
                return frameMoveCompleted ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;
            }

            void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token,
                ValueTaskSourceOnCompletedFlags flags)
            {
                if (token != unchecked((short)Version)) throw new InvalidOperationException();
                if (frameMoveCompleted)
                {
                    continuation.Invoke(state);
                    return;
                }

                if (continuation != null) throw new InvalidOperationException();
                frameMoveContinuation = continuation;
                frameMoveState = state;
            }

            #endregion

            #region ISyncFrame implementation

            int IStackFrame.ResultLength => 0;

            StackFrameState IStackFrame.MoveNext(Waker waker)
            {
                frameMoveCompleted = true;
                frameMoveContinuation?.Invoke(frameMoveState);
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