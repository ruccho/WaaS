#nullable enable

using System;
using System.Collections.Generic;
using WaaS.ComponentModel.Models;
using WaaS.Runtime;
using FunctionType = WaaS.Models.FunctionType;

namespace WaaS.ComponentModel.Runtime
{
    public class LoweredFunction : ICoreSortedExportable<IInvocableFunction>, IInvocableFunction, ICanonOptions,
        ICanonContext
    {
        private readonly bool flattenParameters;
        private readonly bool hasSerializedResult;
        private readonly IDespecializedValueType[] parameterTypes;
        private readonly IRecordType parameterType;
        private readonly IDespecializedValueType? resultType;

        public LoweredFunction(IInstance instance, CanonOptionStringEncodingKind stringEncoding,
            IInvocableFunction? reallocFunction,
            Memory? memoryToRealloc, IFunction componentFunction)
        {
            StringEncoding = stringEncoding;
            ReallocFunction = reallocFunction;
            MemoryToRealloc = memoryToRealloc;
            ComponentFunction = componentFunction;
            Instance = instance;

            var type = componentFunction.Type;
            var paramType = type.ParameterType;
            var paramFlattenedCount = paramType.FlattenedCount;

            // result
            var hasSerializedResult = false;
            var resultType = type.Result?.Despecialize();
            var resultFlattenedCount = resultType?.FlattenedCount ?? 0;
            ValueType[] resultTypes;
            if (resultFlattenedCount > 1)
            {
                resultTypes = Array.Empty<ValueType>();
                hasSerializedResult = true;
            }
            else if (resultFlattenedCount == 1)
            {
                resultType!.Flatten(resultTypes = new ValueType[resultFlattenedCount]);
            }
            else
            {
                resultTypes = Array.Empty<ValueType>();
            }

            // params
            ValueType[] paramTypes;
            if (paramFlattenedCount > 16)
            {
                paramTypes = new ValueType[hasSerializedResult ? 2 : 1];
                paramTypes[0] = ValueType.I32;
            }
            else
            {
                paramTypes = new ValueType[hasSerializedResult ? paramFlattenedCount + 1 : paramFlattenedCount];
                paramType.Flatten(hasSerializedResult ? paramTypes.AsSpan()[..^1] : paramTypes);
            }

            if (hasSerializedResult) paramTypes[^1] = ValueType.I32;

            Type = new FunctionType(paramTypes, resultTypes);
            this.hasSerializedResult = hasSerializedResult;
            this.resultType = resultType;
            parameterType = paramType;
            var parameters = type.Parameters.Span;
            parameterTypes = new IDespecializedValueType[parameters.Length];
            for (var i = 0; i < parameters.Length; i++) parameterTypes[i] = parameters[i].Type.Despecialize();
            flattenParameters = paramFlattenedCount <= 16 /* MAX_FLAT_PARAMS */;
        }

        public IInstance Instance { get; }
        public ICanonOptions Options => this;
        public IFunction ComponentFunction { get; }

        uint ICanonContext.Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
        {
            // lifting the arguments never allocates in the component memory
            throw new InvalidOperationException();
        }

        public CanonOptionStringEncodingKind StringEncoding { get; }
        public IInvocableFunction? ReallocFunction { get; }
        public Memory? MemoryToRealloc { get; }

        public IInvocableFunction CoreExternal => this;
        public FunctionType Type { get; }

        public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            var inputValuesParams = hasSerializedResult ? inputValues[..^1] : inputValues;

            ValueLifter lifter;
            {
                var typeSelector = ElementTypeSelector.FromRecord(parameterType);

                if (!flattenParameters && inputValuesParams.Length != 1) throw new InvalidOperationException();

                if (flattenParameters)
                {
                    lifter = new ValueLifter(this, typeSelector, inputValuesParams);
                }
                else
                {
                    var ptr = inputValuesParams[0].ExpectValueI32();
                    if (Utils.ElementSizeAlignTo(ptr, parameterType.AlignmentRank) != ptr)
                        throw new TrapException("Parameter pointer is not aligned");
                    lifter = new ValueLifter(this, typeSelector,
                        MemoryToRealloc!.Span[checked((int)ptr)..]);
                }
            }

            uint? serializedResultPtr = null;
            if (hasSerializedResult)
            {
                var ptr = inputValues[^1].ExpectValueI32();
                if (Utils.ElementSizeAlignTo(ptr, resultType!.AlignmentRank) != ptr)
                    throw new TrapException("Result pointer is not aligned");
                serializedResultPtr = ptr;
            }

            var binder = ComponentFunction.GetBinder(context);
            var argumentPusher = binder.ArgumentPusher;
            foreach (var parameterType in parameterTypes)
                ValueTransfer.TransferNext(ref lifter, argumentPusher, parameterType);
            return new StackFrame(Frame.Get(context, this, binder.CreateFrame(), binder,
                serializedResultPtr));
        }

