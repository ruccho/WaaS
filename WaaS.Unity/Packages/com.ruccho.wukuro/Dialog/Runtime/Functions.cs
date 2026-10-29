using System.Threading.Tasks;
using WaaS.ComponentModel.Binding;

namespace Wukuro.Dialog
{
    public class Functions : IFunctions
    {
        private static DialogModuleImpl DialogModuleType => Types.DialogModuleType;

        public Functions(Core.ITypes.IContextResourceImpl contextResourceImpl)
        {
            Context = contextResourceImpl;
        }

        public async ValueTask<Owned<ITypes.IDialogModuleResourceImpl>> GetDialogModule(
            Borrowed<Core.ITypes.IContextResourceImpl> context)
        {
            var dialogModule = WukuroContexts.GetModule<IDialogModule>(context);
            return (Owned<ITypes.IDialogModuleResourceImpl>)DialogModuleType.Wrap(dialogModule);
        }

        public Core.ITypes.IContextResourceImpl Context { get; }
        public ITypes.IDialogModuleResourceImpl DialogModule => DialogModuleType;
    }
}