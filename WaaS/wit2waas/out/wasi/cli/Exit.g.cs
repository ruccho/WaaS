// <auto-generated />
#nullable enable

namespace Wasi.Cli
{
    // interface exit
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"exit")]
    public partial interface IExit
    {
        /// <summary>
        ///     Exit the current instance and any linked instances.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"exit")]
        global::System.Threading.Tasks.ValueTask Exit(global::WaaS.ComponentModel.Binding.Result<global::WaaS.ComponentModel.Binding.None, global::WaaS.ComponentModel.Binding.None> @status);

    }
}
