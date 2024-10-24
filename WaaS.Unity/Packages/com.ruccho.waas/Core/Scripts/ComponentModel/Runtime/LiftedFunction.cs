#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using WaaS.ComponentModel.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Runtime
{
    public class LiftedFunction : IFunction, ICanonOptions
    {
        private readonly Memory? memoryToRealloc;

        public LiftedFunction(IInvocableFunction coreFunction, IFunctionType type,
            CanonOptionStringEncodingKind stringEncoding, IInvocableFunction? reallocFunction,
            IInvocableFunction? postReturnFunction, Memory? memoryToRealloc)
        {
            this.memoryToRealloc = memoryToRealloc;
            CoreFunction = coreFunction;
            Type = type;
            StringEncoding = stringEncoding;
            ReallocFunction = reallocFunction;
            PostReturnFunction = postReturnFunction;
        }

        public IInvocableFunction? PostReturnFunction { get; }
        public IInvocableFunction CoreFunction { get; }

        public CanonOptionStringEncodingKind StringEncoding { get; }

        public IInvocableFunction? ReallocFunction { get; }
        public Memory MemoryToRealloc => memoryToRealloc;
        public IFunctionType Type { get; }

        public FunctionBinder GetBinder(ExecutionContext context)
        {
            return new FunctionBinder(Binder.Get(context, this));
        }

        private class Binder : IFunctionBinderCore, ICanonContext
        {
            private static readonly Stack<Binder> Pool = new();
            private IStackFrame? frame;
            private LiftedFunction function;
            private int invoked;
            private StackValueItems stackValueItems;

            public ExecutionContext Context { get; private set; }
            public ICanonOptions Options => function;

            public IFunction ComponentFunction => function;

            public uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
            {
                Span<StackValueItem> args = stackalloc StackValueItem[5];
                args[0] = new StackValueItem(originalPtr);
                args[1] = new StackValueItem(originalSize);
                args[2] = new StackValueItem(alignment);
                args[3] = new StackValueItem(newSize);
                Context.InterruptFrame(function.ReallocFunction, args[..4], args[4..]);
                return args[4].ExpectValueI32();
            }

            public ushort Version { get; private set; }

            public void Dispose(ushort version)
            {
                if (version != Version) return;
                ArgumentPusher.Dispose();
                frame?.Dispose();
                if (++Version == ushort.MaxValue) return;
                Pool.Push(this);
            }

            public ValuePusher ArgumentPusher { get; private set; }

            public IStackFrame CreateFrame()
            {
                if (Interlocked.CompareExchange(ref invoked, 1, 0) != 0) throw new InvalidOperationException();
                return frame = function.CoreFunction.CreateFrame(Context, stackValueItems.UnsafeItems);
            }

            public void TakeResults(ValuePusher resultValuePusher)
            {
                var coreResultTypes = function.CoreFunction.Type.ResultTypes;
                Span<StackValueItem> resultValues = stackalloc StackValueItem[frame!.ResultLength];
                frame.TakeResults(resultValues);
                frame.Dispose();
                frame = default;
                // lift result values
                var resultType = function.Type.Result?.Despecialize();
                if (resultType != null)
                {
                    var flatten = resultType.FlattenedCount <= 1 /* MAX_FLAT_RESULTS */;

                    if (!flatten)
                        if (coreResultTypes.Length != 1)
                            throw new InvalidOperationException();

                    var lifter = flatten
                        ? new ValueLifter(this, ElementTypeSelector.FromSingle(resultType), resultValues)
                        : new ValueLifter(this, ElementTypeSelector.FromSingle(resultType),
                            function.MemoryToRealloc.Span.Slice(checked((int)resultValues[0].ExpectValueI32())));

                    ValueTransfer.TransferNext(ref lifter, resultValuePusher);
                }

                // post-return

                // TODO: make async
                var postReturn = function.PostReturnFunction;
                if (postReturn != null) Context.InterruptFrame(postReturn, resultValues, Span<StackValueItem>.Empty);
            }

            private void Reset(ExecutionContext context, LiftedFunction function)
            {
                Context = context;
                this.function = function;
                invoked = 0;
                frame = null;
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