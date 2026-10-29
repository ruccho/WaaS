using System;
using WaaS.ComponentModel.Runtime;

namespace Wukuro
{
    public class DelegateImportProvider<T> : IImportProvider<T> where T : ISortedExportable
    {
        public string Name { get; }
        private readonly Func<T> _provider;

        public DelegateImportProvider(string name, Func<T> provider)
        {
            Name = name;
            _provider = provider;
        }

        public T Provide() => _provider();
    }
}