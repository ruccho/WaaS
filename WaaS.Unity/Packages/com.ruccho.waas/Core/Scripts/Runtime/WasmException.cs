using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class WasmException : Exception
    {
        public WasmException(InstanceFunction function, Instruction instruction, string message = null,
            Exception innerException = null) : base(message, innerException)
        {
            Function = function;
            Instruction = instruction;
            Message =
                $"{instruction.GetType().Name} at 0x{instruction.Index:X4} in function (;{function.function.FunctionIndex};) in module: \"{function.instance.Module.SourceDescription}\", InnerException: {innerException}";
        }

        public InstanceFunction Function { get; }
        public Instruction Instruction { get; }
        public override string Message { get; }
    }
}