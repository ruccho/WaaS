using WaaS.ComponentModel.Runtime;

namespace Wukuro
{
    public interface IImportProvider<out T> where T : ISortedExportable
    {
        string Name { get; }
        T Provide();
    }
}