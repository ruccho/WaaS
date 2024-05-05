using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    public class ConstantExpression
    {
        private bool _inEvaluation;

        internal ConstantExpression(ref ModuleReader reader)
        {
            Instructions = InstructionReader.ReadTillEnd(ref reader);
            if (Instructions.Length < 2) throw new InvalidModuleException();
        }

        public ReadOnlyMemory<Instruction> Instructions { get; }

        public StackValueItem Evaluate(GlobalInstance globalInstance)
        {
            var globals = globalInstance.Globals.Span;
            var instrs = Instructions.Span;

            StackValueItem? result = null;
            foreach (var instr in instrs)
            {
                if (instr is End) break;
                result = instr switch
                {
                    ConstI32 c => new StackValueItem(c.Value),
                    ConstI64 c => new StackValueItem(c.Value),
                    ConstF32 c => new StackValueItem(c.Value),
                    ConstF64 c => new StackValueItem(c.Value),
                    GlobalGet g => globals[(int)g.GlobalIndex].GetStackValue(),
                    _ => throw new InvalidModuleException()
                };
            }

            if (result == null) throw new InvalidModuleException();

            return result.Value;
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> initializingGlobals)
        {
            if (_inEvaluation) throw new InvalidOperationException();

            _inEvaluation = true;

            try
            {
                var instrs = Instructions.Span;

                StackValueItem? result = null;
                foreach (var instr in instrs)
                {
                    if (instr is End) break;
                    if (result.HasValue) throw new InvalidModuleException("type mismatch"); // duplicate assignment
                    result = instr switch
                    {
                        ConstI32 c => new StackValueItem(c.Value),
                        ConstI64 c => new StackValueItem(c.Value),
                        ConstF32 c => new StackValueItem(c.Value),
                        ConstF64 c => new StackValueItem(c.Value),
                        GlobalGet g => initializingGlobals[(int)g.GlobalIndex].GetStackValue(),
                        _ => throw new InvalidModuleException(
                            $"Instruction {instr.GetType().Name} cannot be used for constant expression!")
                    };
                }

                if (result == null) throw new InvalidModuleException();

                return result.Value;
            }
            finally
            {
                _inEvaluation = false;
            }
        }
    }
}