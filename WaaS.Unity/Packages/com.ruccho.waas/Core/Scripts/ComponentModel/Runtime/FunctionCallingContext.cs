using System;
using System.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    public interface IFunctionBinderCore : IVersionedDisposable<ushort>
    {
        ValuePusher ArgumentPusher { get; }
        IStackFrame CreateFrame();
        void TakeResults(ValuePusher resultValuePusher);
    }

    public readonly struct FunctionBinder : IDisposable
    {
        private readonly IFunctionBinderCore core;
        private readonly ushort version;

        public FunctionBinder(IFunctionBinderCore core)
        {
            this.core = core;
            version = core.Version;
        }

        private void ThrowIfDisposed()
        {
            if (core.Version != version) throw new InvalidOperationException();
        }

        public void Dispose()
        {
            core.Dispose(version);
        }

        public ValuePusher ArgumentPusher
        {
            get
            {
                ThrowIfDisposed();
                return core.ArgumentPusher;
            }
        }

        internal IStackFrame CreateFrame()
        {
            ThrowIfDisposed();
            return core.CreateFrame();
        }

        public void Invoke(ExecutionContext context)
        {
            ThrowIfDisposed();
            context.Invoke(core.CreateFrame());
        }

        public void TakeResults(ValuePusher resultValuePusher)
        {
            ThrowIfDisposed();
            core.TakeResults(resultValuePusher);
        }

        public TResult TakeResult<TResult>(Func<Pullable, ValueTask<TResult>> resultPuller)
        {
            PushPullAdapter.Get(out var pullable, out var resultPusher);
            using (pullable)
            using (resultPusher)
            {
                var task = resultPuller(pullable);
                TakeResults(resultPusher);
                return task.Result;
            }
        }

        public TResult TakeResult<TResult>()
        {
            static ValueTask<TResult> PullAsync(Pullable pullable)
            {
                return pullable.PullValueAsync<TResult>();
            }

            return TakeResult(static pullable => PullAsync(pullable));
        }
    }
}