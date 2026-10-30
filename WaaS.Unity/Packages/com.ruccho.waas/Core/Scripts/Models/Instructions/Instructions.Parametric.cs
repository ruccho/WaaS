using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents "drop" instruction.
    /// </summary>
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

    /// <summary>
    ///     Represents "select" instruction.
    /// </summary>
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

            // the values of reference types require the typed select
            if (t1.IsReference() || t2.IsReference()) throw new InvalidCodeException("type mismatch");

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

    /// <summary>
    ///     Represents "select t*" instruction, which can select the values of reference types.
    /// </summary>
    [OpCode(0x1C)]
    public partial class SelectTyped : Instruction
    {
        [Operand(0)] public ReadOnlyMemory<ValueType> Types { get; }

        public override void Execute(WasmStackFrame current)
        {
            var c = current.Pop().ExpectValueI32();
            var v2 = current.Pop().ExpectValue();
            var v1 = current.Pop().ExpectValue();

            current.Push(c != 0 ? v1 : v2);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (Types.Length != 1) throw new InvalidModuleException("invalid result arity");
            if (!Types.Span[0].IsDefined()) throw new InvalidModuleException("malformed value type");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = Types.Span[0];
            stackState.Pop(ValueType.I32);
            stackState.Pop(type);
            stackState.Pop(type);
            stackState.Push(type);
        }
    }
}