using System;
using System.Buffers;
using System.Threading.Tasks;

#pragma warning disable CS4014

namespace WaaS.Runtime
{
    public class AsyncExternalStackFrame : StackFrame
    {
        private readonly AsyncExternalFunction function;
        private StackValueItem[] inputValues;
        private StackValueItem[] outputValues;
        private StackFrameState state = StackFrameState.Ready;

        internal AsyncExternalStackFrame(AsyncExternalFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            var type = function.Type;
            this.inputValues = ArrayPool<StackValueItem>.Shared.Rent(type.ParameterTypes.Length);
            outputValues = ArrayPool<StackValueItem>.Shared.Rent(type.ResultTypes.Length);

            inputValues.CopyTo(this.inputValues);
        }

        public override int ResultLength => function.Type.ResultTypes.Length;

        public override StackFrameState MoveNext(Waker waker)
        {
            if (state == StackFrameState.Ready)
            {
                state = StackFrameState.Pending;
                InvokeAsync(waker);
            }

            return state;
        }

        private async ValueTask InvokeAsync(Waker waker)
        {
            try
            {
                var type = function.Type;
                await function.InvokeAsync(inputValues.AsSpan(0, type.ParameterTypes.Length),
                    outputValues.AsMemory(0, type.ResultTypes.Length));
            }
            catch (Exception ex)
            {
                waker.Fail(ex);
                return;
            }

            waker.Wake();
            state = StackFrameState.Completed;
        }

        public override void TakeResults(Span<StackValueItem> dest)
        {
            outputValues.CopyTo(dest);
        }

        public override void Dispose()
        {
            if (inputValues != null)
            {
                ArrayPool<StackValueItem>.Shared.Return(inputValues);
                inputValues = null;
            }

            if (outputValues != null)
            {
                ArrayPool<StackValueItem>.Shared.Return(outputValues);
                outputValues = null;
            }
        }
    }
}