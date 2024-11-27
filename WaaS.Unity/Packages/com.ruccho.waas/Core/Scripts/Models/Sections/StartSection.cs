namespace WaaS.Models
{
    /// <summary>
    ///     Start section in a WebAssembly module.
    /// </summary>
    public class StartSection : Section
    {
        internal StartSection(ref ModuleReader reader)
        {
            FunctionIndex = reader.ReadUnalignedLeb128U32();
        }

        public uint FunctionIndex { get; }
    }
}