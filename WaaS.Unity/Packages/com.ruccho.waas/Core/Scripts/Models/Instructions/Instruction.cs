using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents a single instruction in a WebAssembly function body.
    /// </summary>
    public abstract class Instruction
    {
        internal Instruction(uint index)
        {
            Index = index;
        }

        /// <summary>
        ///     Index of the instruction in the function body.
        /// </summary>
        public uint Index { get; }

        /// <summary>
        ///     Executes the instruction.
        /// </summary>
        /// <param name="current"></param>
        public abstract void Execute(WasmStackFrame current);

        /// <summary>
        ///     Get the number of values to pop and push from the stack to validate stack depth.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context);

        /// <summary>
        ///     Simulates stack operations to validate the stack state.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stackState"></param>
        public abstract void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState);

        /// <summary>
        ///     Validates the instruction.
        /// </summary>
        /// <param name="context"></param>
        public virtual void Validate(in ValidationContext context)
        {
        }
    }
}