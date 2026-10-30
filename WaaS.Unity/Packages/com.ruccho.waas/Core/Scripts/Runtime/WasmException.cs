using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     An exception thrown when a WebAssembly instruction fails.
    /// </summary>
    public class WasmException : Exception
    {
        private readonly string message;

        public WasmException(InstanceFunction function, Instruction instruction, string message = null,
            Exception innerException = null) : base(message, innerException)
        {
            Function = function;
            Instruction = instruction;

            var module = function.instance.Module;
            var functionIndex = module.ImportedFunctionCount + function.function.FunctionIndex;
            var functionName = TryGetFunctionName(module, (uint)functionIndex);
            var functionLabel = functionName != null ? $"{functionName} (;{functionIndex};)" : $"(;{functionIndex};)";
            var reason = message ?? (innerException != null
                ? $"{innerException.GetType().Name}: {innerException.Message}"
                : null);

            this.message =
                $"{instruction.GetType().Name} at 0x{instruction.Index:X4} (offset 0x{instruction.Offset:x}) in function {functionLabel} in module: \"{module.SourceDescription}\"" +
                (reason != null ? $", {reason}" : "");
        }

        public InstanceFunction Function { get; }
        public Instruction Instruction { get; }

        /// <summary>
        ///     Stack frames of the WebAssembly execution at the time of the failure, or null if not captured.
        /// </summary>
        public WasmStackTrace WasmStackTrace { get; internal set; }

        public override string Message =>
            WasmStackTrace != null ? $"{message}{Environment.NewLine}{WasmStackTrace}" : message;

        private static string TryGetFunctionName(Module module, uint functionIndex)
        {
            try
            {
                return module.Names.TryGetFunctionName(functionIndex, out var name) ? name : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
