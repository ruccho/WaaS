using WaaS.Runtime;

namespace Wukuro
{
    /// <summary>
    ///     An exception thrown when a script panics. The panic hook installed by <c>#[entry]</c> reports the panic
    ///     through <c>wukuro:core/functions.panic</c>, and the execution of the script is aborted with this exception.
    /// </summary>
    public class WukuroPanicException : TrapException
    {
        public WukuroPanicException(string panicMessage) : base($"The script panicked: {panicMessage}")
        {
            PanicMessage = panicMessage;
        }

        /// <summary>
        ///     The message reported by the script, including the location of the panic.
        /// </summary>
        public string PanicMessage { get; }
    }
}
