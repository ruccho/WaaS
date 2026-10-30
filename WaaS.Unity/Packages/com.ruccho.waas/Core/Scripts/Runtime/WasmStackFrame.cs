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
        // the buffer of a pooled frame is kept for the next use unless it is larger than this
        private const int MaxRetainedBufferLength = 256;

        [ThreadStatic] private static Stack<WasmStackFrame> pool;

        private CompiledFunction compiled;
        private IInvocableFunction[] functions;
        private Global[] globals;
        private bool isEnd;
        private bool isFramePushed;
        private Memory memory;
        private uint programCounter;

        // the number of the locals including the parameters. the operand stack starts here.
        private int stackBase;

        // index of the next slot of the operand stack in the values
        private int stackPointer;

        // the locals followed by the operand stack
        private StackValueItem[] values;

        private ExecutionContext Context { get; set; }
        public InstanceFunction Function { get; private set; }

        public Instance Instance => Function.instance;

        internal uint? MaxMemoryPages => Context.MaxMemoryPages;

        public ushort Version { get; private set; }

        public int GetResultLength(ushort version)
        {
            ThrowIfOutdated(version);
            return Function.function.Type.ResultTypes.Length;
        }

        public void Dispose(ushort version)
        {
            if (version != Version) return;

            if (values != null && values.Length > MaxRetainedBufferLength)
            {
                ArrayPool<StackValueItem>.Shared.Return(values);
                values = default;
            }

            compiled = default;
            functions = default;
            globals = default;
            isEnd = default;
            isFramePushed = default;
            memory = default;
            programCounter = default;
            stackBase = default;
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

            isFramePushed = false;

            // the interpreter loop is kept out of the exception handler for better code generation
            try
            {
                return Run();
            }
            catch (Exception ex) when (ex is not WasmException and not OperationCanceledException)
            {
                throw new WasmException(Function, GetCurrentInstruction(true), innerException: ex);
            }
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            // validate
            var resultTypes = Function.function.Type.ResultTypes;
            var resultLength = resultTypes.Length;
            if (resultLength > dest.Length) throw new ArgumentException();
            if (resultLength > stackPointer - stackBase)
                throw new InvalidCodeException(
                    $"Number of results of current frame is {resultLength} but actual is {stackPointer - stackBase}.");

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

            compiled = function.function.Compiled ??
                       throw new InvalidOperationException("This function is not properly validated.");

            var parameters = function.function.Type.ParameterTypes.Span;
            if (inputValues.Length != parameters.Length) throw new ArgumentException(nameof(inputValues));
            for (var i = 0; i < inputValues.Length; i++)
                if (!inputValues[i].IsType(parameters[i]))
                    throw new ArgumentException(
                        $"signature mismatch. expected {function.Type} but got {inputValues[i]} at {i}");

            var initialLocals = compiled.InitialLocals;
            var bufferLength = checked(initialLocals.Length + compiled.MaxStackDepth);

            if (values == null || values.Length < bufferLength)
            {
                if (values != null && values.Length > MaxRetainedBufferLength)
                    ArrayPool<StackValueItem>.Shared.Return(values);
                values = bufferLength > MaxRetainedBufferLength
                    ? ArrayPool<StackValueItem>.Shared.Rent(bufferLength)
                    : new StackValueItem[Math.Max(bufferLength, 16)];
            }

            // the operand stack is always written before read and need not be cleared
            initialLocals.AsSpan().CopyTo(values);
            inputValues.CopyTo(values);

            stackBase = initialLocals.Length;
            stackPointer = stackBase;

            var instance = function.instance;
            functions = instance.FunctionInstance.FunctionArray;
            globals = instance.GlobalInstance.GlobalArray;
            var memories = instance.MemoryInstance.MemoryArray;
            memory = memories.Length > 0 ? memories[0] : null;
        }

        /// <summary>
        ///     Gets the instruction being executed. Used to build the stack trace on failure.
        /// </summary>
        /// <param name="isInnermost">
        ///     Whether the frame is the innermost one. Otherwise, the frame is suspended after the instruction which
        ///     pushed the next frame, and the program counter already points to the instruction after it.
        /// </param>
        internal Instruction GetCurrentInstruction(bool isInnermost)
        {
            if (Function == null) return null;
            var instrs = Function.function.Body.Instructions.Span;
            if (instrs.IsEmpty) return null;

            var index = (long)programCounter;
            if (!isInnermost && isFramePushed) index--;
            if (index < 0) index = 0;
            if (index >= instrs.Length) index = instrs.Length - 1;
            return instrs[(int)index];
        }

        private void ThrowIfOutdated(ushort version)
        {
            if (version != Version) throw new InvalidOperationException();
        }

        public ref StackValueItem GetLocal(int index)
        {
            if ((uint)index >= (uint)stackBase) throw new ArgumentOutOfRangeException(nameof(index));
            return ref values[index];
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
        public void Push(V128 value)
        {
            Push(new StackValueItem(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackValueItem value)
        {
            values[stackPointer++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackValueItem Pop()
        {
            if (stackPointer <= stackBase) ThrowStackUnderflow();
            return values[--stackPointer];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowStackUnderflow()
        {
            throw new InvalidCodeException("stack underflow");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfBounds()
        {
            throw new TrapException("out of bounds memory access");
        }

        // moves the values a branch carries to the target height and returns the new stack pointer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MoveBranchValues(StackValueItem[] values, int sp, int height, int arity)
        {
            if (arity == 0) return height;
            var source = sp - arity;
            if (source != height)
            {
                if (arity == 1)
                    values[height] = values[source];
                else
                    Array.Copy(values, source, values, height, arity);
            }

            return height + arity;
        }

        /// <summary>
        ///     The interpreter loop. Runs until the function ends or a frame is pushed.
        ///     <see cref="programCounter" /> is updated before each instruction so that it points to the failed one.
        /// </summary>
        private StackFrameState Run()
        {
            var context = Context;
            var code = compiled.Code;
            var values = this.values;
            var stackBase = this.stackBase;
            var sp = stackPointer;
            var pc = (int)programCounter;

            while (true)
            {
                programCounter = (uint)pc;
                if (--context.fuel < 0) context.OnFuelExhausted();

                ref var instr = ref code[pc];
                switch (instr.Op)
                {
                    case Op.Nop:
                        break;
                    case Op.Return:
                        isEnd = true;
                        stackPointer = sp;
                        return StackFrameState.Completed;

                    #region Control

                    case Op.If:
                        if (values[--sp].valueI32 == 0)
                        {
                            pc = instr.A;
                            continue;
                        }

                        break;
                    case Op.Jump:
                        pc = instr.A;
                        continue;
                    case Op.BrLoop:
                        // every loop iteration takes a branch, so this is enough to abort a running loop
                        context.CancellationToken.ThrowIfCancellationRequested();
                        sp = MoveBranchValues(values, sp, stackBase + instr.B, instr.C);
                        pc = instr.A;
                        continue;
                    case Op.Br:
                        sp = MoveBranchValues(values, sp, stackBase + instr.B, instr.C);
                        pc = instr.A;
                        continue;
                    case Op.BrIfLoop:
                        if (values[--sp].valueI32 != 0)
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            sp = MoveBranchValues(values, sp, stackBase + instr.B, instr.C);
                            pc = instr.A;
                            continue;
                        }

                        break;
                    case Op.BrIf:
                        if (values[--sp].valueI32 != 0)
                        {
                            sp = MoveBranchValues(values, sp, stackBase + instr.B, instr.C);
                            pc = instr.A;
                            continue;
                        }

                        break;
                    case Op.ReturnIf:
                        if (values[--sp].valueI32 != 0)
                        {
                            isEnd = true;
                            stackPointer = sp;
                            return StackFrameState.Completed;
                        }

                        break;
                    case Op.BrTable:
                    {
                        var i = values[--sp].valueI32;
                        ref var target = ref compiled.BranchTargets[instr.A + (i < (uint)instr.B ? (int)i : instr.B)];
                        if (target.Index < 0)
                        {
                            isEnd = true;
                            stackPointer = sp;
                            return StackFrameState.Completed;
                        }

                        if (target.IsLoop) context.CancellationToken.ThrowIfCancellationRequested();
                        sp = MoveBranchValues(values, sp, stackBase + target.Height, target.Arity);
                        pc = target.Index;
                        continue;
                    }
                    case Op.Call:
                    {
                        sp -= instr.B;
                        stackPointer = sp;
                        isFramePushed = true;
                        context.PushFrame(functions[instr.A], new ReadOnlySpan<StackValueItem>(values, sp, instr.B));
                        programCounter = (uint)(pc + 1);
                        return StackFrameState.Ready;
                    }

                    #endregion

                    #region Parametric

                    case Op.Drop:
                        sp--;
                        break;
                    case Op.Select:
                        if (values[sp - 1].valueI32 == 0) values[sp - 3] = values[sp - 2];
                        sp -= 2;
                        break;

                    #endregion

                    #region Variable

                    case Op.LocalGet:
                        values[sp] = values[instr.A];
                        sp++;
                        break;
                    case Op.LocalSet:
                        values[instr.A] = values[--sp];
                        break;
                    case Op.LocalTee:
                        values[instr.A] = values[sp - 1];
                        break;
                    case Op.GlobalGet:
                        values[sp] = globals[instr.A].stackValue;
                        sp++;
                        break;
                    case Op.GlobalSet:
                        globals[instr.A].stackValue = values[--sp];
                        break;

                    #endregion

                    #region Numeric

                    case Op.Const:
                    {
                        ref var slot = ref values[sp++];
                        slot.valueType = (ValueType)instr.A;
                        slot.valueI64 = instr.Value;
                        break;
                    }

                    case Op.I32Eqz:
                    {
                        ref var slot = ref values[sp - 1];
                        slot.valueI64 = slot.valueI32 == 0 ? 1UL : 0UL;
                        break;
                    }
                    case Op.I32Eq:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 == values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32Ne:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 != values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LtS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (int)lhs.valueI32 < (int)values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LtU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 < values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GtS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (int)lhs.valueI32 > (int)values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GtU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 > values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LeS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (int)lhs.valueI32 <= (int)values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LeU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 <= values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GeS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (int)lhs.valueI32 >= (int)values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GeU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 >= values[sp - 1].valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }

                    case Op.I64Eqz:
                    {
                        ref var slot = ref values[sp - 1];
                        slot.valueI64 = slot.valueI64 == 0 ? 1UL : 0UL;
                        slot.valueType = ValueType.I32;
                        break;
                    }
                    case Op.I64Eq:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 == values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64Ne:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 != values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LtS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (long)lhs.valueI64 < (long)values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LtU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 < values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GtS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (long)lhs.valueI64 > (long)values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GtU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 > values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LeS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (long)lhs.valueI64 <= (long)values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LeU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 <= values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GeS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (long)lhs.valueI64 >= (long)values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GeU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI64 >= values[sp - 1].valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }

                    case Op.I32Add:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 + values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Sub:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 - values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Mul:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 * values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32And:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 & values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Or:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 | values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Xor:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 ^ values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Shl:
                    {
                        // the shift count is masked to 5 bits as specified
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 << (int)values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32ShrS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (uint)((int)lhs.valueI32 >> (int)values[sp - 1].valueI32);
                        sp--;
                        break;
                    }
                    case Op.I32ShrU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = lhs.valueI32 >> (int)values[sp - 1].valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Rotl:
                    {
                        ref var lhs = ref values[sp - 2];
                        var value = lhs.valueI32;
                        var k = (int)values[sp - 1].valueI32;
                        lhs.valueI64 = (value << k) | (value >> (32 - k));
                        sp--;
                        break;
                    }
                    case Op.I32Rotr:
                    {
                        ref var lhs = ref values[sp - 2];
                        var value = lhs.valueI32;
                        var k = (int)values[sp - 1].valueI32;
                        lhs.valueI64 = (value >> k) | (value << (32 - k));
                        sp--;
                        break;
                    }

                    case Op.I64Add:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 += values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Sub:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 -= values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Mul:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 *= values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64And:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 &= values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Or:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 |= values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Xor:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 ^= values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Shl:
                    {
                        // the shift count is masked to 6 bits as specified
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 <<= (int)values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64ShrS:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 = (ulong)((long)lhs.valueI64 >> (int)values[sp - 1].valueI64);
                        sp--;
                        break;
                    }
                    case Op.I64ShrU:
                    {
                        ref var lhs = ref values[sp - 2];
                        lhs.valueI64 >>= (int)values[sp - 1].valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Rotl:
                    {
                        ref var lhs = ref values[sp - 2];
                        var value = lhs.valueI64;
                        var k = (int)values[sp - 1].valueI64;
                        lhs.valueI64 = (value << k) | (value >> (64 - k));
                        sp--;
                        break;
                    }
                    case Op.I64Rotr:
                    {
                        ref var lhs = ref values[sp - 2];
                        var value = lhs.valueI64;
                        var k = (int)values[sp - 1].valueI64;
                        lhs.valueI64 = (value >> k) | (value << (64 - k));
                        sp--;
                        break;
                    }

                    case Op.I32WrapI64:
                    {
                        ref var slot = ref values[sp - 1];
                        slot.valueI64 = slot.valueI32;
                        slot.valueType = ValueType.I32;
                        break;
                    }
                    case Op.I64ExtendI32S:
                    {
                        ref var slot = ref values[sp - 1];
                        slot.valueI64 = (ulong)(int)slot.valueI32;
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64ExtendI32U:
                    {
                        ref var slot = ref values[sp - 1];
                        slot.valueI64 = slot.valueI32;
                        slot.valueType = ValueType.I64;
                        break;
                    }

                    #endregion

                    #region Memory

                    case Op.Load32:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 4 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = Unsafe.ReadUnaligned<uint>(ref memory.Buffer[(int)address]);
                        slot.valueType = (ValueType)instr.B;
                        break;
                    }
                    case Op.Load64:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 8 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = Unsafe.ReadUnaligned<ulong>(ref memory.Buffer[(int)address]);
                        slot.valueType = (ValueType)instr.B;
                        break;
                    }
                    case Op.I32Load8S:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 1 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = (uint)(sbyte)memory.Buffer[(int)address];
                        break;
                    }
                    case Op.I32Load8U:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 1 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = memory.Buffer[(int)address];
                        break;
                    }
                    case Op.I32Load16S:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 2 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = (uint)Unsafe.ReadUnaligned<short>(ref memory.Buffer[(int)address]);
                        break;
                    }
                    case Op.I32Load16U:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 2 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = Unsafe.ReadUnaligned<ushort>(ref memory.Buffer[(int)address]);
                        break;
                    }
                    case Op.I64Load8S:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 1 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = (ulong)(sbyte)memory.Buffer[(int)address];
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load8U:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 1 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = memory.Buffer[(int)address];
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load16S:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 2 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = (ulong)Unsafe.ReadUnaligned<short>(ref memory.Buffer[(int)address]);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load16U:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 2 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = Unsafe.ReadUnaligned<ushort>(ref memory.Buffer[(int)address]);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load32S:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 4 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = (ulong)Unsafe.ReadUnaligned<int>(ref memory.Buffer[(int)address]);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load32U:
                    {
                        ref var slot = ref values[sp - 1];
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 4 > (ulong)memory.Length) ThrowOutOfBounds();
                        slot.valueI64 = Unsafe.ReadUnaligned<uint>(ref memory.Buffer[(int)address]);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.Store8:
                    {
                        var address = (ulong)values[sp - 2].valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 1 > (ulong)memory.Length) ThrowOutOfBounds();
                        memory.Buffer[(int)address] = (byte)values[sp - 1].valueI32;
                        sp -= 2;
                        break;
                    }
                    case Op.Store16:
                    {
                        var address = (ulong)values[sp - 2].valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 2 > (ulong)memory.Length) ThrowOutOfBounds();
                        Unsafe.WriteUnaligned(ref memory.Buffer[(int)address], (ushort)values[sp - 1].valueI32);
                        sp -= 2;
                        break;
                    }
                    case Op.Store32:
                    {
                        var address = (ulong)values[sp - 2].valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 4 > (ulong)memory.Length) ThrowOutOfBounds();
                        Unsafe.WriteUnaligned(ref memory.Buffer[(int)address], values[sp - 1].valueI32);
                        sp -= 2;
                        break;
                    }
                    case Op.Store64:
                    {
                        var address = (ulong)values[sp - 2].valueI32 + (uint)instr.A;
                        var memory = this.memory;
                        if (address + 8 > (ulong)memory.Length) ThrowOutOfBounds();
                        Unsafe.WriteUnaligned(ref memory.Buffer[(int)address], values[sp - 1].valueI64);
                        sp -= 2;
                        break;
                    }

                    #endregion

                    default:
                    {
                        // instructions without a dedicated operation
                        stackPointer = sp;
                        Function.function.Body.Instructions.Span[pc].Execute(this);
                        sp = stackPointer;

                        if (isFramePushed)
                        {
                            programCounter = (uint)(pc + 1);
                            return StackFrameState.Ready;
                        }

                        if (isEnd) return StackFrameState.Completed;
                        break;
                    }
                }

                pc++;
            }
        }
    }
}
