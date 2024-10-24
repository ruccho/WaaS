using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaaS.Runtime
{
    public class ExecutionContext : IDisposable
    {
        private readonly Stack<IStackFrame> frames = new();
        private readonly object locker = new();
        private readonly uint? maxStackFrames;
        private ValueTaskSource currentPendingTaskSource;
        private bool moving;

        public ExecutionContext(uint? maxStackFrames = null)
        {
            this.maxStackFrames = maxStackFrames;
        }

        public IStackFrame LastFrame { get; private set; }

        private IStackFrame Current => frames.Peek();

        public int ResultLength => LastFrame.ResultLength;

        public void Dispose()
        {
            lock (locker)
            {
                foreach (var f in frames) f.Dispose();
                frames.Clear();

                LastFrame?.Dispose();
                LastFrame = null;
            }
        }

        private void MoveToEndOrPending(IStackFrame initial, out bool pending)
        {
            lock (locker)
            {
                try
                {
                    var isFirst = true;
                    Loop:
                    var current = Current;
                    var depth = frames.Count - 1;
                    if (current is null) throw new InvalidOperationException();

                    if (isFirst)
                        // Logger.Log($"{new string(' ', depth * 4)}- {current}: resume");
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

                            if (frames.TryPeek(out var next) && next is WasmStackFrame nextWasm)
                            {
                                Span<StackValueItem> results = stackalloc StackValueItem[current.ResultLength];
                                current.TakeResults(results);

                                foreach (var value in results) nextWasm.Push(value.ExpectValue());
                            }

                            current.Dispose();
                            current = next;

                            goto LoopCurrent;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch
                {
                    foreach (var f in frames) f.Dispose();

                    frames.Clear();
                    throw;
                }
            }
        }

        public void Invoke(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(function, inputValues);
            Run();
        }

        internal void Invoke(IStackFrame frame)
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

        internal ValueTask InvokeAsync(IStackFrame frame)
        {
            lock (locker)
            {
                if (frames.Count > 0) throw new InvalidOperationException();
                PushFrame(frame);
                return RunAsync();
            }
        }

        internal IStackFrame PushFrame(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            lock (locker)
            {
                if (frames.Count >= maxStackFrames) throw new InvalidOperationException();
                if (frames.TryPeek(out var top) && top is not WasmStackFrame && function is not InstanceFunction)
                    throw new InvalidOperationException();

                var frame = function.CreateFrame(this, inputValues);
                frames.Push(frame);
                return frame;
            }
        }

        internal IStackFrame PushFrame(IStackFrame frame)
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
                MoveToEndOrPending(frame, out var pending);

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
            MoveToEndOrPending(Current, out var pending);

            if (pending)
            {
                Dispose();
                throw new InvalidOperationException("Use RunAsync() instead of Run() to use coroutines.");
            }
        }

        private async ValueTask RunAsync()
        {
            var initial = Current;
            while (true)
            {
                var source = currentPendingTaskSource ??= ValueTaskSource.Create();
                MoveToEndOrPending(initial, out var pending);

                if (!pending) return;

                currentPendingTaskSource = null;

                await source.AsValueTask().ConfigureAwait(false);
            }
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
            source.SetResult();
        }

        public void Fail(Exception ex)
        {
            source.SetException(ex);
        }
    }
}