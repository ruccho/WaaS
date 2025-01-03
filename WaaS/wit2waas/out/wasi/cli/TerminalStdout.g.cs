// <auto-generated />
#nullable enable

namespace Wasi.Cli
{
    // interface terminal-stdout
    /// <summary>
    ///     An interface providing an optional `terminal-output` for stdout as a
    ///     link-time authority.
    /// </summary>
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"terminal-stdout")]
    public partial interface ITerminalStdout
    {
        /// <summary>
        ///     If stdout is connected to a terminal, return a `terminal-output` handle
        ///     allowing further interaction with it.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"get-terminal-stdout")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wasi.Cli.ITerminalOutput.ITerminalOutputResourceImpl>?> GetTerminalStdout();

    }
}
