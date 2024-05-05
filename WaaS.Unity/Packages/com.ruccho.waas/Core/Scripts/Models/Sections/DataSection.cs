using System;

namespace WaaS.Models
{
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

    public class Data
    {
        internal Data(ref ModuleReader reader)
        {
            MemoryIndex = reader.ReadUnalignedLeb128U32();
            Offset = new ConstantExpression(ref reader);
            var numBytes = reader.ReadVectorSize();
            Payload = reader.Read(checked((int)numBytes)).ToArray();
        }

        public uint MemoryIndex { get; }
        public ConstantExpression Offset { get; }
        public ReadOnlyMemory<byte> Payload { get; }
    }
}