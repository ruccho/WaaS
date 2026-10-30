using System;
using System.Collections.Generic;
using System.Threading;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Observes and pauses the execution of WebAssembly in the contexts it is attached to with
    ///     <see cref="ExecutionContext.Debugger" />. The methods are called on the thread executing WebAssembly.
    /// </summary>
    /// <remarks>
    ///     The execution can pause only while it runs with <see cref="ExecutionContext.InvokeAsync" />. With
    ///     <see cref="ExecutionContext.Invoke" />, <see cref="ShouldPause" /> is called but the execution does not pause.
    /// </remarks>
    public interface IWasmDebugger
    {
        /// <summary>
        ///     Called before each instruction is executed. Returns true to pause before the instruction.
        /// </summary>
        bool ShouldPause(in WasmDebugLocation location);

        /// <summary>
        ///     Called when the execution pauses. The execution stays paused until <see cref="WasmDebugPause.Resume" /> is
        ///     called.
        /// </summary>
        void OnPaused(WasmDebugPause pause);
    }

    /// <summary>
    ///     The instruction about to be executed.
    /// </summary>
    public readonly struct WasmDebugLocation
    {
        internal WasmDebugLocation(ExecutionContext context, InstanceFunction function, Instruction instruction,
            int depth)
        {
            Context = context;
            Function = function;
            Instruction = instruction;
            Depth = depth;
        }

        public ExecutionContext Context { get; }
        public InstanceFunction Function { get; }
        public Instruction Instruction { get; }

        /// <summary>
        ///     The number of the frames being executed in the context, including the functions of WebAssembly called in
        ///     the frames of WebAssembly. A step over continues until the depth is the same or smaller.
        /// </summary>
        public int Depth { get; }
    }

    /// <summary>
    ///     A paused execution.
    /// </summary>
    public sealed class WasmDebugPause
    {
        private readonly SynchronizationContext synchronizationContext;
        private Waker waker;
        private int resumed;

        internal WasmDebugPause(ExecutionContext context, IReadOnlyList<WasmDebugFrame> frames, Waker waker, int depth)
        {
            Context = context;
            Frames = frames;
            Depth = depth;
            this.waker = waker;
            synchronizationContext = SynchronizationContext.Current;
        }

        public ExecutionContext Context { get; }

        /// <summary>
        ///     The frames of WebAssembly at the pause, from the innermost one. The values are copied at the pause.
        /// </summary>
        public IReadOnlyList<WasmDebugFrame> Frames { get; }

        /// <summary>
        ///     <see cref="WasmDebugLocation.Depth" /> of the instruction at the pause.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        ///     Resumes the execution. It can be called from any thread, and the execution resumes on the
        ///     synchronization context of the thread that paused, if any. Only the first call resumes.
        /// </summary>
        public void Resume()
        {
            if (Interlocked.Exchange(ref resumed, 1) != 0) return;
            if (synchronizationContext != null && synchronizationContext != SynchronizationContext.Current)
                synchronizationContext.Post(static state => ((WasmDebugPause)state).waker.Wake(), this);
            else
                waker.Wake();
        }
    }

    /// <summary>
    ///     A function of WebAssembly being executed.
    /// </summary>
    public sealed class WasmDebugFrame
    {
        internal WasmDebugFrame(InstanceFunction function, Instruction instruction, StackValueItem[] locals,
            StackValueItem[] operandStack)
        {
            Function = function;
            Instruction = instruction;
            Locals = locals;
            OperandStack = operandStack;
        }

        public InstanceFunction Function { get; }
        public Instance Instance => Function.instance;

        /// <summary>
        ///     The instruction being executed, or the call instruction waiting for the callee in the outer frames.
        /// </summary>
        public Instruction Instruction { get; }

        /// <summary>
        ///     The parameters followed by the other locals.
        /// </summary>
        public ReadOnlyMemory<StackValueItem> Locals { get; }

        /// <summary>
        ///     The values on the operand stack, from the bottom.
        /// </summary>
        public ReadOnlyMemory<StackValueItem> OperandStack { get; }
    }
}
