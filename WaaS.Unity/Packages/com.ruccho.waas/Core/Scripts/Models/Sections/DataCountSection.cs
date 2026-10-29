namespace WaaS.Models
{
    /// <summary>
    ///     Data count section in a WebAssembly module.
    /// </summary>
    public class DataCountSection : Section
    {
        internal DataCountSection(ref ModuleReader reader)
        {
            Count = reader.ReadUnalignedLeb128U32();
        }

        public uint Count { get; }
    }
}
