using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents "ref.null" instruction.
    /// </summary>
    [OpCode(0xD0)]
    public partial class RefNull : Instruction
    {
        [Operand(0)] public ValueType Type { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.Push(StackValueItem.FromReference(Type, null));
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (!Type.IsReference()) throw new InvalidModuleException("malformed reference type");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Push(Type);
        }
    }

    /// <summary>
    ///     Represents "ref.is_null" instruction.
    /// </summary>
    [OpCode(0xD1)]
    public partial class RefIsNull : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop();
            if (!value.valueType.IsReference()) throw new InvalidCodeException();
            current.Push(value.valueI32 == 0 ? 1u : 0u);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.PopReference();
            stackState.Push(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "ref.func" instruction.
    /// </summary>
    [OpCode(0xD2)]
    public partial class RefFunc : Instruction
    {
        [Operand(0)] public uint FunctionIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var function = current.Instance.FunctionInstance.Functions.Span[checked((int)FunctionIndex)];
            current.Push(StackValueItem.FromReference(ValueType.FuncRef, function));
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            context.ValidateFunctionReference(FunctionIndex, true);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Push(ValueType.FuncRef);
        }
    }
}
