using WaaS.Runtime;

namespace WaaS.Models
{
    [OpCode(0x1A)]
    public partial class Drop : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            current.Pop().ExpectValue();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.PopAny();
        }
    }

    [OpCode(0x1B)]
    public partial class Select : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var c = current.Pop().ExpectValueI32();
            var v2 = current.Pop().ExpectValue();
            var v1 = current.Pop().ExpectValue();

            if (c != 0)
                current.Push(v1);
            else
                current.Push(v2);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
            var t2 = stackState.PopAny();
            var t1 = stackState.PopAny();

            if ((byte)t1 == 0)
            {
                stackState.Push(t2);
                return;
            }

            if ((byte)t2 == 0 || t1 == t2)
            {
                stackState.Push(t1);
                return;
            }

            throw new InvalidCodeException();
        }
    }
}