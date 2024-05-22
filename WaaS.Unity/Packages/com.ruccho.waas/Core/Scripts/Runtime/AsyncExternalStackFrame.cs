using System;
using System.Buffers;
using System.Threading.Tasks;

#pragma warning disable CS4014

namespace WaaS.Runtime
{
    public class AsyncExternalStackFrame : StackFrame
    {
        private readonly AsyncExternalFunction function;
        private readonly Memory<StackValueItem> inputValues;
        private readonly Memory<StackValueItem> outputValues;
        private StackValueItem[] inputValuesArray;
        private StackValueItem[] outputValuesArray;
        private StackFrameState state = StackFrameState.Ready;

        internal AsyncExternalStackFrame(AsyncExternalFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            var type = function.Type;
            inputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ParameterTypes.Length);
            outputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ResultTypes.Length);

            this.inputValues = inputValuesArray.AsMemory().Slice(0, type.ParameterTypes.Length);
            outputValues = outputValuesArray.AsMemory().Slice(0, type.ResultTypes.Length);

            inputValues.CopyTo(this.inputValues.Span);
        }

        public override int ResultLength => function.Type.ResultTypes.Length;

        public override StackFrameState MoveNext(Waker waker)
        {
            if (state == StackFrameState.Ready)
            {
                state = StackFrameState.Pending;
                InvokeAsync(waker);
                return StackFrameState.Pending;
            }

            return state;
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

        public override void TakeResults(Span<StackValueItem> dest)
        {
            outputValues.Span.CopyTo(dest);
        }

        public override void Dispose()
        {
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
        }
    }
}