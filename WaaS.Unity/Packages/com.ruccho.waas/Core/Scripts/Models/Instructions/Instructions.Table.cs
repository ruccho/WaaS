using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents "table.get" instruction.
    /// </summary>
    [OpCode(0x25)]
    public partial class TableGet : Instruction
    {
        [Operand(0)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var table = current.Instance.TableInstance.TableArray[TableIndex];
            var index = current.Pop().ExpectValueI32();
            current.Push(StackValueItem.FromReference(table.ElementType, table.GetObject(index)));
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = (ValueType)context.GetTableType(TableIndex).ElementType;
            stackState.Pop(ValueType.I32);
            stackState.Push(type);
        }
    }

    /// <summary>
    ///     Represents "table.set" instruction.
    /// </summary>
    [OpCode(0x26)]
    public partial class TableSet : Instruction
    {
        [Operand(0)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var table = current.Instance.TableInstance.TableArray[TableIndex];
            var value = current.Pop().ExpectReference();
            var index = current.Pop().ExpectValueI32();
            table.SetObject(index, value);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = (ValueType)context.GetTableType(TableIndex).ElementType;
            stackState.Pop(type);
            stackState.Pop(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "table.init" instruction.
    /// </summary>
    [OpCode(0xFC, 0x0C)]
    public partial class TableInit : Instruction
    {
        [Operand(0)] public uint ElementIndex { get; }
        [Operand(1)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var n = current.Pop().ExpectValueI32();
            var src = current.Pop().ExpectValueI32();
            var dest = current.Pop().ExpectValueI32();
            current.Instance.TableInstance.Init(TableIndex, ElementIndex, dest, src, n);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            var tableType = context.GetTableType(TableIndex);
            if ((ValueType)tableType.ElementType != context.GetElement(ElementIndex).Type)
                throw new InvalidModuleException("type mismatch");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "elem.drop" instruction.
    /// </summary>
    [OpCode(0xFC, 0x0D)]
    public partial class ElemDrop : Instruction
    {
        [Operand(0)] public uint ElementIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.Instance.TableInstance.DropElementSegment(ElementIndex);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            context.GetElement(ElementIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
        }
    }

    /// <summary>
    ///     Represents "table.copy" instruction.
    /// </summary>
    [OpCode(0xFC, 0x0E)]
    public partial class TableCopy : Instruction
    {
        [Operand(0)] public uint DestTableIndex { get; }
        [Operand(1)] public uint SrcTableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var tables = current.Instance.TableInstance.TableArray;
            var n = current.Pop().ExpectValueI32();
            var src = current.Pop().ExpectValueI32();
            var dest = current.Pop().ExpectValueI32();
            tables[SrcTableIndex].CopyTo(tables[DestTableIndex], dest, src, n);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (context.GetTableType(DestTableIndex).ElementType != context.GetTableType(SrcTableIndex).ElementType)
                throw new InvalidModuleException("type mismatch");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "table.grow" instruction.
    /// </summary>
    [OpCode(0xFC, 0x0F)]
    public partial class TableGrow : Instruction
    {
        [Operand(0)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var table = current.Instance.TableInstance.TableArray[TableIndex];
            var n = current.Pop().ExpectValueI32();
            var value = current.Pop().ExpectReference();
            current.Push(unchecked((uint)table.Grow(n, value)));
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = (ValueType)context.GetTableType(TableIndex).ElementType;
            stackState.Pop(ValueType.I32);
            stackState.Pop(type);
            stackState.Push(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "table.size" instruction.
    /// </summary>
    [OpCode(0xFC, 0x10)]
    public partial class TableSize : Instruction
    {
        [Operand(0)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.Push(current.Instance.TableInstance.TableArray[TableIndex].Size);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetTableType(TableIndex);
            stackState.Push(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "table.fill" instruction.
    /// </summary>
    [OpCode(0xFC, 0x11)]
    public partial class TableFill : Instruction
    {
        [Operand(0)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var table = current.Instance.TableInstance.TableArray[TableIndex];
            var n = current.Pop().ExpectValueI32();
            var value = current.Pop().ExpectReference();
            var offset = current.Pop().ExpectValueI32();
            table.Fill(offset, n, value);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = (ValueType)context.GetTableType(TableIndex).ElementType;
            stackState.Pop(ValueType.I32);
            stackState.Pop(type);
            stackState.Pop(ValueType.I32);
        }
    }
}
