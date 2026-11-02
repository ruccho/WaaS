#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WaaS.ComponentModel.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Runtime
{
    public class LiftedFunction : IFunction, ICanonOptions
    {
        public LiftedFunction(IInvocableFunction coreFunction, IFunctionType type,
            CanonOptionStringEncodingKind stringEncoding, IInvocableFunction? reallocFunction,
            IInvocableFunction? postReturnFunction, Memory? memoryToRealloc, IInstance instance)
        {
            MemoryToRealloc = memoryToRealloc;
            CoreFunction = coreFunction;
            Type = type;
            StringEncoding = stringEncoding;
            ReallocFunction = reallocFunction;
            PostReturnFunction = postReturnFunction;
            Instance = instance;
        }

        public IInvocableFunction? PostReturnFunction { get; }
        public IInvocableFunction CoreFunction { get; }
        internal IInstance Instance { get; }

        public CanonOptionStringEncodingKind StringEncoding { get; }

        public IInvocableFunction? ReallocFunction { get; }
        public Memory? MemoryToRealloc { get; }

        public IFunctionType Type { get; }

        /// <summary>
        ///     Whether <see cref="InvokeDirectAsync" /> is available.
        /// </summary>
        public bool SupportsDirectInvoke => PostReturnFunction == null;

        /// <summary>
        ///     Invokes the core function with the core values of the parameters and returns the core value of the
        ///     result, without the formatters. The parameters and the result must not need lifting or lowering.
        /// </summary>
        public ValueTask<StackValueItem> InvokeDirectAsync(ExecutionContext context,
            ReadOnlySpan<StackValueItem> parameters)
        {
            var task = context.InvokeAsync(CoreFunction, parameters);
            if (!task.IsCompletedSuccessfully) return WaitForResultAsync(task, context);

            task.GetAwaiter().GetResult();
            return new ValueTask<StackValueItem>(TakeResult(context));
        }

        private async ValueTask<StackValueItem> WaitForResultAsync(ValueTask task, ExecutionContext context)
        {
            await task;
            return TakeResult(context);
        }

        private StackValueItem TakeResult(ExecutionContext context)
        {
            var result = default(StackValueItem);
            if (CoreFunction.Type.ResultTypes.Length != 0)
            {
                Span<StackValueItem> results = stackalloc StackValueItem[1];
                context.TakeResults(results);
                result = results[0];
            }

            context.ReleaseLastFrame();
            return result;
        }

        public FunctionBinder GetBinder(ExecutionContext context)
        {
            return new FunctionBinder(Binder.Get(context, this));
        }

        private class Binder : IFunctionBinderCore, ICanonContext
        {
            private static readonly Stack<Binder> Pool = new();
            private StackFrame frame;
            private LiftedFunction? function;
            private int invoked;
            private StackValueItems stackValueItems;

            private ExecutionContext? Context { get; set; }
            public IInstance Instance => function?.Instance ?? throw new InvalidOperationException();
            public ICanonOptions Options => function ?? throw new InvalidOperationException();

            public IFunction ComponentFunction => function ?? throw new InvalidOperationException();

            public uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
            {
                Span<StackValueItem> args = stackalloc StackValueItem[5];
                args[0] = new StackValueItem(originalPtr);
                args[1] = new StackValueItem(originalSize);
                args[2] = new StackValueItem(alignment);
                args[3] = new StackValueItem(newSize);
                Context!.InterruptFrame(function!.ReallocFunction, args[..4], args[4..]);
                var ptr = args[4].ExpectValueI32();
                if (alignment != 0 && (ptr & (alignment - 1)) != 0)
                    throw new TrapException("Realloc returned unaligned pointer");
                return ptr;
            }

            public ushort Version { get; private set; }

            public ValuePusher ArgumentPusher { get; private set; }

            public void Dispose(ushort version)
            {
                if (version != Version) return;
                ArgumentPusher.Dispose();
                frame.Dispose();
                if (++Version == ushort.MaxValue) return;
                Pool.Push(this);
            }

            public StackFrame CreateFrame()
            {
                if (Interlocked.CompareExchange(ref invoked, 1, 0) != 0) throw new InvalidOperationException();
                return frame = function!.CoreFunction.CreateFrame(Context, stackValueItems.UnsafeItems);
            }

            public unsafe void TakeResults(ValuePusher resultValuePusher)
            {
                var coreResultTypes = function!.CoreFunction.Type.ResultTypes;
                StackValueItem* resultValuesPtr = stackalloc StackValueItem[frame.ResultLength];
                Span<StackValueItem> resultValues = new(resultValuesPtr, frame.ResultLength);
                frame.TakeResults(resultValues);
                frame.Dispose();
                frame = default;
                // lift result values
                var resultType = function.Type.Result?.Despecialize();
                if (resultType != null)
                {
                    var flatten = resultType.FlattenedCount <= 1 /* MAX_FLAT_RESULTS */;

                    ValueLifter lifter;
                    if (flatten)
                    {
                        lifter = new ValueLifter(this, ElementTypeSelector.FromSingle(resultType), resultValues);
                    }
                    else
                    {
                        if (coreResultTypes.Length != 1)
                            throw new InvalidOperationException();

                        var ptr = resultValues[0].ExpectValueI32();

                        if (Utils.ElementSizeAlignTo(ptr, resultType.AlignmentRank) != ptr)
                            throw new TrapException("Result pointer is not aligned");

                        lifter = new ValueLifter(this, ElementTypeSelector.FromSingle(resultType),
                            function.MemoryToRealloc!.Span.Slice(checked((int)ptr)));
                    }

                    ValueTransfer.TransferNext(ref lifter, resultValuePusher);
                }

                // post-return

                // TODO: make async?
                var postReturn = function.PostReturnFunction;
                if (postReturn != null) Context!.InterruptFrame(postReturn, resultValues, Span<StackValueItem>.Empty);
            }

            private void Reset(ExecutionContext context, LiftedFunction function)
            {
                Context = context;
                this.function = function;
                invoked = 0;
                frame = default;
                ArgumentPusher = LoweringPusherBase
                    .GetRoot(this, true, 16 /* MAX_FLAT_PARAMS */, out stackValueItems).Wrap();
            }

            public static Binder Get(ExecutionContext context, LiftedFunction function)
            {
                if (!Pool.TryPop(out var pooled)) pooled = new Binder();

                pooled.Reset(context, function);
                return pooled;
            }
        }
    }
}