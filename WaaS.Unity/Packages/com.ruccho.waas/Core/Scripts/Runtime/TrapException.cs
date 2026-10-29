using System;

namespace WaaS.Runtime
{
    public class TrapException : Exception
    {
        public TrapException()
        {
        }

        public TrapException(string message) : base(message)
        {
        }

        public TrapException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        ///     Stack frames of the WebAssembly execution at the time of the trap, or null if not captured.
        /// </summary>
        public WasmStackTrace WasmStackTrace { get; internal set; }

        public override string Message =>
            WasmStackTrace != null ? $"{base.Message}{Environment.NewLine}{WasmStackTrace}" : base.Message;
    }
}
