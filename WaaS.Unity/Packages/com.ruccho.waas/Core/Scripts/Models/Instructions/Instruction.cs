using WaaS.Runtime;

namespace WaaS.Models
{
    public abstract class Instruction
    {
        internal Instruction(uint index)
        {
            Index = index;
        }

        public uint Index { get; }

        public abstract void Execute(WasmStackFrame current);

        public abstract (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context);

        public abstract void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState);

        public virtual void Validate(in ValidationContext context)
        {
        }
    }
}