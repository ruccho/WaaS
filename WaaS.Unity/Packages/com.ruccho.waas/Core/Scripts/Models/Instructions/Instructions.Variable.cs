using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents "local.get" instruction.
    /// </summary>
    [OpCode(0x20)]
    public partial class LocalGet : Instruction
    {
        [Operand(0)] public uint LocalIndex { get; }

        public override void Execute(in TransientWasmStackFrame current, ref StackFrame? pushedFrame)
        {
            current.Push(current.GetLocal((int)LocalIndex));
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(in context);
            context.GetLocalType(LocalIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Push(context.GetLocalType(LocalIndex));
        }
    }

    /// <summary>
    ///     Represents "local.set" instruction.
    /// </summary>
    [OpCode(0x21)]
    public partial class LocalSet : Instruction
    {
        [Operand(0)] public uint LocalIndex { get; }

        public override void Execute(in TransientWasmStackFrame current, ref StackFrame? pushedFrame)
        {
            ref var local = ref current.GetLocal((int)LocalIndex);
            var val = current.Pop();

            local.ExpectValue(out var type);

            local = val.ExpectValue(type);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(context.GetLocalType(LocalIndex));
        }
    }

    /// <summary>
    ///     Represents "local.tee" instruction.
    /// </summary>
    [OpCode(0x22)]
    public partial class LocalTee : Instruction
    {
        [Operand(0)] public uint LocalIndex { get; }

        public override void Execute(in TransientWasmStackFrame current, ref StackFrame? pushedFrame)
        {
            var val = current.Pop().ExpectValue();
            current.Push(val);

            ref var local = ref current.GetLocal((int)LocalIndex);

            local.ExpectValue(out var type);

            local = val.ExpectValue(type);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = context.GetLocalType(LocalIndex);
            stackState.Pop(type);
            stackState.Push(type);
        }
    }

    /// <summary>
    ///     Represents "global.get" instruction.
    /// </summary>
    [OpCode(0x23)]
    public partial class GlobalGet : Instruction
    {
        [Operand(0)] public uint GlobalIndex { get; }

        public override void Execute(in TransientWasmStackFrame current, ref StackFrame? pushedFrame)
        {
            current.Push(current.Instance.GlobalInstance.Globals.Span[(int)GlobalIndex].GetStackValue());
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(in context);
            context.GetGlobalType(GlobalIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Push(context.GetGlobalType(GlobalIndex).ValueType);
        }
    }

    /// <summary>
    ///     Represents "global.set" instruction.
    /// </summary>
    [OpCode(0x24)]
    public partial class GlobalSet : Instruction
    {
        [Operand(0)] public uint GlobalIndex { get; }

        public override void Execute(in TransientWasmStackFrame current, ref StackFrame? pushedFrame)
        {
            if (current.Instance.GlobalInstance.Globals.Span[checked((int)GlobalIndex)] is not GlobalMutable
                globalMutable)
                throw new InvalidCodeException();

            globalMutable.SetStackValue(current.Pop());
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(in context);
            context.GetGlobalType(GlobalIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var globalType = context.GetGlobalType(GlobalIndex);
            if (globalType.Mutability == Mutability.Const) throw new InvalidModuleException();
            stackState.Pop(globalType.ValueType);
        }
    }
}