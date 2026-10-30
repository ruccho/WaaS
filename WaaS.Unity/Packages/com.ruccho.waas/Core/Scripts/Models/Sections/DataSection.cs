using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Data section in a WebAssembly module.
    /// </summary>
    public class DataSection : Section
    {
        internal DataSection(ref ModuleReader reader)
        {
            var numData = reader.ReadVectorSize();
            var data = new Data[numData];

            for (var i = 0; i < data.Length; i++) data[i] = new Data(ref reader);

            Data = data;
        }

        public ReadOnlyMemory<Data> Data { get; }
    }

    /// <summary>
    ///     Represents a single data segment in a data section.
    /// </summary>
    public class Data
    {
        internal Data(ref ModuleReader reader)
        {
            var kind = reader.ReadUnalignedLeb128U32();
            switch (kind)
            {
                case 0:
                    Offset = new ConstantExpression(ref reader);
                    break;
                case 1:
                    IsPassive = true;
                    break;
                case 2:
                    MemoryIndex = reader.ReadUnalignedLeb128U32();
                    Offset = new ConstantExpression(ref reader);
                    break;
                default:
                    throw new InvalidModuleException("malformed data segment kind");
            }

            var numBytes = reader.ReadVectorSize();
            Payload = reader.Read(checked((int)numBytes)).ToArray();
        }

        /// <summary>
        ///     Whether the segment is passive, which is copied to the memory only by <c>memory.init</c>.
        /// </summary>
        public bool IsPassive { get; }

        /// <summary>
        ///     The index of the memory initialized by an active segment.
        /// </summary>
        public uint MemoryIndex { get; }

        /// <summary>
        ///     The offset in the memory initialized by an active segment. <c>null</c> for passive segments.
        /// </summary>
        public IConstantExpression Offset { get; }
        public ReadOnlyMemory<byte> Payload { get; }
    }
}