// <auto-generated />
#nullable enable

namespace Wasi.Cli
{
    // interface run
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"run")]
    public partial interface IRun
    {
        /// <summary>
        ///     Run the program.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"run")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Result<global::WaaS.ComponentModel.Binding.None, global::WaaS.ComponentModel.Binding.None>> Run();

    }
}
