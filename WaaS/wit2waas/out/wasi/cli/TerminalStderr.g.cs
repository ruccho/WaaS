// <auto-generated />
#nullable enable

namespace Wasi.Cli
{
    // interface terminal-stderr
    /// <summary>
    ///     An interface providing an optional `terminal-output` for stderr as a
    ///     link-time authority.
    /// </summary>
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"terminal-stderr")]
    public partial interface ITerminalStderr
    {
        /// <summary>
        ///     If stderr is connected to a terminal, return a `terminal-output` handle
        ///     allowing further interaction with it.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"get-terminal-stderr")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wasi.Cli.ITerminalOutput.ITerminalOutputResourceImpl>?> GetTerminalStderr();

    }
}
