using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    public abstract class StackFrame
    {
        public abstract int ResultLength { get; }
        public abstract bool MoveNext();

        public abstract void TakeResults(Span<StackValueItem> dest);
    }

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

        private enum StateKind
        {
            Ready,
            Running,
            Completed
        }
    }

    public class WasmStackFrame : StackFrame
    {
        private readonly Stack<int> labelDepths = new();
        private readonly Memory<StackValueItem> locals;
        private readonly Stack<StackValueItem> stack = new(); // TODO: use pool

        private bool isEnd;

        // program counter
        private uint pc;

        internal WasmStackFrame(ExecutionContext context, InstanceFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            Context = context;
            Function = function;

            var numParams = Function.function.Type.ParameterTypes.Length;
            if (numParams != inputValues.Length) throw new InvalidOperationException();

            var functionLocalTypes = Function.function.Body.Locals.Span;

            var numLocals = numParams;
            for (var i = 0; i < functionLocalTypes.Length; i++)
            {
                var functionLocal = functionLocalTypes[i];
                numLocals += (int)functionLocal.Count;
            }

            var locals = new StackValueItem[numLocals];
            var cursor = 0;

            for (var i = 0; i < numParams; i++) locals[cursor++] = inputValues[i];

            foreach (var functionLocal in functionLocalTypes)
                for (var i = 0; i < functionLocal.Count; i++)
                    locals[cursor++] = new StackValueItem(functionLocal.Type);

            this.locals = locals;
        }

        public ExecutionContext Context { get; }
        public InstanceFunction Function { get; }

        public Instance Instance => Function.instance;

        public override int ResultLength => Function.function.Type.ResultTypes.Length;

        public ref StackValueItem GetLocal(int index)
        {
            return ref locals.Span[index];
        }

        public override bool MoveNext()
        {
            if (isEnd) return true;

            var instrs = Function.function.Body.Instructions.Span;
            if (pc >= instrs.Length) return false;

            var instr = instrs[checked((int)pc)];

            instr.Execute(this);

            if (isEnd) return false;

            pc++;

            return pc < instrs.Length;
        }

        public override void TakeResults(Span<StackValueItem> dest)
        {
            // validate
            var resultTypes = Function.function.Type.ResultTypes;
            var resultLength = resultTypes.Length;
            if (resultLength > dest.Length) throw new ArgumentException();
            if (resultLength > stack.Count)
                throw new InvalidCodeException(
                    $"Number of results of current frame is {resultLength} but actual is {stack.Count}.");

            for (var i = resultLength - 1; i >= 0; i--)
            {
                var value = stack.Pop();
                var resultType = resultTypes.Span[i];
                if (!value.IsType(resultType)) throw new InvalidCodeException();
                dest[i] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(uint value)
        {
            stack.Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ulong value)
        {
            stack.Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(float value)
        {
            stack.Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(double value)
        {
            stack.Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackValueItem value)
        {
            stack.Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackValueItem Pop()
        {
            return stack.Pop();
        }

        public void Jump(uint index)
        {
            pc = index - 1;
        }

        public void End()
        {
            isEnd = true;
        }

        public void EnterBlock(Label label)
        {
            stack.Push(new StackValueItem(label));
            labelDepths.Push(stack.Count);
        }

        public void EndBlock()
        {
            if (labelDepths.Count == 0)
            {
                isEnd = true;
                return;
            }

            var numResults = stack.Count - labelDepths.Pop();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numResults];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = stack.Pop();

            var label = stack.Pop().ExpectLabel();

            if (Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            // push results
            for (var i = 0; i < tempResults.Length; i++) stack.Push(tempResults[i]);

            pc = blockInstr.End.Index;
        }

        public void JumpLabel(uint depth)
        {
            for (var i = 0; i < depth; i++) labelDepths.Pop();

            if (labelDepths.Count == 0)
            {
                isEnd = true;
                return;
            }

            var numValues = stack.Count - labelDepths.Pop();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numValues];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = stack.Pop();

            var label = stack.Pop().ExpectLabel();

            if (Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            var arity = blockInstr.Arity;

            if (numValues < arity) throw new InvalidCodeException();

            for (var i = 0; i < arity; i++) stack.Push(tempResults[^(int)(arity - i)]);

            pc = label.ContinuationIndex - 1;
        }
    }
}