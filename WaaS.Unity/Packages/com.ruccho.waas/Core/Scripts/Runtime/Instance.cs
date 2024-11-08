using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     An instance of a WebAssembly module.
    /// </summary>
    public class Instance : IDisposable, IModuleExports
    {
        private bool isDisposed;

        public Instance(Module module, IImports importObject)
        {
            var importSection = module.ImportSection;
            var globalSection = module.GlobalSection;
            var elementSection = module.ElementSection;
            var tableSection = module.TableSection;
            var dataSection = module.DataSection;
            var startSection = module.StartSection;
            var exportSection = module.ExportSection;

            var memorySection = module.MemorySection;

            GlobalInstance = new GlobalInstance(importSection, globalSection, importObject);

            MemoryInstance =
                new MemoryInstance(memorySection, importSection, importObject, dataSection, GlobalInstance);

            Module = module;

            FunctionInstance = new FunctionInstance(this, importSection, importObject);

            TableInstance = new TableInstance(GlobalInstance, FunctionInstance, importSection, tableSection,
                elementSection, importObject);

            ExportInstance =
                new ExportInstance(FunctionInstance, exportSection, TableInstance,
                    MemoryInstance, GlobalInstance);

            MemoryInstance.Initialize(dataSection, GlobalInstance);
            TableInstance.Initialize(FunctionInstance, GlobalInstance, elementSection);

            // NOTE: should we support async start?
            if (startSection != null)
            {
                var startFunction = FunctionInstance.Functions.Span[checked((int)startSection.FunctionIndex)];
                var type = startFunction.Type;
                if (type.ParameterTypes.Length != 0 || type.ResultTypes.Length != 0) throw new InvalidModuleException();
                new ExecutionContext(ushort.MaxValue).Invoke(startFunction, Span<StackValueItem>.Empty);
            }
        }

        public Module Module { get; }

        public GlobalInstance GlobalInstance { get; }
        public TableInstance TableInstance { get; }
        public ExportInstance ExportInstance { get; }
        public MemoryInstance MemoryInstance { get; }
        public FunctionInstance FunctionInstance { get; }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        public bool TryGetExport<T>(string name, out T value) where T : IExternal
        {
            if (ExportInstance.Items.TryGetValue(name, out var item) && item is T valueTyped)
            {
                value = valueTyped;
                return true;
            }

            value = default;
            return false;
        }

        public Span<byte> GetMemory(uint index)
        {
            return MemoryInstance.Memories.Span[checked((int)index)].Span;
        }

        public bool TryGrowMemory(uint index, int numPages, out int oldNumPages)
        {
            var memory = MemoryInstance.Memories.Span[checked((int)index)];
            oldNumPages = memory.NumPages;
            return memory.TryGrow(numPages);
        }

        private void DisposeCore()
        {
            if (isDisposed) return;
            isDisposed = true;
        }

        ~Instance()
        {
            DisposeCore();
        }
    }
}