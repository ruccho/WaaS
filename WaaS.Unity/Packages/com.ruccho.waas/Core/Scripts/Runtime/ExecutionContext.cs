using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaaS.Runtime
{
    public class ExecutionContext
    {
        private readonly Stack<StackFrame> frames = new();
        private readonly uint? maxStackFrames;

        public ExecutionContext(uint? maxStackFrames = null)
        {
            this.maxStackFrames = maxStackFrames;
        }

        private StackFrame Current => frames.Peek();

        internal void Trap()
        {
            throw new TrapException();
        }

        public bool MoveNext(out StackFrame lastFrame)
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
                        foreach (var value in results) nextWasm.Push(value.ExpectValue());
                    }
                    else
                    {
                        lastFrame = current;
                        return false;
                    }
                }
            }
            catch
            {
                frames.Clear();
                throw;
            }

            lastFrame = null;
            return true;
        }

        public ValueTask<StackFrame> InvokeAsync(IInvocableFunction function, ReadOnlySpan<StackValueItem> inputValues)
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
                _ => throw new InvalidOperationException()
            };
            frames.Push(frame);
        }

        private async ValueTask<StackFrame> RunAsync()
        {
            // TODO: async
            StackFrame lastFrame;
            while (MoveNext(out lastFrame)) ;
            return lastFrame;
        }
    }
}