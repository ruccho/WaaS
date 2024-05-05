using System;

namespace WaaS.Models
{
    public class CustomSection : Section
    {
        internal CustomSection(ref ModuleReader reader, uint size)
        {
            var next = reader.Position + size;
            Name = reader.ReadUtf8String();

            var contentSize = next - reader.Position;
            Data = reader.Read(checked((int)contentSize)).ToArray();
        }

        public string Name { get; }
        public ReadOnlyMemory<byte> Data { get; }
    }
}