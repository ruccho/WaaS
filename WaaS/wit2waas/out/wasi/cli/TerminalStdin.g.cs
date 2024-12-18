// <auto-generated />
#nullable enable

namespace Wasi.Cli
{
    // interface terminal-stdin
    /// <summary>
    ///     An interface providing an optional `terminal-input` for stdin as a
    ///     link-time authority.
    /// </summary>
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"terminal-stdin")]
    public partial interface ITerminalStdin
    {
        /// <summary>
        ///     If stdin is connected to a terminal, return a `terminal-input` handle
        ///     allowing further interaction with it.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"get-terminal-stdin")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wasi.Cli.ITerminalInput.ITerminalInputResourceImpl>?> GetTerminalStdin();

    }
}
