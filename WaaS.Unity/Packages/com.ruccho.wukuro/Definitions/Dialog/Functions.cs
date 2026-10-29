#nullable enable

namespace Wukuro.Dialog
{
    // interface functions
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"wukuro:dialog/functions")]
    public partial interface IFunctions
    {
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"get-dialog-module")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl>> GetDialogModule(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IContextResourceImpl> @context);

    }
}
