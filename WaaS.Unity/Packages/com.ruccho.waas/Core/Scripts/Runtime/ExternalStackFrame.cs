using System;

namespace WaaS.Runtime
{
    public class ExternalStackFrame : IStackFrame
    {
        private readonly ExternalFunction function;
        private readonly StackValueItem[] inputValues;
        private readonly StackValueItem[] outputValues;
        private bool invoked;

        internal ExternalStackFrame(ExternalFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            var type = function.Type;
            this.inputValues = new StackValueItem[type.ParameterTypes.Length];
            outputValues = new StackValueItem[type.ResultTypes.Length];

            inputValues.CopyTo(this.inputValues);
        }

        public int ResultLength => function.Type.ResultTypes.Length;

        public StackFrameState MoveNext(Waker waker)
        {
            if (invoked) throw new InvalidOperationException();
            invoked = true;
            function.Invoke(inputValues, outputValues);
            return StackFrameState.Completed;
        }

        public void TakeResults(Span<StackValueItem> dest)
        {
            outputValues.CopyTo(dest);
        }

        public void Dispose()
        {
        }
    }
}