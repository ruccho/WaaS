#nullable enable

namespace Wukuro.Dialog
{
    // interface types
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"wukuro:dialog/types")]
    public partial interface ITypes
    {
        [global::WaaS.ComponentModel.Binding.ComponentResource("dialog-module")]
        public partial interface IDialogModuleResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.open-dialog")]
            global::System.Threading.Tasks.ValueTask OpenDialog(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self, global::WaaS.ComponentModel.Binding.Option<string> @provider);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.close-dialog")]
            global::System.Threading.Tasks.ValueTask CloseDialog(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.show-message")]
            global::System.Threading.Tasks.ValueTask ShowMessage(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self, Wukuro.Dialog.ITypes.MessageInfo @message);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.show-options")]
            global::System.Threading.Tasks.ValueTask<int> ShowOptions(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self, global::System.ReadOnlyMemory<Wukuro.Dialog.ITypes.Text> @options);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.fade-in")]
            global::System.Threading.Tasks.ValueTask FadeIn(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self, float @duration);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]dialog-module.fade-out")]
            global::System.Threading.Tasks.ValueTask FadeOut(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Dialog.ITypes.IDialogModuleResourceImpl> @self, float @duration);

        }

        [global::WaaS.ComponentModel.Binding.ComponentRecord]
        public readonly partial struct MessageInfo
        {
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public string Speaker { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public string Face { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public Wukuro.Dialog.ITypes.Text Text { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public bool Auto { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public bool ForbidSkip { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public bool ForbidAccel { get; init; }
            /// <summary>
            ///     Names of host-specific display styles of the message. The host decides their meanings.
            /// </summary>
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public global::System.ReadOnlyMemory<string> Styles { get; init; }
        }

        /// <summary>
        ///     Text shown to the player: either the text written directly in the script, or a reference to a localized text.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentVariant, global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        public readonly partial struct Text
        {
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public string? Plain { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public Wukuro.Dialog.ITypes.TextId? Localized { get; init; }
        }

        /// <summary>
        ///     A reference to a localized text. The host decides how to resolve it.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentRecord]
        public readonly partial struct TextId
        {
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public string Table { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentFieldAttribute]
            public string Key { get; init; }
        }

    }
}
