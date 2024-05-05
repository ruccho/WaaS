using WaaS.Models;

namespace WaaS.Runtime
{
    public interface IInvocableFunction : IExportItem, IImportItem
    {
        FunctionType Type { get; }
    }

    public class InstanceFunction : IInvocableFunction
    {
        public readonly Function function;
        public readonly Instance instance;

        public InstanceFunction(Instance instance, Function function)
        {
            this.instance = instance;
            this.function = function;
        }

        public FunctionType Type => function.Type;
    }
}