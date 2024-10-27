using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS4014

namespace WaaS.Runtime
{
    public class AsyncExternalStackFrame : IStackFrameCore
    {
        [ThreadStatic] private static Stack<AsyncExternalStackFrame> pool;

        private AsyncExternalFunction function;
        private Memory<StackValueItem> inputValues;
        private StackValueItem[] inputValuesArray;
        private Memory<StackValueItem> outputValues;
        private StackValueItem[] outputValuesArray;
        private StackFrameState state = StackFrameState.Ready;

        public void Dispose(ushort version)
        {
            if (version != Version) return;
            if (inputValuesArray != null)
            {
                ArrayPool<StackValueItem>.Shared.Return(inputValuesArray);
                inputValuesArray = null;
            }

            if (outputValuesArray != null)
            {
                ArrayPool<StackValueItem>.Shared.Return(outputValuesArray);
                outputValuesArray = null;
            }

            function = default;
            inputValues = default;
            outputValues = default;
            inputValuesArray = default;
            outputValuesArray = default;
            state = default;
        }

        public int GetResultLength(ushort version)
        {
            ThrowIfOutdated(version);
            return function.Type.ResultTypes.Length;
        }

        public StackFrameState MoveNext(ushort version, Waker waker)
        {
            ThrowIfOutdated(version);
            if (state == StackFrameState.Ready)
            {
                state = StackFrameState.Pending;
                InvokeAsync(waker);
                return StackFrameState.Pending;
            }

            return state;
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            outputValues.Span.CopyTo(dest);
        }

        public ushort Version { get; }

        public static AsyncExternalStackFrame Get(AsyncExternalFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            pool ??= new Stack<AsyncExternalStackFrame>();
            if (!pool.TryPop(out var pooled)) pooled = new AsyncExternalStackFrame();
            pooled.Reset(function, inputValues);
            return pooled;
        }

        private void Reset(AsyncExternalFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            var type = function.Type;
            inputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ParameterTypes.Length);
            outputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ResultTypes.Length);
            Array.Clear(inputValuesArray, 0, inputValuesArray.Length);
            Array.Clear(outputValuesArray, 0, outputValuesArray.Length);

            this.inputValues = inputValuesArray.AsMemory().Slice(0, type.ParameterTypes.Length);
            outputValues = outputValuesArray.AsMemory().Slice(0, type.ResultTypes.Length);

            inputValues.CopyTo(this.inputValues.Span);
            state = StackFrameState.Ready;
        }

        private async ValueTask InvokeAsync(Waker waker)
        {
            try
            {
                var type = function.Type;
                await function.InvokeAsync(inputValues.Span, outputValues);
            }
            catch (Exception ex)
            {
                waker.Fail(ex);
                throw;
            }

            state = StackFrameState.Completed;

            waker.Wake();
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }
    }
}