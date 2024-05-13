using System;
using System.Collections.Generic;

namespace WaaS.Runtime
{
    public class ExecutionContext : IDisposable
    {
        private readonly Stack<StackFrame> frames = new();
        private readonly uint? maxStackFrames;

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

        public bool MoveNext()
        {
            try
            {
                var current = Current;
                if (current is null) throw new InvalidOperationException();

                if (!current.MoveNext())
                {
                    frames.Pop();
                    if (frames.TryPeek(out var next) && next is WasmStackFrame nextWasm)
                    {
                        Span<StackValueItem> results = stackalloc StackValueItem[current.ResultLength];
                        current.TakeResults(results);
                        current.Dispose();

                        foreach (var value in results) nextWasm.Push(value.ExpectValue());
                    }
                    else
                    {
                        LastFrame = current;
                        return false;
                    }
                }
            }
            catch
            {
                foreach (var f in frames) f.Dispose();

                frames.Clear();
                throw;
            }

            return true;
        }

        public void Invoke(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
        {
            if (frames.Count > 0) throw new InvalidOperationException();
            PushFrame(function, inputValues);
            Run();
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
                _ => throw new InvalidOperationException()
            };
            frames.Push(frame);
        }

        private void Run()
        {
            // TODO: async
            while (MoveNext()) ;
        }

        public void TakeResults(Span<StackValueItem> results)
        {
            LastFrame.TakeResults(results);
        }
    }
}