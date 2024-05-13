using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class WasmStackFrame : StackFrame
    {
        private bool isDisposed;

        private bool isEnd;
        private int[] labelDepthStack;
        private int labelDepthStackPointer;

        private Memory<StackValueItem> locals;

        private uint programCounter;
        private Memory<StackValueItem> stack;
        private StackValueItem[] stackBuffer;
        private int stackPointer;

        internal WasmStackFrame(ExecutionContext context, InstanceFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            Context = context;
            Function = function;

            var numParams = Function.function.Type.ParameterTypes.Length;

            var functionLocalTypes = Function.function.Body.Locals.Span;

            var numLocals = numParams;
            for (var i = 0; i < functionLocalTypes.Length; i++)
            {
                var functionLocal = functionLocalTypes[i];
                numLocals += (int)functionLocal.Count;
            }

            // alloc

            var maxStackDepth = function.function.MaxStackDepth;
            if (!maxStackDepth.HasValue)
                throw new InvalidOperationException("This function is not properly validated.");
            var stackBufferSize = checked((int)(numLocals + maxStackDepth.Value));

            var maxLabelDepths = function.function.MaxBlockDepth;
            if (!maxLabelDepths.HasValue)
                throw new InvalidOperationException("This function is not properly validated.");

            stackBuffer = ArrayPool<StackValueItem>.Shared.Rent(stackBufferSize);
            labelDepthStack = ArrayPool<int>.Shared.Rent(checked((int)maxLabelDepths.Value));

            locals = stackBuffer.AsMemory(0, numLocals);
            stack = stackBuffer.AsMemory(numLocals, checked((int)maxStackDepth.Value));

            var localsSpan = locals.Span;

            var cursor = numParams;

            // initialize locals
            foreach (var functionLocal in functionLocalTypes)
                for (var i = 0; i < functionLocal.Count; i++)
                    localsSpan[cursor++] = new StackValueItem(functionLocal.Type);

            if (inputValues.Length != numParams) throw new ArgumentException(nameof(inputValues));
            inputValues.CopyTo(localsSpan.Slice(0, numParams));
        }

        public ExecutionContext Context { get; }
        public InstanceFunction Function { get; }

        public Instance Instance => Function.instance;

        public override int ResultLength => Function.function.Type.ResultTypes.Length;

        ~WasmStackFrame()
        {
            DisposeCore();
        }

        public override void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            if (isDisposed) return;

            if (stackBuffer != null) ArrayPool<StackValueItem>.Shared.Return(stackBuffer);
            stackBuffer = default;
            locals = default;
            stack = default;

            if (labelDepthStack != null) ArrayPool<int>.Shared.Return(labelDepthStack);
            labelDepthStack = null;

            isDisposed = true;
        }

        public ref StackValueItem GetLocal(int index)
        {
            return ref locals.Span[index];
        }

        public override bool MoveNext()
        {
            if (isEnd) return true;

            var instrs = Function.function.Body.Instructions.Span;
            if (programCounter >= instrs.Length) return false;

            var instr = instrs[checked((int)programCounter)];

            instr.Execute(this);

            if (isEnd) return false;

            programCounter++;

            return programCounter < instrs.Length;
        }

        public override void TakeResults(Span<StackValueItem> dest)
        {
            // validate
            var resultTypes = Function.function.Type.ResultTypes;
            var resultLength = resultTypes.Length;
            if (resultLength > dest.Length) throw new ArgumentException();
            if (resultLength > stackPointer)
                throw new InvalidCodeException(
                    $"Number of results of current frame is {resultLength} but actual is {stackPointer}.");

            for (var i = resultLength - 1; i >= 0; i--)
            {
                var value = Pop();
                var resultType = resultTypes.Span[i];
                if (!value.IsType(resultType)) throw new InvalidCodeException();
                dest[i] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(uint value)
        {
            Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ulong value)
        {
            Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(float value)
        {
            Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(double value)
        {
            Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackValueItem value)
        {
            stack.Span[checked(stackPointer++)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackValueItem Pop()
        {
            return stack.Span[checked(--stackPointer)];
        }

        private void PushLabelDepth(int depth)
        {
            labelDepthStack[checked(labelDepthStackPointer++)] = depth;
        }

        private int PopLabelDepth()
        {
            return labelDepthStack[checked(--labelDepthStackPointer)];
        }

        public void Jump(uint index)
        {
            programCounter = index - 1;
        }

        public void End()
        {
            isEnd = true;
        }

        public void EnterBlock(Label label)
        {
            Push(new StackValueItem(label));
            PushLabelDepth(stackPointer);
        }

        public void EndBlock()
        {
            if (labelDepthStackPointer == 0)
            {
                isEnd = true;
                return;
            }

            var numResults = stackPointer - PopLabelDepth();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numResults];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = Pop();

            var label = Pop().ExpectLabel();

            if (Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            // push results
            for (var i = 0; i < tempResults.Length; i++) Push(tempResults[i]);

            programCounter = blockInstr.End.Index;
        }

        public void JumpLabel(uint depth)
        {
            for (var i = 0; i < depth; i++) PopLabelDepth();

            if (labelDepthStackPointer == 0)
            {
                isEnd = true;
                return;
            }

            var numValues = stackPointer - PopLabelDepth();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numValues];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = Pop();

            var label = Pop().ExpectLabel();

            if (Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            var arity = blockInstr.Arity;

            if (numValues < arity) throw new InvalidCodeException();

            for (var i = 0; i < arity; i++) Push(tempResults[^(int)(arity - i)]);

            programCounter = label.ContinuationIndex - 1;
        }
    }
}