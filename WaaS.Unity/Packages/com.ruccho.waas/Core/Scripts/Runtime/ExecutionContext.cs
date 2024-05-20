using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaaS.Runtime
{
    public class ExecutionContext : IDisposable
    {
        private readonly Stack<StackFrame> frames = new();
        private readonly uint? maxStackFrames;
        private ValueTaskSource currentPendingTaskSource;

        public ExecutionContext(uint? maxStackFrames = null)
        {
            this.maxStackFrames = maxStackFrames;
        }

        public StackFrame LastFrame { get; private set; }

        private StackFrame Current => frames.Peek();

        public int ResultLength => LastFrame.ResultLength;

        public void Dispose()
        {
            LastFrame?.Dispose();
            LastFrame = null;
        }

        private void MoveToEndOrPending(out bool pending)
        {
            try
            {
                Loop:
                var current = Current;
                if (current is null) throw new InvalidOperationException();

                LoopCurrent:
                switch (current.MoveNext(new Waker(currentPendingTaskSource)))
                {
                    case StackFrameState.Ready:
                    {
                        goto Loop;
                    }
                    case StackFrameState.Pending:
                    {
                        pending = true;
                        return;
                    }
                    case StackFrameState.Completed:
                    {
                        frames.Pop();
                        if (frames.TryPeek(out var next) && next is WasmStackFrame nextWasm)
                        {
                            Span<StackValueItem> results = stackalloc StackValueItem[current.ResultLength];
                            current.TakeResults(results);
                            current.Dispose();

                            foreach (var value in results) nextWasm.Push(value.ExpectValue());

                            current = next;

                            goto LoopCurrent;
                        }

                        // end
                        LastFrame = current;
                        pending = false;
                        return;
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

        public void Invoke(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(function, inputValues);
            Run();
        }

        public ValueTask InvokeAsync(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(function, inputValues);
            return RunAsync();
        }

        internal void PushFrame(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count >= maxStackFrames) throw new InvalidOperationException();
            if (frames.TryPeek(out var top) && top is not WasmStackFrame && function is not InstanceFunction)
                throw new InvalidOperationException();

            StackFrame frame = function switch
            {
                InstanceFunction instanceFunction => new WasmStackFrame(this, instanceFunction, inputValues),
                ExternalFunction externalFunction => new ExternalStackFrame(externalFunction, inputValues),
                AsyncExternalFunction asyncExternalFunction => new AsyncExternalStackFrame(asyncExternalFunction,
                    inputValues),
                _ => throw new InvalidOperationException()
            };
            frames.Push(frame);
        }

        private void Run()
        {
            MoveToEndOrPending(out var pending);

            if (pending) throw new InvalidOperationException("Use RunAsync() instead of Run() to use coroutines.");
        }

        private async ValueTask RunAsync()
        {
            while (true)
            {
                var source = currentPendingTaskSource ??= ValueTaskSource.Create();
                MoveToEndOrPending(out var pending);

                if (!pending) return;

                currentPendingTaskSource = null;

                // wait for wake
                Console.WriteLine("pending");
                await source.AsValueTask();
                Console.WriteLine("pending completed");
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