using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents an instance of a memory.
    /// </summary>
    public class MemoryInstance
    {
        private readonly ReadOnlyMemory<byte>[] dataSegments;

        public MemoryInstance(MemorySection memorySection, ImportSection importSection, IImports imports,
            DataSection dataSection, GlobalInstance globalInstance)
        {
            var internalMemoryTypes = memorySection != null ? memorySection.MemoryTypes.Span : Span<MemoryType>.Empty;
            var numMemories = internalMemoryTypes.Length;
            var importItems = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;
            foreach (var import in importItems)
                if (import.Descriptor.MemoryType.HasValue)
                    numMemories++;

            if (numMemories > 1) throw new InvalidModuleException("multiple memories");

            var memories = new Memory[numMemories];
            var cursor = 0;

            foreach (var import in importItems)
            {
                var t = import.Descriptor.MemoryType;

                if (t.HasValue)
                {
                    if (!imports.TryGetImportable(import.ModuleName, import.Name, out Memory importedMemory))
                        throw new InvalidOperationException();

                    // the current size is compared with the minimum
                    if (!new Limits((uint)importedMemory.NumPages, importedMemory.PageLimits.Max)
                            .IsImportable(t.Value.Limits))
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

            var data = dataSection != null ? dataSection.Data.Span : Span<Data>.Empty;
            dataSegments = new ReadOnlyMemory<byte>[data.Length];
            for (var i = 0; i < data.Length; i++) dataSegments[i] = data[i].Payload;

            Memories = memories;
            MemoryArray = memories;
        }

        public ReadOnlyMemory<Memory> Memories { get; }

        internal Memory[] MemoryArray { get; }

        /// <summary>
        ///     Initializes the memories with the active segments, and drops the active segments.
        /// </summary>
        /// <exception cref="TrapException">A segment is out of the bounds of the memory.</exception>
        public void Initialize(DataSection dataSection, GlobalInstance globalInstance)
        {
            var data = dataSection != null ? dataSection.Data.Span : Span<Data>.Empty;
            for (var i = 0; i < data.Length; i++)
            {
                var d = data[i];
                if (d.IsPassive) continue;

                var offset = d.Offset.Evaluate(globalInstance).ExpectValueI32();
                Init(d.MemoryIndex, (uint)i, offset, 0, (uint)d.Payload.Length);
                DropDataSegment((uint)i);
            }
        }

        internal void Init(uint memoryIndex, uint dataIndex, uint destOffset, uint srcOffset, uint count)
        {
            var memory = MemoryArray[memoryIndex].Span;
            var segment = dataSegments[dataIndex].Span;
            if ((ulong)srcOffset + count > (ulong)segment.Length || (ulong)destOffset + count > (ulong)memory.Length)
                throw new TrapException("out of bounds memory access");
            segment.Slice((int)srcOffset, (int)count).CopyTo(memory[(int)destOffset..]);
        }

        internal void DropDataSegment(uint dataIndex)
        {
            dataSegments[dataIndex] = ReadOnlyMemory<byte>.Empty;
        }
    }
}