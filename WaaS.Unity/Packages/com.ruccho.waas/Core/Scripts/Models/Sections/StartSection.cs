namespace WaaS.Models
{
    public class StartSection : Section
    {
        internal StartSection(ref ModuleReader reader)
        {
            FunctionIndex = reader.ReadUnalignedLeb128U32();
        }

        public uint FunctionIndex { get; }
    }
}