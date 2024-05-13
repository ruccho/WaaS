using System;

namespace WaaS.Runtime
{
    public abstract class StackFrame : IDisposable
    {
        public abstract int ResultLength { get; }

        public abstract void Dispose();
        public abstract bool MoveNext();

        public abstract void TakeResults(Span<StackValueItem> dest);
    }
}