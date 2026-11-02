using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a stack frame of a WebAssembly function loaded from binary.
    ///     The calls from it to WebAssembly functions are also executed in the frame without pushing new frames, and the
    ///     members such as <see cref="Function" /> reflect the innermost function being executed.
    /// </summary>
    public partial class WasmStackFrame : IStackFrameCore
    {
        // the buffer of a pooled frame is kept for the next use unless it is larger than this
        private const int MaxRetainedBufferLength = 1024;

        [ThreadStatic] private static Stack<WasmStackFrame> pool;

        // the callers of the function being executed
        private int activationCount;
        private Activation[] activations;
        private int usedActivationCount;

        // the host function being called without a frame
        private IInvocableFunction directCallee;

        private CompiledFunction compiled;
        private Instance currentInstance;
        private IInvocableFunction[] functions;
        private Global[] globals;
        private bool isEnd;
        private bool isFramePushed;

        // whether the frame pushed by this frame is being moved, whose exceptions are not of this frame
        private bool isMovingPushedFrame;
        private Memory memory;
        private uint programCounter;

        // the function this frame is created for
        private InstanceFunction rootFunction;
        private Table[] tables;

        // index of the locals of the function being executed in the values
        private int frameBase;

        // index of the operand stack of the function being executed in the values, which follows the locals
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
            return rootFunction.function.Type.ResultTypes.Length;
        }

        public void Dispose(ushort version)
        {
            if (version != Version) return;

            if (values != null && values.Length > MaxRetainedBufferLength)
            {
                ArrayPool<StackValueItem>.Shared.Return(values);
                values = default;
            }

            if (usedActivationCount > 0) Array.Clear(activations, 0, usedActivationCount);

            activationCount = default;
            usedActivationCount = default;
            directCallee = default;
            compiled = default;
            currentInstance = default;
            functions = default;
            globals = default;
            isEnd = default;
            isFramePushed = default;
            memory = default;
            programCounter = default;
            frameBase = default;
            rootFunction = default;
            stackBase = default;
            stackPointer = default;
            tables = default;
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
                // a debugger pauses before each instruction and reads the operand stack
                return Context.Debugger != null ? Run(waker) : RunRegister(waker);
            }
            catch (DebuggerPause)
            {
                // resumed from the instruction
                return StackFrameState.Pending;
            }
            catch (Exception ex) when (ex is not WasmException and not OperationCanceledException &&
                                       !isMovingPushedFrame)
            {
                throw new WasmException(Function, GetCurrentInstruction(true), innerException: ex);
            }
            finally
            {
                isMovingPushedFrame = false;
            }
        }

        public void TakeResults(ushort version, Span<StackValueItem> dest)
        {
            ThrowIfOutdated(version);
            // validate
            var resultTypes = rootFunction.function.Type.ResultTypes;
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
            rootFunction = function;

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

            frameBase = 0;
            stackBase = initialLocals.Length;
            stackPointer = stackBase;

            SetInstance(function.instance);
        }

        private void SetInstance(Instance instance)
        {
            currentInstance = instance;
            functions = instance.FunctionInstance.FunctionArray;
            globals = instance.GlobalInstance.GlobalArray;
            tables = instance.TableInstance.TableArray;
            var memories = instance.MemoryInstance.MemoryArray;
            memory = memories.Length > 0 ? memories[0] : null;
        }

        /// <summary>
        ///     Starts executing the function called from the current function.
        /// </summary>
        /// <param name="callee">The function to call.</param>
        /// <param name="returnIndex">The instruction index of the current function to continue after the call.</param>
        /// <param name="frameBase">
        ///     The index of the parameters on the operand stack, where the locals of the callee start.
        /// </param>
        private void EnterFunction(InstanceFunction callee, int returnIndex, int frameBase)
        {
            var calleeCompiled = callee.function.Compiled;
            var initialLocals = calleeCompiled.InitialLocals;
            var numParameters = calleeCompiled.NumParameters;
            var calleeStackBase = frameBase + initialLocals.Length;

            var values = this.values;
            var requiredLength = calleeStackBase + calleeCompiled.MaxStackDepth;
            if (values.Length < requiredLength) values = GrowValues(requiredLength, frameBase + numParameters);

            // the parameters are already in place
            if (numParameters < initialLocals.Length)
            {
                ref var destination = ref values[frameBase];
                ref var source = ref initialLocals[0];
                for (var i = numParameters; i < initialLocals.Length; i++)
                    Unsafe.Add(ref destination, i) = Unsafe.Add(ref source, i);
            }

            if (activations == null)
                activations = new Activation[16];
            else if (activationCount == activations.Length)
                Array.Resize(ref activations, activations.Length * 2);

            ref var caller = ref activations[activationCount++];
            if (activationCount > usedActivationCount) usedActivationCount = activationCount;
            caller.Function = Function;
            caller.ReturnIndex = returnIndex;
            caller.FrameBase = this.frameBase;
            caller.StackBase = stackBase;

            Function = callee;
            compiled = calleeCompiled;
            this.frameBase = frameBase;
            stackBase = calleeStackBase;
            if (callee.instance != currentInstance) SetInstance(callee.instance);
        }

        /// <summary>
        ///     Returns from the function called in this frame to the caller.
        /// </summary>
        /// <returns>The instruction index of the caller to continue. The stack pointer is set to the field.</returns>
        private int ReturnToCaller(int sp)
        {
            stackPointer = MoveBranchValues(ref values[0], sp, frameBase, compiled.NumResults);

            ref var caller = ref activations[--activationCount];
            // the references are kept until the frame is disposed
            var function = caller.Function;
            Function = function;
            compiled = function.function.Compiled;
            frameBase = caller.FrameBase;
            stackBase = caller.StackBase;

            if (function.instance != currentInstance) SetInstance(function.instance);
            return caller.ReturnIndex;
        }

        private StackValueItem[] GrowValues(int requiredLength, int usedLength)
        {
            var oldValues = values;
            var length = Math.Max(requiredLength, oldValues.Length * 2);
            var newValues = length > MaxRetainedBufferLength
                ? ArrayPool<StackValueItem>.Shared.Rent(length)
                : new StackValueItem[length];
            Array.Copy(oldValues, newValues, usedLength);
            if (oldValues.Length > MaxRetainedBufferLength) ArrayPool<StackValueItem>.Shared.Return(oldValues);
            values = newValues;
            return newValues;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IInvocableFunction GetIndirectCallee(uint functionTypeIndex, uint tableIndex, uint functionIndex)
        {
            if (tables[tableIndex] is not Table<IInvocableFunction> table) throw new InvalidOperationException();

            var functionType = currentInstance.Module.TypeSection.FuncTypes.Span[checked((int)functionTypeIndex)];

            if (functionIndex >= table.Size) throw new TrapException("undefined element");
            var function = table.Elements[(int)functionIndex];
            if (function == null)
                throw new TrapException($"[{nameof(CallIndirect)}] Function is null. index: {functionIndex}");

            if (!function.Type.Match(functionType))
                throw new TrapException($"[{nameof(CallIndirect)}] Function type mismatch.");

            return function;
        }

        /// <summary>
        ///     Adds the frames of the functions being executed in this frame to the stack trace, from the innermost one.
        /// </summary>
        internal void CaptureStackTrace(List<WasmStackTraceFrame> dest, bool isInnermost)
        {
            // it stays set when the function throws
            if (directCallee != null) dest.Add(WasmStackTraceFrame.CaptureHost(directCallee));

            dest.Add(WasmStackTraceFrame.CaptureWasm(Function, GetCurrentInstruction(isInnermost)));
            for (var i = activationCount - 1; i >= 0; i--)
            {
                ref var caller = ref activations[i];
                var instrs = caller.Function.function.Body.Instructions.Span;
                // the caller is suspended after the call instruction
                dest.Add(WasmStackTraceFrame.CaptureWasm(caller.Function, instrs[caller.ReturnIndex - 1]));
            }
        }

        internal int ProgramCounter => (int)programCounter;
        internal int ActivationCount => activationCount;

        /// <summary>
        ///     Adds the functions being executed in this frame to the frames of a debugger, from the innermost one.
        /// </summary>
        internal void CaptureDebugFrames(List<WasmDebugFrame> dest, bool isInnermost)
        {
            var calleeFrameBase = frameBase;
            dest.Add(new WasmDebugFrame(Function, GetCurrentInstruction(isInnermost),
                values.AsSpan(frameBase, stackBase - frameBase).ToArray(),
                values.AsSpan(stackBase, stackPointer - stackBase).ToArray()));
            for (var i = activationCount - 1; i >= 0; i--)
            {
                ref var caller = ref activations[i];
                var instrs = caller.Function.function.Body.Instructions.Span;
                // the operand stack of the caller ends at the parameters passed to the callee, which are its locals
                dest.Add(new WasmDebugFrame(caller.Function, instrs[caller.ReturnIndex - 1],
                    values.AsSpan(caller.FrameBase, caller.StackBase - caller.FrameBase).ToArray(),
                    values.AsSpan(caller.StackBase, calleeFrameBase - caller.StackBase).ToArray()));
                calleeFrameBase = caller.FrameBase;
            }
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
            if ((uint)index >= (uint)(stackBase - frameBase)) throw new ArgumentOutOfRangeException(nameof(index));
            return ref values[frameBase + index];
        }

        public void End()
        {
            isEnd = true;
        }

        public void PushFrame(IInvocableFunction function, Span<StackValueItem> parameters)
        {
            isFramePushed = true;
            Context.PushFrameFromFrame(function, parameters);
        }

        /// <summary>
        ///     Moves the frame pushed by this frame, and passes its results to this frame if it completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private StackFrameState MovePushedFrame(StackFrame frame, in Waker waker)
        {
            StackFrameState state;
            isMovingPushedFrame = true;
            if (frame.Core is WasmStackFrame)
            {
                // nested frames of WebAssembly are left to the execution context not to grow the native stack
                if (!Context.TryEnterInlineFrame())
                {
                    isMovingPushedFrame = false;
                    return StackFrameState.Ready;
                }

                try
                {
                    state = frame.MoveNext(waker);
                }
                finally
                {
                    Context.ExitInlineFrame();
                }
            }
            else
            {
                state = frame.MoveNext(waker);
            }

            if (state != StackFrameState.Completed) return state;

            Context.CompleteFrame(frame, this);
            isMovingPushedFrame = false;
            return StackFrameState.Completed;
        }

        /// <summary>
        ///     Calls a host function that does not need a frame, without going through the stack of the frames.
        ///     <see cref="programCounter" /> stays at the call so that the stack trace of an exception points to it.
        /// </summary>
        /// <param name="callee">The function to call.</param>
        /// <param name="parametersIndex">The index of the parameters, which is where the results are written.</param>
        /// <param name="numParameters">The number of the parameters.</param>
        /// <param name="pendingCore">The frame to wait for the function when it has been started but is not completed.</param>
        /// <returns>Whether the function has been called and is completed.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryInvokeDirectly(IInvocableFunction callee, int parametersIndex, int numParameters,
            out IStackFrameCore pendingCore)
        {
            pendingCore = null;
            var parameters = new ReadOnlySpan<StackValueItem>(values, parametersIndex, numParameters);

            // an exception of the host is not an error at the instruction
            if (callee is ExternalFunction external)
            {
                var numResults = external.Type.ResultTypes.Length;
                var results = numResults <= MaxInlineResults
                    ? stackalloc StackValueItem[numResults]
                    : new StackValueItem[numResults];

                isMovingPushedFrame = true;
                directCallee = callee;
                external.Invoke(Context, parameters, results);
                directCallee = null;
                isMovingPushedFrame = false;

                results.CopyTo(values.AsSpan(parametersIndex));
                stackPointer = parametersIndex + numResults;
                return true;
            }

            if (callee is LoweredFunction { DirectFunction: { } direct } lowered)
            {
                var numResults = lowered.Type.ResultTypes.Length;

                isMovingPushedFrame = true;
                directCallee = callee;
                var task = direct.InvokeDirect(new DirectInvocationContext(lowered, Context), parameters);
                directCallee = null;
                isMovingPushedFrame = false;

                if (!task.IsCompletedSuccessfully)
                {
                    pendingCore = new DirectInvocationStackFrame(task, numResults);
                    return false;
                }

                if (numResults != 0) values[parametersIndex] = task.Result;
                stackPointer = parametersIndex + numResults;
                return true;
            }

            return false;
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
        private static void ThrowStackOverflow()
        {
            throw new TrapException("Stack overflow.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowDivideByZero(int pc)
        {
            programCounter = (uint)pc;
            throw Traps.DivideByZero();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowOutOfBounds(int pc)
        {
            programCounter = (uint)pc;
            throw new TrapException("out of bounds memory access");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfCancellationRequested(ExecutionContext context, int pc)
        {
            if (!context.CancellationToken.IsCancellationRequested) return;
            programCounter = (uint)pc;
            context.CancellationToken.ThrowIfCancellationRequested();
        }

        // moves the values a branch carries to the target height and returns the new stack pointer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MoveBranchValues(ref StackValueItem values, int sp, int height, int arity)
        {
            var source = sp - arity;
            if (source != height)
                for (var i = 0; i < arity; i++)
                    Unsafe.Add(ref values, height + i) = Unsafe.Add(ref values, source + i);

            return height + arity;
        }

        private const int MaxInlineResults = 4;
        private static readonly byte[] EmptyMemory = new byte[1];

        /// <summary>
        ///     The interpreter loop. Runs until the function ends or a frame is pushed.
        ///     <see cref="programCounter" /> is updated before an instruction which may throw so that it points to the
        ///     failed one.
        /// </summary>
        private StackFrameState Run(in Waker waker)
        {
            // the locals are kept few for the register allocation
            var context = Context;
            var frameBase = this.frameBase;
            var sp = stackPointer;
            var pc = (int)programCounter;

            // the bounds are not checked on the hot path. the validation ensures that the indices of the locals and
            // the operand stack are within the buffer, and the program counter stays in the code.
            ref var v0 = ref values[0];
            ref var c0 = ref compiled.BaseCode[0];

            ref var m0 = ref EmptyMemory[0];
            var memLen = 0UL;
            if (memory != null && memory.Buffer.Length != 0)
            {
                m0 = ref memory.Buffer[0];
                memLen = (ulong)memory.Length;
            }

            while (true)
            {
                ref var instr = ref Unsafe.Add(ref c0, pc);

                // the fuel is consumed only at the start of each basic block
                int cost = instr.Cost;
                if (cost != 0 && (context.fuel -= cost) < 0)
                {
                    programCounter = (uint)pc;
                    // the debugger reads the operand stack
                    stackPointer = sp;
                    // a pause of the debugger throws, to keep the loop small
                    context.OnFuelExhausted(this, waker);
                }
                IInvocableFunction callee;
                switch (instr.Op)
                {
                    case Op.Nop:
                        break;
                    case Op.Return:
                        goto Return;

                    #region Control

                    case Op.If:
                        if (Unsafe.Add(ref v0, --sp).valueI32 == 0)
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
                        ThrowIfCancellationRequested(context, pc);
                        sp = MoveBranchValues(ref v0, sp, this.stackBase + instr.B, instr.C);
                        pc = instr.A;
                        continue;
                    case Op.Br:
                        sp = MoveBranchValues(ref v0, sp, this.stackBase + instr.B, instr.C);
                        pc = instr.A;
                        continue;
                    case Op.BrIfLoop:
                        if (Unsafe.Add(ref v0, --sp).valueI32 != 0)
                        {
                            ThrowIfCancellationRequested(context, pc);
                            sp = MoveBranchValues(ref v0, sp, this.stackBase + instr.B, instr.C);
                            pc = instr.A;
                            continue;
                        }

                        break;
                    case Op.BrIf:
                        if (Unsafe.Add(ref v0, --sp).valueI32 != 0)
                        {
                            sp = MoveBranchValues(ref v0, sp, this.stackBase + instr.B, instr.C);
                            pc = instr.A;
                            continue;
                        }

                        break;
                    case Op.ReturnIf:
                        if (Unsafe.Add(ref v0, --sp).valueI32 != 0) goto Return;
                        break;
                    case Op.BrTable:
                    {
                        var i = Unsafe.Add(ref v0, --sp).valueI32;
                        ref var target = ref compiled.BranchTargets[instr.A + (i < (uint)instr.B ? (int)i : instr.B)];
                        if (target.Index < 0) goto Return;

                        if (target.IsLoop) ThrowIfCancellationRequested(context, pc);
                        sp = MoveBranchValues(ref v0, sp, this.stackBase + target.Height, target.Arity);
                        pc = target.Index;
                        continue;
                    }
                    case Op.Call:
                        callee = functions[instr.A];
                        goto Call;
                    case Op.CallIndirect:
                        programCounter = (uint)pc;
                        callee = GetIndirectCallee((uint)instr.A, (uint)instr.C, Unsafe.Add(ref v0, --sp).valueI32);
                        goto Call;

                    #endregion

                    #region Parametric

                    case Op.Drop:
                        sp--;
                        break;
                    case Op.Select:
                        if (Unsafe.Add(ref v0, sp - 1).valueI32 == 0) Unsafe.Add(ref v0, sp - 3) = Unsafe.Add(ref v0, sp - 2);
                        sp -= 2;
                        break;

                    #endregion

                    #region Variable

                    case Op.LocalGet:
                        Unsafe.Add(ref v0, sp) = Unsafe.Add(ref v0, frameBase + instr.A);
                        sp++;
                        break;
                    case Op.LocalSet:
                        Unsafe.Add(ref v0, frameBase + instr.A) = Unsafe.Add(ref v0, --sp);
                        break;
                    case Op.LocalTee:
                        Unsafe.Add(ref v0, frameBase + instr.A) = Unsafe.Add(ref v0, sp - 1);
                        break;
                    case Op.GlobalGet:
                        Unsafe.Add(ref v0, sp) = globals[instr.A].stackValue;
                        sp++;
                        break;
                    case Op.GlobalSet:
                        globals[instr.A].stackValue = Unsafe.Add(ref v0, --sp);
                        break;

                    #endregion

                    #region Numeric

                    case Op.Const:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp++);
                        slot.valueType = (ValueType)instr.A;
                        slot.valueI64 = instr.Value;
                        break;
                    }

                    case Op.I32Eqz:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = slot.valueI32 == 0 ? 1UL : 0UL;
                        break;
                    }
                    case Op.I32Eq:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 == Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32Ne:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 != Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LtS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (int)lhs.valueI32 < (int)Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LtU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 < Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GtS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (int)lhs.valueI32 > (int)Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GtU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 > Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LeS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (int)lhs.valueI32 <= (int)Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32LeU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 <= Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GeS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (int)lhs.valueI32 >= (int)Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }
                    case Op.I32GeU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 >= Unsafe.Add(ref v0, sp - 1).valueI32 ? 1UL : 0UL;
                        sp--;
                        break;
                    }

                    case Op.I64Eqz:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = slot.valueI64 == 0 ? 1UL : 0UL;
                        slot.valueType = ValueType.I32;
                        break;
                    }
                    case Op.I64Eq:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 == Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64Ne:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 != Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LtS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (long)lhs.valueI64 < (long)Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LtU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 < Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GtS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (long)lhs.valueI64 > (long)Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GtU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 > Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LeS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (long)lhs.valueI64 <= (long)Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64LeU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 <= Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GeS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (long)lhs.valueI64 >= (long)Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.I64GeU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI64 >= Unsafe.Add(ref v0, sp - 1).valueI64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }

                    case Op.I32Add:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 + Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Sub:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 - Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Mul:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 * Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32And:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 & Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Or:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 | Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Xor:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 ^ Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Shl:
                    {
                        // the shift count is masked to 5 bits as specified
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 << (int)Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32ShrS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (uint)((int)lhs.valueI32 >> (int)Unsafe.Add(ref v0, sp - 1).valueI32);
                        sp--;
                        break;
                    }
                    case Op.I32ShrU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueI32 >> (int)Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp--;
                        break;
                    }
                    case Op.I32Rotl:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var value = lhs.valueI32;
                        var k = (int)Unsafe.Add(ref v0, sp - 1).valueI32;
                        lhs.valueI64 = (value << k) | (value >> (32 - k));
                        sp--;
                        break;
                    }
                    case Op.I32Rotr:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var value = lhs.valueI32;
                        var k = (int)Unsafe.Add(ref v0, sp - 1).valueI32;
                        lhs.valueI64 = (value >> k) | (value << (32 - k));
                        sp--;
                        break;
                    }

                    case Op.I64Add:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 += Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Sub:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 -= Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Mul:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 *= Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64And:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 &= Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Or:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 |= Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Xor:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 ^= Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Shl:
                    {
                        // the shift count is masked to 6 bits as specified
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 <<= (int)Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64ShrS:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = (ulong)((long)lhs.valueI64 >> (int)Unsafe.Add(ref v0, sp - 1).valueI64);
                        sp--;
                        break;
                    }
                    case Op.I64ShrU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 >>= (int)Unsafe.Add(ref v0, sp - 1).valueI64;
                        sp--;
                        break;
                    }
                    case Op.I64Rotl:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var value = lhs.valueI64;
                        var k = (int)Unsafe.Add(ref v0, sp - 1).valueI64;
                        lhs.valueI64 = (value << k) | (value >> (64 - k));
                        sp--;
                        break;
                    }
                    case Op.I64Rotr:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var value = lhs.valueI64;
                        var k = (int)Unsafe.Add(ref v0, sp - 1).valueI64;
                        lhs.valueI64 = (value >> k) | (value << (64 - k));
                        sp--;
                        break;
                    }

                    case Op.I32WrapI64:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = slot.valueI32;
                        slot.valueType = ValueType.I32;
                        break;
                    }
                    case Op.I64ExtendI32S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = (ulong)(int)slot.valueI32;
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64ExtendI32U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = slot.valueI32;
                        slot.valueType = ValueType.I64;
                        break;
                    }


                    case Op.F32Eq:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 == Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Ne:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 != Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Lt:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 < Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Gt:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 > Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Le:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 <= Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Ge:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF32 >= Unsafe.Add(ref v0, sp - 1).valueF32 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Eq:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 == Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Ne:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 != Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Lt:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 < Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Gt:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 > Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Le:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 <= Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F64Ge:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueI64 = lhs.valueF64 >= Unsafe.Add(ref v0, sp - 1).valueF64 ? 1UL : 0UL;
                        lhs.valueType = ValueType.I32;
                        sp--;
                        break;
                    }
                    case Op.F32Add:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF32 = lhs.valueF32 + Unsafe.Add(ref v0, sp - 1).valueF32;
                        sp--;
                        break;
                    }
                    case Op.F32Sub:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF32 = lhs.valueF32 - Unsafe.Add(ref v0, sp - 1).valueF32;
                        sp--;
                        break;
                    }
                    case Op.F32Mul:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF32 = lhs.valueF32 * Unsafe.Add(ref v0, sp - 1).valueF32;
                        sp--;
                        break;
                    }
                    case Op.F32Div:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF32 = lhs.valueF32 / Unsafe.Add(ref v0, sp - 1).valueF32;
                        sp--;
                        break;
                    }
                    case Op.F64Add:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF64 = lhs.valueF64 + Unsafe.Add(ref v0, sp - 1).valueF64;
                        sp--;
                        break;
                    }
                    case Op.F64Sub:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF64 = lhs.valueF64 - Unsafe.Add(ref v0, sp - 1).valueF64;
                        sp--;
                        break;
                    }
                    case Op.F64Mul:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF64 = lhs.valueF64 * Unsafe.Add(ref v0, sp - 1).valueF64;
                        sp--;
                        break;
                    }
                    case Op.F64Div:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        lhs.valueF64 = lhs.valueF64 / Unsafe.Add(ref v0, sp - 1).valueF64;
                        sp--;
                        break;
                    }
                    case Op.F32ConvertI32S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF32 = (int)slot.valueI32;
                        slot.valueType = ValueType.F32;
                        break;
                    }
                    case Op.F32ConvertI32U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF32 = slot.valueI32;
                        slot.valueType = ValueType.F32;
                        break;
                    }
                    case Op.F32ConvertI64S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF32 = (long)slot.valueI64;
                        slot.valueType = ValueType.F32;
                        break;
                    }
                    case Op.F32ConvertI64U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF32 = slot.valueI64;
                        slot.valueType = ValueType.F32;
                        break;
                    }
                    case Op.F64ConvertI32S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF64 = (int)slot.valueI32;
                        slot.valueType = ValueType.F64;
                        break;
                    }
                    case Op.F64ConvertI32U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF64 = slot.valueI32;
                        slot.valueType = ValueType.F64;
                        break;
                    }
                    case Op.F64ConvertI64S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF64 = (long)slot.valueI64;
                        slot.valueType = ValueType.F64;
                        break;
                    }
                    case Op.F64ConvertI64U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF64 = slot.valueI64;
                        slot.valueType = ValueType.F64;
                        break;
                    }
                    case Op.F32DemoteF64:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF32 = (float)slot.valueF64;
                        slot.valueType = ValueType.F32;
                        break;
                    }
                    case Op.F64PromoteF32:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueF64 = (double)slot.valueF32;
                        slot.valueType = ValueType.F64;
                        break;
                    }
                    case Op.Reinterpret:
                        // the bits are kept as they are
                        Unsafe.Add(ref v0, sp - 1).valueType = (ValueType)instr.A;
                        break;

                    #endregion

                    #region Numeric operations which trap

                    case Op.I32DivU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var rhs = Unsafe.Add(ref v0, sp - 1).valueI32;
                        if (rhs == 0) ThrowDivideByZero(pc);
                        lhs.valueI64 = lhs.valueI32 / rhs;
                        sp--;
                        break;
                    }
                    case Op.I32RemU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var rhs = Unsafe.Add(ref v0, sp - 1).valueI32;
                        if (rhs == 0) ThrowDivideByZero(pc);
                        lhs.valueI64 = lhs.valueI32 % rhs;
                        sp--;
                        break;
                    }
                    case Op.I64DivU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var rhs = Unsafe.Add(ref v0, sp - 1).valueI64;
                        if (rhs == 0) ThrowDivideByZero(pc);
                        lhs.valueI64 /= rhs;
                        sp--;
                        break;
                    }
                    case Op.I64RemU:
                    {
                        ref var lhs = ref Unsafe.Add(ref v0, sp - 2);
                        var rhs = Unsafe.Add(ref v0, sp - 1).valueI64;
                        if (rhs == 0) ThrowDivideByZero(pc);
                        lhs.valueI64 %= rhs;
                        sp--;
                        break;
                    }
                    case Op.I32ExtendS8:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = unchecked((uint)(sbyte)(byte)slot.valueI32);
                        break;
                    }
                    case Op.I64Ctz:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        slot.valueI64 = BitUtils.TrailingZeroCount(slot.valueI64);
                        break;
                    }

                    #endregion

                    #region Memory

                    case Op.Load32:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 4 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = (ValueType)instr.B;
                        break;
                    }
                    case Op.Load64:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 8 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = (ValueType)instr.B;
                        break;
                    }
                    case Op.I32Load8S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 1 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = (uint)(sbyte)Unsafe.Add(ref m0, (int)address);
                        break;
                    }
                    case Op.I32Load8U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 1 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.Add(ref m0, (int)address);
                        break;
                    }
                    case Op.I32Load16S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 2 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = (uint)Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref m0, (int)address));
                        break;
                    }
                    case Op.I32Load16U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 2 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref m0, (int)address));
                        break;
                    }
                    case Op.I64Load8S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 1 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = (ulong)(sbyte)Unsafe.Add(ref m0, (int)address);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load8U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 1 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.Add(ref m0, (int)address);
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load16S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 2 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = (ulong)Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load16U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 2 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load32S:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 4 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = (ulong)Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.I64Load32U:
                    {
                        ref var slot = ref Unsafe.Add(ref v0, sp - 1);
                        var address = (ulong)slot.valueI32 + (uint)instr.A;
                        if (address + 4 > memLen) ThrowOutOfBounds(pc);
                        slot.valueI64 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref m0, (int)address));
                        slot.valueType = ValueType.I64;
                        break;
                    }
                    case Op.Store8:
                    {
                        var address = (ulong)Unsafe.Add(ref v0, sp - 2).valueI32 + (uint)instr.A;
                        if (address + 1 > memLen) ThrowOutOfBounds(pc);
                        Unsafe.Add(ref m0, (int)address) = (byte)Unsafe.Add(ref v0, sp - 1).valueI32;
                        sp -= 2;
                        break;
                    }
                    case Op.Store16:
                    {
                        var address = (ulong)Unsafe.Add(ref v0, sp - 2).valueI32 + (uint)instr.A;
                        if (address + 2 > memLen) ThrowOutOfBounds(pc);
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref m0, (int)address), (ushort)Unsafe.Add(ref v0, sp - 1).valueI32);
                        sp -= 2;
                        break;
                    }
                    case Op.Store32:
                    {
                        var address = (ulong)Unsafe.Add(ref v0, sp - 2).valueI32 + (uint)instr.A;
                        if (address + 4 > memLen) ThrowOutOfBounds(pc);
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref m0, (int)address), Unsafe.Add(ref v0, sp - 1).valueI32);
                        sp -= 2;
                        break;
                    }
                    case Op.Store64:
                    {
                        var address = (ulong)Unsafe.Add(ref v0, sp - 2).valueI32 + (uint)instr.A;
                        if (address + 8 > memLen) ThrowOutOfBounds(pc);
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref m0, (int)address), Unsafe.Add(ref v0, sp - 1).valueI64);
                        sp -= 2;
                        break;
                    }

                    #endregion

                    default:
                    {
                        // instructions without a dedicated operation
                        programCounter = (uint)pc;
                        stackPointer = sp;
                        Function.function.Body.Instructions.Span[pc].Execute(this);
                        sp = stackPointer;

                        if (isFramePushed)
                        {
                            programCounter = (uint)(pc + 1);
                            return StackFrameState.Ready;
                        }

                        if (isEnd) return StackFrameState.Completed;
                        if (memory != null && memory.Buffer.Length != 0)
                        {
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }
                        break;
                    }
                }

                pc += instr.Length;
                continue;

                Call:
                {
                    programCounter = (uint)pc;
                    // the calls in this frame count as frames of the context
                    if (context.FrameCount + activationCount >= context.MaxStackFrameCount) ThrowStackOverflow();

                    var numParameters = instr.B;
                    if (callee is InstanceFunction wasmCallee)
                    {
                        // the cancellation is observed on calls as well as loops, to abort a deep recursion
                        ThrowIfCancellationRequested(context, pc);
                        EnterFunction(wasmCallee, pc + 1, sp - numParameters);
                        c0 = ref compiled.BaseCode[0];
                        v0 = ref values[0];
                        frameBase = this.frameBase;
                        sp = stackBase;
                        pc = 0;
                        if (memory != null && memory.Buffer.Length != 0)
                        {
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }
                        continue;
                    }

                    // functions of the host run in their own frames, which are moved here without going back to the
                    // execution context. this frame continues if the callee completes synchronously.
                    sp -= numParameters;
                    stackPointer = sp;
                    if (TryInvokeDirectly(callee, sp, numParameters, out var pendingCore))
                    {
                        sp = stackPointer;
                        pc++;
                        if (memory != null && memory.Buffer.Length != 0)
                        {
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }

                        continue;
                    }

                    isFramePushed = true;
                    var frame = pendingCore != null
                        ? context.PushFrame(new StackFrame(pendingCore))
                        : context.PushFrameFromFrame(callee,
                            new ReadOnlySpan<StackValueItem>(values, sp, numParameters));
                    programCounter = (uint)(pc + 1);

                    var state = MovePushedFrame(frame, waker);
                    if (state != StackFrameState.Completed) return state;

                    // the results are pushed by the completed frame
                    isFramePushed = false;
                    sp = stackPointer;
                    pc++;
                    if (memory != null && memory.Buffer.Length != 0)
                    {
                        m0 = ref memory.Buffer[0];
                        memLen = (ulong)memory.Length;
                    }
                    continue;
                }

                Return:
                if (activationCount == 0)
                {
                    isEnd = true;
                    stackPointer = sp;
                    return StackFrameState.Completed;
                }

                // the locals are not passed by reference to keep them in registers
                pc = ReturnToCaller(sp);
                sp = stackPointer;
                c0 = ref compiled.BaseCode[0];
                frameBase = this.frameBase;
                if (memory != null && memory.Buffer.Length != 0)
                {
                    m0 = ref memory.Buffer[0];
                    memLen = (ulong)memory.Length;
                }
            }
        }

        private struct Activation
        {
            public InstanceFunction Function;
            public int ReturnIndex;
            public int FrameBase;
            public int StackBase;
        }
    }
}
