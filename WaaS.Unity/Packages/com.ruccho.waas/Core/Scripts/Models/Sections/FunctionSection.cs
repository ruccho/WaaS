using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Function section in a WebAssembly module.
    /// </summary>
    public class FunctionSection : Section
    {
        internal FunctionSection(ref ModuleReader reader)
        {
            var numTypeIndices = reader.ReadVectorSize();
            var typeIndices = new uint[numTypeIndices];

            for (var i = 0; i < numTypeIndices; i++) typeIndices[i] = reader.ReadUnalignedLeb128U32();

            TypeIndices = typeIndices;
        }

        public ReadOnlyMemory<uint> TypeIndices { get; }
    }
}