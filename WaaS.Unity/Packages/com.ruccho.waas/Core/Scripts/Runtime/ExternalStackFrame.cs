using System;
using System.Buffers;
using System.Collections.Generic;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a stack frame used to invoke an external function.
    /// </summary>
    public class ExternalStackFrame : IStackFrameCore
    {
        [ThreadStatic] private static Stack<ExternalStackFrame> pool;
        private ExecutionContext context;

        private ExternalFunction function;
        private Memory<StackValueItem> inputValues;
        private StackValueItem[] inputValuesArray;
        private bool invoked;
        private Memory<StackValueItem> outputValues;
        private StackValueItem[] outputValuesArray;

        public void Dispose(ushort version)
        {
            if (version != Version) return;
            if (inputValuesArray != default) ArrayPool<StackValueItem>.Shared.Return(inputValuesArray);

            if (outputValuesArray != default) ArrayPool<StackValueItem>.Shared.Return(outputValuesArray);

            function = default;
            inputValues = default;
            outputValues = default;
            inputValuesArray = default;
            outputValuesArray = default;
            invoked = default;
            context = default;

            if (++Version != ushort.MaxValue)
            {
                pool ??= new Stack<ExternalStackFrame>();
                pool.Push(this);
            }
        }

        public ushort Version { get; private set; }

        public int GetResultLength(ushort version)
        {
            ThrowIfOutdated(version);
            return function.Type.ResultTypes.Length;
        }

        public StackFrameState MoveNext(ushort version, Waker waker)
        {
            ThrowIfOutdated(version);
            if (invoked) throw new InvalidOperationException();
            invoked = true;
            function.Invoke(context, inputValues.Span, outputValues.Span);
            return StackFrameState.Completed;
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            outputValues.Span.CopyTo(dest);
        }

        public static ExternalStackFrame Get(ExecutionContext context, ExternalFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            pool ??= new Stack<ExternalStackFrame>();
            if (!pool.TryPop(out var pooled)) pooled = new ExternalStackFrame();
            pooled.Reset(context, function, inputValues);
            return pooled;
        }

        private void Reset(ExecutionContext context, ExternalFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            this.function = function;
            this.context = context;
            var type = function.Type;
            inputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ParameterTypes.Length);
            outputValuesArray = ArrayPool<StackValueItem>.Shared.Rent(type.ResultTypes.Length);
            Array.Clear(inputValuesArray, 0, inputValuesArray.Length);
            Array.Clear(outputValuesArray, 0, outputValuesArray.Length);

            this.inputValues = inputValuesArray.AsMemory().Slice(0, type.ParameterTypes.Length);
            outputValues = outputValuesArray.AsMemory().Slice(0, type.ResultTypes.Length);

            inputValues.CopyTo(this.inputValues.Span);
            invoked = false;
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }
    }
}