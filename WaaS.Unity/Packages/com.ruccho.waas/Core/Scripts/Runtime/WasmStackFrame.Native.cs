#if WAAS_NATIVE_INTERPRETER
using System;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    public unsafe partial class WasmStackFrame
    {
        // how many loop iterations and calls run between the checks of the cancellation
        private const int PollInterval = 4096;

        // the callers while the interpreter on pointers is running. copied from and to the activations
        private NativeActivation[] nativeActivations;

        private static NativeFunction* GetNative(InstanceFunction function)
        {
            if (function.Native == null) function.instance.GetNative();
            return function.Native;
        }

        /// <summary>
        ///     Runs the register code on pointers. What it cannot do by itself is handled here, keeping the managed state
        ///     of the frame up to date.
        /// </summary>
        private StackFrameState RunNative(in Waker waker)
        {
            var context = Context;
            var function = GetNative(Function);

            if (nativeActivations == null || nativeActivations.Length < activationCount + 16)
                nativeActivations = new NativeActivation[Math.Max(activationCount * 2, 16)];

            for (var i = 0; i < activationCount; i++)
            {
                ref var activation = ref activations[i];
                ref var native = ref nativeActivations[i];
                native.Function = GetNative(activation.Function);
                native.ReturnPc = activation.Function.function.Compiled.EntryIndices[activation.ReturnIndex] - 1;
                native.FrameBase = activation.FrameBase;
            }

            var poll = context.CancellationToken.CanBeCanceled ? PollInterval : int.MaxValue;
            NativeFrameState state = default;
            state.Function = function;
            state.Ip = function->Code + compiled.EntryIndices[programCounter];
            state.FrameBase = frameBase;
            state.ActivationCount = activationCount;
            state.Poll = poll;

            var isSynchronized = false;
            try
            {
                while (true)
                {
                    int reason;
                    state.Fuel = context.fuel;
                    state.ActivationLimit = (int)Math.Min(nativeActivations.Length,
                        Math.Max(context.MaxStackFrameCount - context.FrameCount, 0));

                    fixed (StackValueItem* valuesPointer = values)
                    fixed (byte* memoryPointer = memory?.Buffer)
                    fixed (NativeActivation* activationsPointer = nativeActivations)
                    {
                        state.Values = valuesPointer;
                        state.Capacity = values.Length;
                        state.Memory = memoryPointer;
                        state.MemoryLength = memory != null ? (ulong)memory.Length : 0;
                        state.Activations = activationsPointer;
                        reason = NativeInterpreter.Run(&state);
                    }

                    context.fuel = state.Fuel;
                    activationCount = state.ActivationCount;
                    frameBase = state.FrameBase;
                    if (state.Function != function)
                    {
                        function = state.Function;
                        SetFunction(function);
                    }

                    stackBase = frameBase + function->NumLocals;

                    var pc = (int)(state.Ip - function->Code);
                    ref var instr = ref *state.Ip;
                    switch (reason)
                    {
                        case NativeExit.Completed:
                            isEnd = true;
                            stackPointer = stackBase + instr.A;
                            return StackFrameState.Completed;
                        case NativeExit.Call:
                        {
                            var callee = functions[state.ExitArgument];
                            var result = CallFromNative(callee, instr.C, ref instr, pc, ref state, ref function, waker);
                            if (result.HasValue) return result.Value;
                            break;
                        }
                        case NativeExit.CallIndirect:
                        {
                            programCounter = (uint)compiled.Origins[pc];
                            var callee = GetIndirectCallee((uint)instr.B, instr.Tag,
                                values[frameBase + instr.C].valueI32);
                            var result = CallFromNative(callee, callee.Type.ParameterTypes.Length, ref instr, pc,
                                ref state, ref function, waker);
                            if (result.HasValue) return result.Value;
                            break;
                        }
                        case NativeExit.Fallback:
                        {
                            var index = compiled.Origins[pc];
                            programCounter = (uint)index;
                            stackPointer = stackBase + instr.A;
                            Function.function.Body.Instructions.Span[index].Execute(this);

                            if (isFramePushed)
                            {
                                programCounter = (uint)(index + 1);
                                return StackFrameState.Ready;
                            }

                            if (isEnd) return StackFrameState.Completed;
                            state.Ip++;
                            break;
                        }
                        case NativeExit.OutOfFuel:
                        {
                            ref var block = ref compiled.GetBlock(pc);
                            programCounter = (uint)block.Index;
                            stackPointer = stackBase + block.Height;
                            SynchronizeActivations();
                            isSynchronized = true;
                            return Run(waker);
                        }
                        case NativeExit.OutOfBounds:
                            ThrowOutOfBounds(compiled.Origins[pc]);
                            break;
                        case NativeExit.DivideByZero:
                            ThrowDivideByZero(compiled.Origins[pc]);
                            break;
                        case NativeExit.Grow:
                            programCounter = (uint)compiled.Origins[pc];
                            if (context.FrameCount + activationCount >= context.MaxStackFrameCount) ThrowStackOverflow();
                            if (activationCount >= nativeActivations.Length)
                                Array.Resize(ref nativeActivations, nativeActivations.Length * 2);
                            else
                                GrowValues(values.Length * 2, values.Length);
                            break;
                        case NativeExit.Poll:
                            ThrowIfCancellationRequested(context, compiled.Origins[pc]);
                            state.Poll = poll;
                            break;
                        case NativeExit.ReturnToAnotherInstance:
                        {
                            MoveBranchValues(ref values[0], stackBase + instr.A, frameBase, function->NumResults);
                            ref var caller = ref nativeActivations[--state.ActivationCount];
                            function = caller.Function;
                            state.Function = function;
                            state.FrameBase = caller.FrameBase;
                            state.Ip = function->Code + caller.ReturnPc + 1;
                            activationCount = state.ActivationCount;
                            SetFunction(function);
                            SetInstance(Function.instance);
                            break;
                        }
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
            finally
            {
                if (!isSynchronized) SynchronizeActivations();
            }
        }

        // returns the state to leave the loop with
        private StackFrameState? CallFromNative(IInvocableFunction callee, int numParameters,
            ref RegisterInstruction instr, int pc, ref NativeFrameState state, ref NativeFunction* function,
            in Waker waker)
        {
            var context = Context;
            var origin = compiled.Origins[pc];
            programCounter = (uint)origin;
            if (context.FrameCount + activationCount >= context.MaxStackFrameCount) ThrowStackOverflow();

            var sp = stackBase + instr.A;
            if (callee is InstanceFunction wasmCallee)
            {
                // a function of another instance has its own memory, globals and functions
                ThrowIfCancellationRequested(context, origin);

                var native = GetNative(wasmCallee);
                var calleeFrame = sp - numParameters;
                var required = calleeFrame + native->NumLocals + native->MaxStackDepth;
                if (values.Length < required) GrowValues(required, values.Length);
                if (activationCount >= nativeActivations.Length)
                    Array.Resize(ref nativeActivations, nativeActivations.Length * 2);

                for (var i = native->NumParameters; i < native->NumLocals; i++)
                    values[calleeFrame + i] = native->InitialLocals[i];

                ref var activation = ref nativeActivations[activationCount];
                activation.Function = function;
                activation.ReturnPc = pc;
                activation.FrameBase = frameBase;
                state.ActivationCount = ++activationCount;

                function = native;
                state.Function = native;
                state.FrameBase = calleeFrame;
                state.Ip = native->Code;
                SetFunction(native);
                SetInstance(wasmCallee.instance);
                return null;
            }

            sp -= numParameters;
            stackPointer = sp;
            if (TryInvokeDirectly(callee, sp, numParameters, out var pendingCore))
            {
                state.Ip++;
                return null;
            }

            isFramePushed = true;
            var frame = pendingCore != null
                ? context.PushFrame(new StackFrame(pendingCore))
                : context.PushFrameFromFrame(callee, new ReadOnlySpan<StackValueItem>(values, sp, numParameters));
            programCounter = (uint)(origin + 1);

            var frameState = MovePushedFrame(frame, waker);
            if (frameState != StackFrameState.Completed) return frameState;

            isFramePushed = false;
            state.Ip++;
            return null;
        }

        private void SetFunction(NativeFunction* function)
        {
            var target = (InstanceFunction)GCHandle.FromIntPtr(function->Handle).Target;
            Function = target;
            compiled = target.function.Compiled;
        }

        private void SynchronizeActivations()
        {
            if (activationCount == 0) return;

            if (activations == null || activations.Length < activationCount)
                Array.Resize(ref activations, Math.Max(activationCount, 16));
            if (activationCount > usedActivationCount) usedActivationCount = activationCount;

            for (var i = 0; i < activationCount; i++)
            {
                ref var native = ref nativeActivations[i];
                var function = (InstanceFunction)GCHandle.FromIntPtr(native.Function->Handle).Target;
                ref var activation = ref activations[i];
                activation.Function = function;
                activation.ReturnIndex = function.function.Compiled.Origins[native.ReturnPc] + 1;
                activation.FrameBase = native.FrameBase;
                activation.StackBase = native.FrameBase + native.Function->NumLocals;
            }
        }
    }
}
#endif
