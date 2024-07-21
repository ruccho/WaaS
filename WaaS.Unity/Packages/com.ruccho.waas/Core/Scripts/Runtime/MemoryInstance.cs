using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class MemoryInstance
    {
        public MemoryInstance(MemorySection memorySection, ImportSection importSection, IImports imports,
            DataSection dataSection, GlobalInstance globalInstance)
        {
            var internalMemoryTypes = memorySection != null ? memorySection.MemoryTypes.Span : Span<MemoryType>.Empty;
            var numMemories = internalMemoryTypes.Length;
            var importItems = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;
            foreach (var import in importItems)
                if (import.Description.MemoryType.HasValue)
                    numMemories++;

            if (numMemories > 1) throw new InvalidModuleException("multiple memories");

            var memories = new Memory[numMemories];
            var cursor = 0;

            foreach (var import in importItems)
            {
                var t = import.Description.MemoryType;

                if (t.HasValue)
                {
                    if (!imports.TryGetImportable(import.ModuleName, import.Name, out Memory importedMemory))
                        throw new InvalidOperationException();

                    if (!importedMemory.PageLimits.IsImportable(t.Value.Limits))
                        throw new
                            InvalidModuleException("incompatible import type");

                    checked
                    {
                        memories[cursor++] = importedMemory;
                    }
                }
            }

            foreach (var memoryType in internalMemoryTypes)
                checked
                {
                    memories[cursor++] = new Memory(memoryType.Limits);
                }

            // validation
            var data = dataSection != null ? dataSection.Data.Span : Span<Data>.Empty;
            foreach (var d in data)
            {
                var offset = checked((int)d.Offset.Evaluate(globalInstance).ExpectValueI32());
                var memoryIndex = d.MemoryIndex;
                var memory = memories[memoryIndex];

                if (memory.Length < offset + d.Payload.Length) throw new InvalidModuleException();
            }

            Memories = memories;
        }

        public ReadOnlyMemory<Memory> Memories { get; }

        public void Initialize(DataSection dataSection, GlobalInstance globalInstance)
        {
            var memories = Memories.Span;
            var data = dataSection != null ? dataSection.Data.Span : Span<Data>.Empty;
            foreach (var d in data)
            {
                var offset = checked((int)d.Offset.Evaluate(globalInstance).ExpectValueI32());
                var memoryIndex = d.MemoryIndex;
                var memory = memories[checked((int)memoryIndex)];
                var dest = memory.Span[offset..];
                d.Payload.Span.CopyTo(dest);
            }
        }
    }
}