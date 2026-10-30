using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents an execution context, which is used to manage the execution of WebAssembly functions.
    ///     It contains a stack of frames and behaves like a thread of execution.
    /// </summary>
    public class ExecutionContext : IDisposable
    {
        private readonly Stack<StackFrame> frames = new();
        private readonly object locker = new();
        private readonly uint? maxStackFrames;
        private ValueTaskSource currentPendingTaskSource;

        // remaining fuel. it is long.MaxValue when the fuel is unlimited, and never runs out in practice.
        internal long fuel = long.MaxValue;
        // frames of WebAssembly moved directly by their caller frame, instead of the loop in MoveToEndOrPending()
        private const int MaxInlineFrameDepth = 8;
        private int inlineFrameDepth;
        private bool isFuelLimited;
        private bool moving;

        public ExecutionContext(uint? maxStackFrames = null)
        {
            this.maxStackFrames = maxStackFrames;
        }

        /// <summary>
        ///     The remaining fuel, or null if the fuel is unlimited (default).
        ///     Each executed WebAssembly instruction consumes one unit of fuel, and <see cref="FuelExhaustedException" /> is
        ///     thrown when an instruction is about to be executed with no fuel left.
        ///     The host can refill the fuel at any time, e.g. between invocations or while the execution is suspended.
        /// </summary>
        public ulong? Fuel
        {
            get => isFuelLimited ? (ulong)Math.Max(fuel, 0) : null;
            set
            {
                isFuelLimited = value.HasValue;
                fuel = value.HasValue ? (long)Math.Min(value.Value, long.MaxValue) : long.MaxValue;
            }
        }

        /// <summary>
        ///     If set, <see cref="Fuel" /> is reset to this value each time the execution starts or resumes, that is, at the
        ///     start of an invocation and after each suspension on an asynchronous host function.
        ///     This limits the number of instructions executed between two suspensions.
        /// </summary>
        public ulong? FuelPerResume { get; set; }

        /// <summary>
        ///     If set, "memory.grow" executed on this context fails once the memory would exceed this number of pages,
        ///     in addition to the maximum declared by the module.
        /// </summary>
        public uint? MaxMemoryPages { get; set; }

        /// <summary>
        ///     The token to abort the execution on this context. It is observed while WebAssembly code is running (at
        ///     branches and calls) and while the execution is suspended on an asynchronous host function.
        ///     When cancellation is requested, <see cref="OperationCanceledException" /> is thrown from the invocation and the
        ///     frames of the invocation are discarded.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public StackFrame LastFrame { get; private set; }

        internal int FrameCount => frames.Count;

        internal uint? MaxStackFrames => maxStackFrames;

        private StackFrame? Current
        {
            get
            {
                if (!frames.TryPeek(out var top)) return null;
                return top;
            }
        }

        public int ResultLength => LastFrame.ResultLength;

        public void Dispose()
        {
            lock (locker)
            {
                foreach (var f in frames) f.Dispose();
                frames.Clear();

                LastFrame.Dispose();
                LastFrame = default;
            }
        }

        private void MoveToEndOrPending(StackFrame initial, out bool pending)
        {
            Span<StackValueItem> resultsBuffer = stackalloc StackValueItem[16];

            lock (locker)
            {
                try
                {
                    var isFirst = true;
                    Loop:
                    var current = Current ?? throw new InvalidOperationException();
                    var depth = frames.Count - 1;

                    CancellationToken.ThrowIfCancellationRequested();

                    if (isFirst)
                        // Console.WriteLine($"{new string(' ', depth * 4)}- {current}: resume");
                        isFirst = false;

                    LoopCurrent:

                    if (moving) throw new InvalidOperationException("ExecutionContext detected reentrant evaluation!");

                    moving = true;
                    StackFrameState state;
                    try
                    {
                        state = current.MoveNext(new Waker(currentPendingTaskSource));
                    }
                    finally
                    {
                        moving = false;
                    }

                    switch (state)
                    {
                        case StackFrameState.Ready:
                        {
                            goto Loop;
                        }
                        case StackFrameState.Pending:
                        {
                            // Logger.Log($"{new string(' ', depth)}- {current}: pending");
                            pending = true;
                            return;
                        }
                        case StackFrameState.Completed:
                        {
                            // Logger.Log($"{new string(' ', depth)}- {current}: completed");
                            if (frames.Pop() == initial)
                            {
                                if (frames.Count == 0)
                                    // end
                                    LastFrame = current;

                                pending = false;
                                return;
                            }

                            if (frames.TryPeek(out var next) && next.DoesTakeResults())
                            {
                                // stackalloc inside this loop would never be released until the method returns,
                                // so the buffer is allocated once outside the loop.
                                var resultLength = current.ResultLength;
                                if (resultLength <= resultsBuffer.Length)
                                {
                                    var results = resultsBuffer.Slice(0, resultLength);
                                    current.TakeResults(results);
                                    next.PushResults(results);
                                }
                                else
                                {
                                    var rented = ArrayPool<StackValueItem>.Shared.Rent(resultLength);
                                    try
                                    {
                                        var results = rented.AsSpan(0, resultLength);
                                        current.TakeResults(results);
                                        next.PushResults(results);
                                    }
                                    finally
                                    {
                                        ArrayPool<StackValueItem>.Shared.Return(rented);
                                    }
                                }
                            }

                            current.Dispose();
                            current = next;

                            goto LoopCurrent;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                    // capture the frames before they are disposed
                    WasmStackTrace.Attach(ex, frames);

                    DiscardFrames(initial);
                    throw;
                }
            }
        }

        internal bool TryEnterInlineFrame()
        {
            if (inlineFrameDepth >= MaxInlineFrameDepth) return false;
            inlineFrameDepth++;
            return true;
        }

        internal void ExitInlineFrame()
        {
            inlineFrameDepth--;
        }

        /// <summary>
        ///     Pops the completed frame at the top, and passes its results to the next frame.
        /// </summary>
        internal void CompleteFrame(StackFrame frame, WasmStackFrame next)
        {
            if (frames.Pop() != frame) throw new InvalidOperationException();

            var resultLength = frame.ResultLength;
            if (resultLength <= 16)
            {
                Span<StackValueItem> results = stackalloc StackValueItem[resultLength];
                frame.TakeResults(results);
                next.PushResults(next.Version, results);
            }
            else
            {
                var rented = ArrayPool<StackValueItem>.Shared.Rent(resultLength);
                try
                {
                    var results = rented.AsSpan(0, resultLength);
                    frame.TakeResults(results);
                    next.PushResults(next.Version, results);
                }
                finally
                {
                    ArrayPool<StackValueItem>.Shared.Return(rented);
                }
            }

            frame.Dispose();
        }

        private void DiscardFrames(StackFrame initial)
        {
            lock (locker)
            {
                while (frames.TryPeek(out var f))
                {
                    frames.Pop();
                    f.Dispose();
                    if (f == initial) break;
                }
            }
        }

        // called when the fuel counter goes below zero
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void OnFuelExhausted()
        {
            if (!isFuelLimited)
            {
                fuel = long.MaxValue;
                return;
            }

            fuel = 0;
            throw new FuelExhaustedException();
        }

        private void RefillFuel()
        {
            if (FuelPerResume.HasValue) Fuel = FuelPerResume;
        }

        public void Invoke(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(function, inputValues);
            Run();
        }

        internal void Invoke(StackFrame frame)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(frame);
            Run();
        }

        public ValueTask InvokeAsync(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            lock (locker)
            {
                if (frames.Count > 0) throw new InvalidOperationException();
                PushFrame(function, inputValues);
                return RunAsync();
            }
        }

        internal ValueTask InvokeAsync(StackFrame frame)
        {
            lock (locker)
            {
                if (frames.Count > 0) throw new InvalidOperationException();
                PushFrame(frame);
                return RunAsync();
            }
        }

        internal StackFrame PushFrame(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            lock (locker)
            {
                return PushFrameWhileMoving(function, inputValues);
            }
        }

        /// <summary>
        ///     Pushes a frame from a frame being moved.
        /// </summary>
        internal StackFrame PushFrameFromFrame(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            // the lock is already held while a frame is moved
            return moving ? PushFrameWhileMoving(function, inputValues) : PushFrame(function, inputValues);
        }

        private StackFrame PushFrameWhileMoving(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            // the exhaustion of the call stack is a trap, as the exhaustion of the stack of the host would be
            if (frames.Count >= maxStackFrames) throw new TrapException("Stack overflow.");

            var frame = function.CreateFrame(this, inputValues);
            frames.Push(frame);
            return frame;
        }

        internal StackFrame PushFrame(StackFrame frame)
        {
            lock (locker)
            {
                if (frames.Count >= maxStackFrames) throw new InvalidOperationException();
                frames.Push(frame);
                return frame;
            }
        }

        internal void InterruptFrame(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues,
            Span<StackValueItem> results)
        {
            lock (locker)
            {
                var frame = PushFrame(function, inputValues);
                var movingNow = moving;
                moving = false;
                MoveToEndOrPending(frame, out var pending);
                moving = movingNow;
                if (pending)
                {
                    Dispose();
                    throw new InvalidOperationException("InterruptFrame() cannot handle async frames.");
                }

                frame.TakeResults(results);
                frame.Dispose();
            }
        }

        private void Run()
        {
            RefillFuel();
            MoveToEndOrPending(Current ?? throw new InvalidOperationException(), out var pending);

            if (pending)
            {
                Dispose();
                throw new InvalidOperationException("Use RunAsync() instead of Run() to use coroutines.");
            }
        }

        private async ValueTask RunAsync()
        {
            var initial = Current ?? throw new InvalidOperationException();
            while (true)
            {
                RefillFuel();
                var source = currentPendingTaskSource ??= ValueTaskSource.Create();
                MoveToEndOrPending(initial, out var pending);

                if (!pending) return;

                currentPendingTaskSource = null;

                var registration = RegisterCancellation(source, CancellationToken);
                try
                {
                    await source.AsValueTask().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // the suspended frames are never resumed
                    WasmStackTrace.Attach(ex, frames);
                    DiscardFrames(initial);
                    throw;
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        private static CancellationTokenRegistration RegisterCancellation(ValueTaskSource source,
            CancellationToken cancellationToken)
        {
            // kept out of RunAsync() so that the closure is allocated only for a cancelable token
            if (!cancellationToken.CanBeCanceled) return default;
            return cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));
        }

        public void TakeResults(Span<StackValueItem> results)
        {
            LastFrame.TakeResults(results);
        }
    }


    public struct Waker
    {
        private readonly ValueTaskSource source;

        internal Waker(ValueTaskSource source)
        {
            this.source = source;
        }

        public void Wake()
        {
            source.TrySetResult();
        }

        public void Fail(Exception ex)
        {
            source.TrySetException(ex);
        }
    }
}