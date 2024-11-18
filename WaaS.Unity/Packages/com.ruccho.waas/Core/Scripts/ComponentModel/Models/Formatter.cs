#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.ComponentModel.Models
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true,
        Inherited = false)]
    internal class GenerateFormatterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    internal class VariantAttribute : Attribute
    {
        public VariantAttribute(byte tag, Type type)
        {
            Tag = tag;
            Type = type;
        }

        public byte Tag { get; }
        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    internal class VariantFallbackAttribute : Attribute
    {
        public VariantFallbackAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class DontAddToSortAttribute : Attribute
    {
    }

    internal static class Formatter<T> where T : notnull
    {
        private static IFormatter<T>? defaultFormatter;

        static Formatter()
        {
            RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
        }

        public static IFormatter<T> Default
        {
            private get => defaultFormatter ?? throw new InvalidOperationException();
            set => defaultFormatter ??= value;
        }

        private static IFormatter<T> UnsafeGetDefault()
        {
            return Default;
        }

        public static T? ReadOptional(ref ModuleReader reader, IIndexSpace indexSpace)
        {
            var notNull = reader.ReadUnaligned<byte>();
            if (notNull == 0) return default;
            if (notNull != 1) throw new InvalidModuleException();
            return Read(ref reader, indexSpace);
        }

        public static T Read(ref ModuleReader reader, IIndexSpace indexSpace, bool addToSort = true)
        {
            var pos = reader.Position;
            if (!UnsafeGetDefault().TryRead(ref reader, indexSpace, out var read))
                throw new InvalidModuleException($"Failed to read {typeof(T)} at 0x{pos:X}");

            if (read is IReadCallbackReceiver<T> receiver) read = receiver.OnAfterRead(indexSpace);

            if (!addToSort || read is not IUnresolved<ISorted> sorted) return read;
            indexSpace.AddUntyped(sorted);
            return read;
        }

        public static bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, [NotNullWhen(true)] out T? result,
            bool addToSort = true)
        {
            if (!UnsafeGetDefault().TryRead(ref reader, indexSpace, out result)) return false;

            if (result is IReadCallbackReceiver<T> receiver) result = receiver.OnAfterRead(indexSpace);

            if (!addToSort || result is not IUnresolved<ISorted> sorted) return true;
            indexSpace.AddUntyped(sorted);
            return true;
        }
    }

    internal interface IFormatter<T>
    {
        bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, [NotNullWhen(true)] out T? result);
    }

    public static class IndexSpaceExtensions
    {
        public static void AddUntyped(this IIndexSpace indexSpace, IUnresolved<ISorted> sorted)
        {
            switch (sorted)
            {
                case IUnresolved<IComponent> component:
                    indexSpace.Add(component);
                    break;
                case IUnresolved<ICoreSortedExportable<IInvocableFunction>> coreFunction:
                    indexSpace.Add(coreFunction);
                    break;
                case IUnresolved<ICoreSortedExportable<Global>> coreGlobal:
                    indexSpace.Add(coreGlobal);
                    break;
                case IUnresolved<ICoreInstance> coreInstance:
                    indexSpace.Add(coreInstance);
                    break;
                case IUnresolved<ICoreSortedExportable<Memory>> coreMemory:
                    indexSpace.Add(coreMemory);
                    break;
                case IUnresolved<ICoreModule> coreModule:
                    indexSpace.Add(coreModule);
                    break;
                case IUnresolved<ICoreSortedExportable<Table>> coreTable:
                    indexSpace.Add(coreTable);
                    break;
                case IUnresolved<ICoreType> coreType:
                    indexSpace.Add(coreType);
                    break;
                case IUnresolved<ICoreSorted> coreSorted:
                    indexSpace.Add(coreSorted);
                    break;
                case IUnresolved<IFunction> function:
                    indexSpace.Add(function);
                    break;
                case IUnresolved<IInstance> instance:
                    indexSpace.Add(instance);
                    break;
                case IUnresolved<IType> type:
                    indexSpace.Add(type);
                    break;
                case IUnresolved<IValue> value:
                    indexSpace.Add(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sorted));
            }
        }
    }

    public interface IIndexSpace
    {
        IIndexSpace? Parent { get; }
        IUnresolved<T> Get<T>(uint index) where T : ISorted;
        void Add<T>(IUnresolved<T> value) where T : ISorted;
    }

    internal class IndexSpace : IIndexSpace
    {
        private readonly object[] sorts =
        {
            new Sort<ICoreSortedExportable<IInvocableFunction>>(),
            new Sort<ICoreSortedExportable<Table>>(),
            new Sort<ICoreSortedExportable<Memory>>(),
            new Sort<ICoreSortedExportable<Global>>(),
            new Sort<ICoreType>(),
            new Sort<ICoreModule>(),
            new Sort<ICoreInstance>(),
            new Sort<IFunction>(),
            new Sort<IValue>(),
            new Sort<IType>(),
            new Sort<IComponent>(),
            new Sort<IInstance>()
        };

        public IndexSpace(IIndexSpace? parent = null)
        {
            Parent = parent;
        }

        public IIndexSpace? Parent { get; }

        public IUnresolved<T> Get<T>(uint index) where T : ISorted
        {
            return sorts.OfType<IReadOnlySort<T>>().Single().Get(index);
        }

        public void Add<T>(IUnresolved<T> value) where T : ISorted
        {
            Add(value, out _);
        }

        public void Add<T>(IUnresolved<T> value, out int index) where T : ISorted
        {
            sorts.OfType<IWriteOnlySort<T>>().Single().Add(value, out index);
        }

        private interface IWriteOnlySort<in T> where T : ISorted
        {
            void Add(IUnresolved<T> value, out int index);
        }

        private interface IReadOnlySort<out T> where T : ISorted
        {
            IUnresolved<T> Get(uint index);
        }

        private class Sort<T> : IWriteOnlySort<T>, IReadOnlySort<T> where T : ISorted
        {
            private readonly List<IUnresolved<T>> items = new();

            public IUnresolved<T> Get(uint index)
            {
#if DEBUG
                try
                {
                    return items[checked((int)index)];
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Index was out of range. Sort {typeof(T)} has {items.Count} but index {index} was specified.",
                        ex);
                }
#else
                return items[checked((int)index)];
#endif
            }

            public void Add(IUnresolved<T> value, out int index)
            {
                index = items.Count;
                items.Add(value);
            }
        }
    }

    public interface IReadCallbackReceiver<out T>
    {
        T OnAfterRead(IIndexSpace indexSpace);
    }

    public interface IUnresolved<out T> where T : ISorted
    {
        T ResolveFirstTime(IInstantiationContext context);
    }
}