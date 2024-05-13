using System;

namespace WaaS.Runtime
{
    public class ExternalStackFrame : StackFrame
    {
        private readonly ExternalFunction function;
        private readonly StackValueItem[] inputValues;
        private readonly StackValueItem[] outputValues;

        private StateKind state;

        internal ExternalStackFrame(ExternalFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            var type = function.Type;
            this.inputValues = new StackValueItem[type.ParameterTypes.Length];
            outputValues = new StackValueItem[type.ResultTypes.Length];

            inputValues.CopyTo(this.inputValues);

            state = StateKind.Ready;
        }

        public override int ResultLength => function.Type.ResultTypes.Length;

        public override bool MoveNext()
        {
            switch (state)
            {
                case StateKind.Ready:
                {
                    state = StateKind.Running;
                    function.Invoke(inputValues, outputValues);
                    return false;
                }
                case StateKind.Running:
                {
                    throw new InvalidOperationException();
                }
                case StateKind.Completed:
                {
                    return false;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void TakeResults(Span<StackValueItem> dest)
        {
            outputValues.CopyTo(dest);
        }

        public override void Dispose()
        {
        }

        private enum StateKind
        {
            Ready,
            Running,
            Completed
        }
    }
}