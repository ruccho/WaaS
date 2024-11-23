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
        private readonly bool hasSerializedResult;

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
        }

        public IInstance Instance { get; }
        public ICanonOptions Options => this;
        public IFunction ComponentFunction { get; }

        uint ICanonContext.Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
        {
            throw new NotImplementedException();
        }

        public CanonOptionStringEncodingKind StringEncoding { get; }
        public IInvocableFunction? ReallocFunction { get; }
        public Memory? MemoryToRealloc { get; }

        public IInvocableFunction CoreExternal => this;
        public FunctionType Type { get; }

        public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            var functionType = ComponentFunction.Type;

            uint count = 0;
            foreach (var parameter in functionType.Parameters.Span)
            {
                if (parameter.Type is not IDespecializedValueType despecialized)
                    despecialized = parameter.Type.Despecialize();

                count += despecialized.FlattenedCount;
            }

            var inputValuesParams = hasSerializedResult ? inputValues[..^1] : inputValues;

            ValueLifter lifter;
            {
                var flatten = count <= 16 /* MAX_FLAT_PARAMS */;
                var typeSelector = ElementTypeSelector.FromRecord(functionType.ParameterType);

                if (!flatten && inputValuesParams.Length != 1) throw new InvalidOperationException();

                if (flatten)
                {
                    lifter = new ValueLifter(this, typeSelector, inputValuesParams);
                }
                else
                {
                    var ptr = inputValuesParams[0].ExpectValueI32();
                    if (Utils.ElementSizeAlignTo(ptr, functionType.ParameterType.AlignmentRank) != ptr)
                        throw new TrapException("Parameter pointer is not aligned");
                    lifter = new ValueLifter(this, typeSelector,
                        MemoryToRealloc!.Span[checked((int)ptr)..]);
                }
            }

            Memory<byte>? serializedResultMemory = null;
            if (hasSerializedResult)
            {
                var ptr = inputValues[^1].ExpectValueI32();
                if (Utils.ElementSizeAlignTo(ptr, functionType.Result!.Despecialize().AlignmentRank) != ptr)
                    throw new TrapException("Result pointer is not aligned");
                serializedResultMemory = MemoryToRealloc!.AsMemory()[(int)ptr..];
            }

            var binder = ComponentFunction.GetBinder(context);
            for (var i = 0; i < functionType.Parameters.Length; i++)
                ValueTransfer.TransferNext(ref lifter, binder.ArgumentPusher);
            return new StackFrame(Frame.Get(this, binder.CreateFrame(), binder,
                serializedResultMemory));
        }

        private class Frame : IStackFrameCore
        {
            [ThreadStatic] private static Stack<Frame>? pool;
            private FunctionBinder binder;

            private LoweredFunction? function;
            private StackFrame internalFrame;
            private Memory<byte>? serializedResultMemory;

            public ushort Version { get; private set; }

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
                        .GetRootForSerializedResult(function, serializedResultMemory!.Value).Wrap();
                    binder.TakeResults(pusher);
                }
                else
                {
                    using var pusher = LoweringPusherBase
                        .GetRoot(function ?? throw new InvalidOperationException(), false, 1, out var items).Wrap();
                    binder.TakeResults(pusher);
                    items.UnsafeItems.CopyTo(dest);
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

            private void Reset(LoweredFunction function, StackFrame internalFrame, FunctionBinder binder,
                Memory<byte>? serializedResultMemory)
            {
                this.function = function;
                this.internalFrame = internalFrame;
                this.binder = binder;
                this.serializedResultMemory = serializedResultMemory;
            }

            public static Frame Get(LoweredFunction function, StackFrame internalFrame, FunctionBinder binder,
                Memory<byte>? serializedResultMemory)
            {
                pool ??= new Stack<Frame>();
                if (!pool.TryPop(out var pooled)) pooled = new Frame();
                pooled.Reset(function, internalFrame, binder, serializedResultMemory);
                return pooled;
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (version != Version) throw new InvalidOperationException();
            }
        }
    }
}