        private class Frame : IStackFrameCore, ICanonContext
        {
            [ThreadStatic] private static Stack<Frame>? pool;

            // the flattened result is at most one value
            private readonly StackValueItem[] flattenedResult = new StackValueItem[1];
            private FlattenedLoweringPusher? flattenedResultPusher;
            private FunctionBinder binder;
            private ExecutionContext? context;

            private LoweredFunction? function;
            private StackFrame internalFrame;
            private uint? serializedResultPtr;

            public ushort Version { get; private set; }

            public IInstance Instance => function?.Instance ?? throw new InvalidOperationException();
            public ICanonOptions Options => function ?? throw new InvalidOperationException();
            public IFunction ComponentFunction => function?.ComponentFunction ?? throw new InvalidOperationException();

            // used to lower the results (e.g. strings and lists) into the component memory
            public uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
            {
                var reallocFunction = function!.ReallocFunction ??
                                      throw new TrapException("The realloc option is not specified.");

                Span<StackValueItem> args = stackalloc StackValueItem[5];
                args[0] = new StackValueItem(originalPtr);
                args[1] = new StackValueItem(originalSize);
                args[2] = new StackValueItem(alignment);
                args[3] = new StackValueItem(newSize);
                context!.InterruptFrame(reallocFunction, args[..4], args[4..]);
                var ptr = args[4].ExpectValueI32();
                if (alignment != 0 && (ptr & (alignment - 1)) != 0)
                    throw new TrapException("Realloc returned unaligned pointer");
                return ptr;
            }

            public int GetResultLength(ushort version)
            {
                ThrowIfOutdated(version);
                if (function!.hasSerializedResult)
                    return 0;
                return function!.ComponentFunction.Type.Result != null ? 1 : 0;
            }

            public void Dispose(ushort version)
            {
                if (version != Version) return;
                internalFrame.Dispose();
                internalFrame = default;

                binder.Dispose();
                binder = default;
                context = null;
                function = null;

                if (++Version == ushort.MaxValue) return;
                pool ??= new Stack<Frame>();
                pool.Push(this);
            }

            public StackFrameState MoveNext(ushort version, Waker waker)
            {
                ThrowIfOutdated(version);
                return internalFrame.MoveNext(waker);
            }

            public void TakeResults(ushort version, Span<StackValueItem> dest)
            {
                ThrowIfOutdated(version);
                if (function!.hasSerializedResult)
                {
                    using var pusher = LoweringPusherBase
                        .GetRootForSerializedResult(this, serializedResultPtr!.Value).Wrap();
                    binder.TakeResults(pusher);
                }
                else
                {
                    var resultType = function.resultType;
                    var count = checked((int)(resultType?.FlattenedCount ?? 0));
                    flattenedResult[0] = default;
                    if (flattenedResultPusher == null || flattenedResultPusher.Version == ushort.MaxValue)
                        flattenedResultPusher = FlattenedLoweringPusher.CreateOwned();
                    using (var pusher = flattenedResultPusher.InitRoot(this, flattenedResult.AsMemory(0, count),
                               ElementTypeSelector.FromSingle(resultType)).Wrap())
                    {
                        binder.TakeResults(pusher);
                    }

                    flattenedResult.AsSpan(0, count).CopyTo(dest);
                }
            }

            public bool DoesTakeResults(ushort version)
            {
                ThrowIfOutdated(version);
                return internalFrame.DoesTakeResults();
            }

            public void PushResults(ushort version, Span<StackValueItem> source)
            {
                ThrowIfOutdated(version);
                internalFrame.PushResults(source);
            }

            private void Reset(ExecutionContext context, LoweredFunction function, StackFrame internalFrame,
                FunctionBinder binder, uint? serializedResultPtr)
            {
                this.context = context;
                this.function = function;
                this.internalFrame = internalFrame;
                this.binder = binder;
                this.serializedResultPtr = serializedResultPtr;
            }

            public static Frame Get(ExecutionContext context, LoweredFunction function, StackFrame internalFrame,
                FunctionBinder binder, uint? serializedResultPtr)
            {
                pool ??= new Stack<Frame>();
                if (!pool.TryPop(out var pooled)) pooled = new Frame();
                pooled.Reset(context, function, internalFrame, binder, serializedResultPtr);
                return pooled;
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (version != Version) throw new InvalidOperationException();
            }
        }
    }
}