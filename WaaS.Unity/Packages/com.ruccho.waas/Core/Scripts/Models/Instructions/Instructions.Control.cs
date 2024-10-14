using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    public readonly struct BlockType : IEquatable<BlockType>
    {
        public ValueType? Type { get; }

        internal BlockType(ref ModuleReader reader)
        {
            var t = reader.ReadUnaligned<byte>();
            if (t == 0x40) Type = null;
            else Type = (ValueType)t;
        }

        public bool Equals(BlockType other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public static bool operator ==(BlockType left, BlockType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockType left, BlockType right)
        {
            return !left.Equals(right);
        }
    }

    [OpCode(0x00)]
    public partial class Unreachable : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            throw new TrapException();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    [OpCode(0x01)]
    public partial class Nop : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    public abstract partial class BlockInstruction : Instruction
    {
        [Operand(0)] public BlockType BlockType { get; }

        public End End { get; private set; }

        public abstract uint Arity { get; }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }

        protected abstract void OnBeforeBlockEnter(WasmStackFrame current, out uint continuationIndex);

        public sealed override void Execute(WasmStackFrame current)
        {
            OnBeforeBlockEnter(current, out var continuationIndex);
            current.EnterBlock(new Label(Index, continuationIndex));
        }

        public virtual void InjectDelimiter(BlockDelimiterInstruction delimiter)
        {
            if (delimiter is End end)
            {
                if (End != null) throw new InvalidCodeException();
                End = end;
            }
        }

        // for validation
        // internal ValueType[] EvaluatedResultTypes { get; set; }
    }

    [OpCode(0x02)]
    public partial class Block : BlockInstruction
    {
        public override uint Arity => BlockType.Type.HasValue ? 1u : 0u;

        protected override void OnBeforeBlockEnter(WasmStackFrame current, out uint continuationIndex)
        {
            continuationIndex = End.Index + 1;
        }
    }

    [OpCode(0x03)]
    public partial class Loop : BlockInstruction
    {
        public override uint Arity => 0;

        protected override void OnBeforeBlockEnter(WasmStackFrame current, out uint continuationIndex)
        {
            continuationIndex = Index;
        }
    }

    [OpCode(0x04)]
    public partial class If : BlockInstruction
    {
        public override uint Arity => BlockType.Type.HasValue ? 1u : 0u;

        public Else Else { get; private set; } // optional

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, Arity);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
        }

        protected override void OnBeforeBlockEnter(WasmStackFrame current, out uint continuationIndex)
        {
            var c = current.Pop().ExpectValueI32();
            continuationIndex = End.Index + 1;

            if (c != 0) return;

            if (Else != null)
                current.Jump(Else.Index + 1);
            else
                current.Jump(End.Index);
        }

        public override void InjectDelimiter(BlockDelimiterInstruction delimiter)
        {
            base.InjectDelimiter(delimiter);
            if (delimiter is Else @else) Else = @else;
        }
    }

    public abstract class BlockDelimiterInstruction : Instruction
    {
        protected BlockDelimiterInstruction(uint index) : base(index)
        {
        }
    }

    [OpCode(0x05)]
    public partial class Else : BlockDelimiterInstruction
    {
        public override void Execute(WasmStackFrame current)
        {
            current.EndBlock();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    [OpCode(0x0B)]
    public partial class End : BlockDelimiterInstruction
    {
        public override void Execute(WasmStackFrame current)
        {
            current.EndBlock();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    [OpCode(0x0C)]
    public partial class Br : Instruction
    {
        [Operand(0)] public uint LabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.JumpLabel(LabelIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    [OpCode(0x0D)]
    public partial class BrIf : Instruction
    {
        [Operand(0)] public uint LabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var c = current.Pop().ExpectValueI32();
            if (c == 0) return;
            current.JumpLabel(LabelIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
        }
    }

    [OpCode(0x0E)]
    public partial class BrTable : Instruction
    {
        [Operand(0)] public ReadOnlyMemory<uint> LabelIndices { get; }
        [Operand(1)] public uint DefaultLabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var i = current.Pop().ExpectValueI32();

            if (i < LabelIndices.Length)
                current.JumpLabel(LabelIndices.Span[checked((int)i)]);
            else
                current.JumpLabel(DefaultLabelIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
        }
    }

    [OpCode(0x0F)]
    public partial class Return : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            current.End();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    [OpCode(0x10)]
    public partial class Call : Instruction
    {
        [Operand(0)] public uint FuncIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var function = current.Instance.FunctionInstance.Functions.Span[checked((int)FuncIndex)];

            var paramTypes = function.Type.ParameterTypes.Span;

            Span<StackValueItem> parameters = stackalloc StackValueItem[paramTypes.Length];

            for (var i = paramTypes.Length - 1; i >= 0; i--)
            {
                var type = paramTypes[i];
                var stackItem = current.Pop();
                if (!stackItem.IsType(type)) throw new InvalidCodeException();
                parameters[i] = stackItem;
            }

            current.PushFrame(function, parameters);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(in context);
            context.GetFunctionType(FuncIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            var type = context.GetFunctionType(FuncIndex);
            return ((uint)type.ParameterTypes.Length, (uint)type.ResultTypes.Length);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = context.GetFunctionType(FuncIndex);
            var paramTypes = type.ParameterTypes.Span;

            for (var i = paramTypes.Length - 1; i >= 0; i--) stackState.Pop(paramTypes[i]);

            foreach (var valueType in type.ResultTypes.Span) stackState.Push(valueType);
        }
    }

    [OpCode(0x11)]
    public partial class CallIndirect : Instruction
    {
        [Operand(0)] public uint FunctionTypeIndex { get; }
        [Operand(1)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var functionIndex = current.Pop().ExpectValueI32();
            if (current.Instance.TableInstance.Tables.Span[0] is not Table<IInvocableFunction> table)
                throw new InvalidOperationException();

            var functionType = current.Instance.Module.TypeSection.FuncTypes.Span[checked((int)FunctionTypeIndex)];

            var function = table.Elements[checked((int)functionIndex)];
            if (function == null) throw new TrapException();

            if (!function.Type.Match(functionType)) throw new TrapException();

            var paramTypes = function.Type.ParameterTypes.Span;

            Span<StackValueItem> parameters = stackalloc StackValueItem[paramTypes.Length];

            for (var i = paramTypes.Length - 1; i >= 0; i--)
            {
                var type = paramTypes[i];
                var stackItem = current.Pop();
                if (!stackItem.IsType(type)) throw new InvalidCodeException();
                parameters[i] = stackItem;
            }

            current.PushFrame(function, parameters);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (TableIndex != 0) throw new InvalidModuleException($"Reserved byte must be 0 but {TableIndex}");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            checked
            {
                var type = context.Module.TypeSection.FuncTypes.Span[(int)FunctionTypeIndex];
                return ((uint)type.ParameterTypes.Length + 1, (uint)type.ResultTypes.Length);
            }
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetTableType(0);
            stackState.Pop(ValueType.I32);
            var type = context.Module.TypeSection.FuncTypes.Span[(int)FunctionTypeIndex];
            var paramTypes = type.ParameterTypes.Span;

            for (var i = paramTypes.Length - 1; i >= 0; i--) stackState.Pop(paramTypes[i]);

            foreach (var valueType in type.ResultTypes.Span) stackState.Push(valueType);
        }
    }
}