using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        private StackFrame? firstPushedFrame;

        public Instance Instance => Function.instance;
        internal ExecutionContext Context { get; private set; }
        public InstanceFunction Function { get; private set; }

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
            stackBuffer = default;
            locals = default;
            stack = default;

            if (labelDepthStack != null) ArrayPool<int>.Shared.Return(labelDepthStack);
            labelDepthStack = default;

            Context = default;
            Function = default;
            isEnd = default;
            programCounter = default;
            stackPointer = default;
            labelDepthStackPointer = default;
            singleResult = default;
            results = default;
            firstPushedFrame = default;

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
            if (firstPushedFrame is { } frame)
            {
                Context.PushFrame(frame);
                firstPushedFrame = default;
                return StackFrameState.Ready;
            }

            var stack = this.stack.Span;
            var transient = new TransientWasmStackFrame(this, locals.Span, stack, labelDepthStack);

            if (transient.MoveNext(out var pushedFrame))
            {
                isEnd = true;
                FlushResults(stack);
                return StackFrameState.Completed;
            }

            Context.PushFrame(pushedFrame.Value);
            return StackFrameState.Ready;
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            // validate
            if (singleResult.HasValue)
            {
                if (dest.Length != 1) throw new InvalidOperationException();
                dest[0] = singleResult.Value;
            }
            else if (results != null)
            {
                if (results.Length != dest.Length) throw new InvalidOperationException();
                results.CopyTo(dest);
            }
            else if (dest.Length != 0)
            {
                throw new InvalidOperationException();
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
            source.CopyTo(stack.Span[stackPointer..]);
            stackPointer += source.Length;
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
            foreach (var functionLocal in functionLocalTypes)
            {
                numLocals += (int)functionLocal.Count;
            }

            var maxStackDepth = function.function.MaxStackDepth;
            if (!maxStackDepth.HasValue)
                throw new InvalidOperationException("This function is not properly validated.");

            var maxLabelDepths = function.function.MaxBlockDepth;
            if (!maxLabelDepths.HasValue)
                throw new InvalidOperationException("This function is not properly validated.");

            if (inputValues.Length != numParams) throw new ArgumentException(nameof(inputValues));
            for (var i = 0; i < inputValues.Length; i++)
                if (!inputValues[i].IsType(parameters[i]))
                    throw new ArgumentException(
                        $"signature mismatch. expected {function.Type} but got {inputValues[i]} at {i}");

            // Preemptive Moving
            // We run the first MoveNext() here preemptively until it is suspended.
            // If it reaches the end without suspension, we can forget input values, stack and locals immediately.

            // TODO: use pooled arrays for recursive calls to avoid stack overflow

            Span<StackValueItem> locals = stackalloc StackValueItem[numLocals];
            Span<StackValueItem> stack = stackalloc StackValueItem[checked((int)maxStackDepth.Value)];
            Span<int> labelDepthStack = stackalloc int[checked((int)maxLabelDepths.Value)];
            var cursor = numParams;
            foreach (var functionLocal in functionLocalTypes)
                for (var i = 0; i < functionLocal.Count; i++)
                    locals[cursor++] = new StackValueItem(functionLocal.Type);

            if (inputValues.Length != numParams) throw new ArgumentException(nameof(inputValues));
            for (var i = 0; i < inputValues.Length; i++)
                if (!inputValues[i].IsType(parameters[i]))
                    throw new ArgumentException(
                        $"signature mismatch. expected {function.Type} but got {inputValues[i]} at {i}");

            inputValues.CopyTo(locals[..numParams]);

            var transient = new TransientWasmStackFrame(this, locals, stack, labelDepthStack);

            if (transient.MoveNext(out var pushedFrame))
            {
                isEnd = true;
                FlushResults(stack);
            }
            else
            {
                firstPushedFrame = pushedFrame.Value;

                // copy transient state to heap

                stackBuffer = ArrayPool<StackValueItem>.Shared.Rent(locals.Length + stack.Length);
                this.locals = stackBuffer.AsMemory(0, locals.Length);
                this.stack = stackBuffer.AsMemory(locals.Length, stack.Length);
                locals.CopyTo(this.locals.Span);
                stack.CopyTo(this.stack.Span);

                this.labelDepthStack = ArrayPool<int>.Shared.Rent(labelDepthStack.Length);
                labelDepthStack.CopyTo(this.labelDepthStack);
            }
        }

        private void FlushResults(Span<StackValueItem> stack)
        {
            var resultTypes = Function.function.Type.ResultTypes.Span;
            var resultLength = resultTypes.Length;
            if (resultLength > stackPointer)
                throw new InvalidCodeException(
                    $"Number of results of current frame is {resultLength} but actual is {stackPointer}.");

            // copy results

            switch (resultLength)
            {
                case 0:
                {
                    singleResult = default;
                    results = default;
                    break;
                }
                case 1:
                {
                    if (!stack[stackPointer - 1].IsType(resultTypes[0])) throw new InvalidCodeException($"Result type mismatch. Expected {resultTypes[0]} but got {stack[0]}.");
                    singleResult = stack[stackPointer - 1];
                    results = default;
                    break;
                }
                default:
                {
                    for (var i = 0; i < resultLength; i++)
                        if (!stack[stackPointer - i].IsType(resultTypes[i]))
                            throw new InvalidCodeException();

                    singleResult = default;
                    results = ArrayPool<StackValueItem>.Shared.Rent(resultLength);
                    stack.Slice(stackPointer - resultLength, resultLength).CopyTo(results);
                    break;
                }
            }
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }

        #region open fields

        internal bool isEnd;
        internal uint programCounter;
        internal int stackPointer;
        internal int labelDepthStackPointer;

        #endregion

        #region state

        private StackValueItem[] stackBuffer;
        private Memory<StackValueItem> locals;
        private Memory<StackValueItem> stack;
        private int[] labelDepthStack;

        #endregion

        #region results

        private StackValueItem? singleResult;
        private StackValueItem[] results;

        #endregion
    }

    public readonly ref struct TransientWasmStackFrame
    {
        private readonly WasmStackFrame frame;
        private readonly Span<StackValueItem> locals;
        private readonly Span<StackValueItem> stack;
        private readonly Span<int> labelDepthStack;

        public Instance Instance => frame.Instance;

        public TransientWasmStackFrame(WasmStackFrame frame, Span<StackValueItem> locals, Span<StackValueItem> stack,
            Span<int> labelDepthStack) : this()
        {
            this.frame = frame;
            this.locals = locals;
            this.stack = stack;
            this.labelDepthStack = labelDepthStack;
        }

        public bool MoveNext([NotNullWhen(false)] out StackFrame? pushedFrame)
        {
            var instrs = frame.Function.function.Body.Instructions.Span;
            pushedFrame = default;

            while (true)
            {
                if (frame.programCounter >= instrs.Length) return true;

                var instr = instrs[checked((int)frame.programCounter)];

                try
                {
                    instr.Execute(in this, ref pushedFrame);
                }
                catch (Exception ex) when (ex is not WasmException)
                {
                    throw new WasmException(frame.Function, instr, innerException: ex);
                }

                frame.programCounter++;

                if (frame.isEnd || instrs.Length <= frame.programCounter) return true;

                if (pushedFrame.HasValue) return false; // suspend
            }
        }

        public void End()
        {
            frame.isEnd = true;
        }

        public StackFrame CreateFrame(IInvocableFunction function, Span<StackValueItem> parameters)
        {
            return function.CreateFrame(frame.Context, parameters);
        }

        public void Jump(uint index)
        {
            frame.programCounter = index - 1;
        }

        public void EnterBlock(Label label)
        {
            Push(new StackValueItem(label));
            PushLabelDepth(frame.stackPointer);
        }

        public void EndBlock()
        {
            if (frame.labelDepthStackPointer == 0)
            {
                frame.isEnd = true;
                return;
            }

            var numResults = frame.stackPointer - PopLabelDepth();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numResults];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = Pop();

            var label = Pop().ExpectLabel();

            if (frame.Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            // push results
            for (var i = 0; i < tempResults.Length; i++) Push(tempResults[i]);

            frame.programCounter = blockInstr.End.Index;
        }

        public void JumpLabel(uint depth)
        {
            for (var i = 0; i < depth; i++) PopLabelDepth();

            if (frame.labelDepthStackPointer == 0)
            {
                frame.isEnd = true;
                return;
            }

            var numValues = frame.stackPointer - PopLabelDepth();

            Span<StackValueItem> tempResults = stackalloc StackValueItem[numValues];

            for (var i = 0; i < tempResults.Length; i++) tempResults[^(i + 1)] = Pop();

            var label = Pop().ExpectLabel();

            if (frame.Function.function.Body.Instructions.Span[checked((int)label.BlockInstructionIndex)] is not
                BlockInstruction
                blockInstr)
                throw new InvalidCodeException();

            var arity = blockInstr.Arity;

            if (numValues < arity) throw new InvalidCodeException();

            for (var i = 0; i < arity; i++) Push(tempResults[^(int)(arity - i)]);

            frame.programCounter = label.ContinuationIndex - 1;
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
            stack[checked(frame.stackPointer++)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackValueItem Pop()
        {
            return stack[checked(--frame.stackPointer)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushLabelDepth(int depth)
        {
            labelDepthStack[checked(frame.labelDepthStackPointer++)] = depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PopLabelDepth()
        {
            return labelDepthStack[checked(--frame.labelDepthStackPointer)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StackValueItem GetLocal(int index)
        {
            return ref locals[index];
        }
    }
}