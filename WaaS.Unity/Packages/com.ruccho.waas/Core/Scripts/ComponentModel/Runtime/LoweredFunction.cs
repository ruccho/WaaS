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
        public LoweredFunction(CanonOptionStringEncodingKind stringEncoding, IInvocableFunction? reallocFunction,
            Memory? memoryToRealloc, IFunction componentFunction)
        {
            StringEncoding = stringEncoding;
            ReallocFunction = reallocFunction;
            MemoryToRealloc = memoryToRealloc;
            ComponentFunction = componentFunction;

            var type = componentFunction.Type;
            var paramType = type.ParameterType;
            var paramFlattenedCount = paramType.FlattenedCount;

            ValueType[] paramTypes;
            if (paramFlattenedCount > 16)
                paramTypes = new[] { ValueType.I32 };
            else
                paramType.Flatten(paramTypes = new ValueType[paramFlattenedCount]);

            var resultType = type.Result?.Despecialize();
            var resultFlattenedCount = resultType?.FlattenedCount ?? 0;
            ValueType[] resultTypes;
            if (resultFlattenedCount > 1)
                resultTypes = new[] { ValueType.I32 };
            else if (resultFlattenedCount == 1)
                resultType!.Flatten(resultTypes = new ValueType[resultFlattenedCount]);
            else
                resultTypes = Array.Empty<ValueType>();

            Type = new FunctionType(paramTypes, resultTypes);
        }

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

            ValueLifter lifter;
            {
                var flatten = count <= 16 /* MAX_FLAT_PARAMS */;
                var typeSelector = ElementTypeSelector.FromRecord(functionType.ParameterType);

                if (!flatten && inputValues.Length != 1) throw new InvalidOperationException();

                lifter = flatten
                    ? new ValueLifter(this, typeSelector, inputValues)
                    : new ValueLifter(this, typeSelector,
                        MemoryToRealloc!.Span[checked((int)inputValues[0].ExpectValueI32())..]);
            }

            var binder = ComponentFunction.GetBinder(context);
            for (var i = 0; i < functionType.Parameters.Length; i++)
                ValueTransfer.TransferNext(ref lifter, binder.ArgumentPusher);
            return new StackFrame(Frame.Get(this, binder.CreateFrame(), binder));
        }

        private class Frame : IStackFrameCore
        {
            [ThreadStatic] private static Stack<Frame>? pool;
            private FunctionBinder binder;

            private LoweredFunction? function;
            private StackFrame internalFrame;

            public ushort Version { get; private set; }

            public int GetResultLength(ushort version)
            {
                ThrowIfOutdated(version);
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
                using var pusher = LoweringPusherBase
                    .GetRoot(function ?? throw new InvalidOperationException(), false, 1, out var items).Wrap();
                binder.TakeResults(pusher);
                items.UnsafeItems.CopyTo(dest);
            }

            private void Reset(LoweredFunction function, StackFrame internalFrame, FunctionBinder binder)
            {
                this.function = function;
                this.internalFrame = internalFrame;
                this.binder = binder;
            }

            public static Frame Get(LoweredFunction function, StackFrame internalFrame, FunctionBinder binder)
            {
                pool ??= new Stack<Frame>();
                if (!pool.TryPop(out var pooled)) pooled = new Frame();
                pooled.Reset(function, internalFrame, binder);
                return pooled;
            }

            private void ThrowIfOutdated(ushort version)
            {
                if (version != Version) throw new InvalidOperationException();
            }
        }
    }
}