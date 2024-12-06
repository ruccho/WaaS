using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a stack frame of a WebAssembly function loaded from binary.
    /// </summary>
    public class WasmStackFrame : IStackFrameCore
    {
        [ThreadStatic] private static Stack<WasmStackFrame> pool;

        private bool isEnd;
        private bool isFramePushed;
        private int[] labelDepthStack;
        private int labelDepthStackPointer;
        private Memory<StackValueItem> locals;
        private uint programCounter;
        private Memory<StackValueItem> stack;
        private StackValueItem[] stackBuffer;
        private int stackPointer;
        private ExecutionContext Context { get; set; }
        public InstanceFunction Function { get; private set; }

        public Instance Instance => Function.instance;

        public ushort Version { get; private set; }

        public int GetResultLength(ushort version)
        {
            ThrowIfOutdated(version);
            return Function.function.Type.ResultTypes.Length;
        }

        public void Dispose(ushort version)
        {
            if (version != Version) return;

            if (stackBuffer != null) ArrayPool<StackValueItem>.Shared.Return(stackBuffer);

            if (labelDepthStack != null) ArrayPool<int>.Shared.Return(labelDepthStack);

            isEnd = default;
            isFramePushed = default;
            labelDepthStack = default;
            labelDepthStackPointer = default;
            locals = default;
            programCounter = default;
            stack = default;
            stackBuffer = default;
            stackPointer = default;
            Context = default;
            Function = default;

            if (++Version != ushort.MaxValue)
            {
                pool ??= new Stack<WasmStackFrame>();
                pool.Push(this);
            }
        }

        public StackFrameState MoveNext(ushort version, Waker waker)
        {
            ThrowIfOutdated(version);

            if (isEnd) return StackFrameState.Completed;

            var instrs = Function.function.Body.Instructions.Span;

            while (true)
            {
                if (programCounter >= instrs.Length) return StackFrameState.Completed;

                var instr = instrs[checked((int)programCounter)];

                isFramePushed = false;
                try
                {
                    instr.Execute(this);
                }
                catch (Exception ex) when (ex is not WasmException)
                {
                    throw new WasmException(Function, instr, innerException: ex);
                }

                programCounter++;

                if (isEnd || instrs.Length <= programCounter) return StackFrameState.Completed;

                if (isFramePushed) return StackFrameState.Ready;
            }
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
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

        public bool DoesTakeResults(ushort version)
        {
            ThrowIfOutdated(version);
            return true;
        }

        public void PushResults(ushort version, Span<StackValueItem> source)
        {
            ThrowIfOutdated(version);
            foreach (var value in source) Push(value);
        }

        public static WasmStackFrame Get(ExecutionContext context, InstanceFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            pool ??= new Stack<WasmStackFrame>();
            if (!pool.TryPop(out var pooled)) pooled = new WasmStackFrame();
            pooled.Reset(context, function, inputValues);
            return pooled;
        }

        private void Reset(ExecutionContext context, InstanceFunction function,
            ReadOnlySpan<StackValueItem> inputValues)
        {
            Context = context;
            Function = function;

            var parameters = Function.function.Type.ParameterTypes.Span;
            var numParams = parameters.Length;

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

            Array.Clear(stackBuffer, 0, stackBuffer.Length);
            Array.Clear(labelDepthStack, 0, labelDepthStack.Length);

            locals = stackBuffer.AsMemory(0, numLocals);
            stack = stackBuffer.AsMemory(numLocals, checked((int)maxStackDepth.Value));

            var localsSpan = locals.Span;

            var cursor = numParams;

            // initialize locals
            foreach (var functionLocal in functionLocalTypes)
                for (var i = 0; i < functionLocal.Count; i++)
                    localsSpan[cursor++] = new StackValueItem(functionLocal.Type);

            if (inputValues.Length != numParams) throw new ArgumentException(nameof(inputValues));
            for (var i = 0; i < inputValues.Length; i++)
                if (!inputValues[i].IsType(parameters[i]))
                    throw new ArgumentException(
                        $"signature mismatch. expected {function.Type} but got {inputValues[i]} at {i}");

            inputValues.CopyTo(localsSpan.Slice(0, numParams));
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }

        public ref StackValueItem GetLocal(int index)
        {
            return ref locals.Span[index];
        }

        public void End()
        {
            isEnd = true;
        }

        public void PushFrame(IInvocableFunction function, Span<StackValueItem> parameters)
        {
            isFramePushed = true;
            Context.PushFrame(function, parameters);
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