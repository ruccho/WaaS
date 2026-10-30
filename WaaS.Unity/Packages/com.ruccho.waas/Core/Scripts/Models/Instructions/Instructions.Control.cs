using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents "blocktype".
    /// </summary>
    public readonly struct BlockType : IEquatable<BlockType>
    {
        /// <summary>
        ///     The result type of the block when it is specified as a single value type.
        /// </summary>
        public ValueType? Type { get; }

        /// <summary>
        ///     The index of the function type of the block when it is specified as a type index (multi-value).
        /// </summary>
        public uint? TypeIndex { get; }

        internal BlockType(ref ModuleReader reader)
        {
            Type = null;
            TypeIndex = null;

            var t = reader.ReadUnaligned<byte>();
            if (t == 0x40) return;

            if (t is >= (byte)ValueType.V128 and <= (byte)ValueType.I32 or (byte)ValueType.FuncRef or
                (byte)ValueType.ExternRef)
            {
                Type = (ValueType)t;
                return;
            }

            // s33 type index (must be non-negative)
            const int maxNumSrcBytes = 5; // ceil(33 / 7)
            ulong result = 0;
            var source = t;
            for (var i = 0;; i++)
            {
                result |= (ulong)(source & 0x7F) << (7 * i);

                if (source >> 7 == 0)
                {
                    if (i == maxNumSrcBytes - 1)
                    {
                        // the last byte holds bit 28-32 and the sign extension
                        if ((source & 0b01110000) != 0) throw new InvalidModuleException("invalid block type");
                    }
                    else
                    {
                        if ((source & 0b01000000) != 0) throw new InvalidModuleException("invalid block type");
                    }

                    break;
                }

                if (i == maxNumSrcBytes - 1) throw new InvalidModuleException("invalid block type");
                source = reader.ReadUnaligned<byte>();
            }

            TypeIndex = (uint)result;
        }

        public bool Equals(BlockType other)
        {
            return Type == other.Type && TypeIndex == other.TypeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, TypeIndex);
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

    /// <summary>
    ///     Represents "unreachable" instruction.
    /// </summary>
    [OpCode(0x00)]
    public partial class Unreachable : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            throw new TrapException("unreachable");
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
    ///     Represents "nop" instruction.
    /// </summary>
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

    /// <summary>
    ///     Base of block instructions.
    /// </summary>
    public abstract partial class BlockInstruction : Instruction
    {
        // indexed by (type - V128)
        // indexed by the value type minus ExternRef (0x6F)
        private static readonly ValueType[][] SingleValueTypes = CreateSingleValueTypes();

        [Operand(0)] public BlockType BlockType { get; }

        private static ValueType[][] CreateSingleValueTypes()
        {
            var types = new ValueType[(byte)ValueType.I32 - (byte)ValueType.ExternRef + 1][];
            for (var i = 0; i < types.Length; i++) types[i] = new[] { (ValueType)(i + (byte)ValueType.ExternRef) };
            return types;
        }

        public End End { get; private set; }

        /// <summary>
        ///     Types of the values the block takes from the enclosing stack. Available after validation.
        /// </summary>
        public ReadOnlyMemory<ValueType> ParameterTypes { get; private set; }

        /// <summary>
        ///     Types of the values the block leaves on the enclosing stack. Available after validation.
        /// </summary>
        public ReadOnlyMemory<ValueType> ResultTypes { get; private set; }

        /// <summary>
        ///     Number of the values a branch to this block carries.
        /// </summary>
        public abstract uint Arity { get; }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);

            if (BlockType.TypeIndex is { } typeIndex)
            {
                var funcTypes = context.Module.TypeSection?.FuncTypes ?? default;
                if (typeIndex >= funcTypes.Length) throw new InvalidCodeException("unknown type");
                var functionType = funcTypes.Span[(int)typeIndex];
                ParameterTypes = functionType.ParameterTypes;
                ResultTypes = functionType.ResultTypes;
            }
            else
            {
                ParameterTypes = ReadOnlyMemory<ValueType>.Empty;
                ResultTypes = BlockType.Type is { } type
                    ? SingleValueTypes[(byte)type - (byte)ValueType.ExternRef]
                    : ReadOnlyMemory<ValueType>.Empty;
            }
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return ((uint)ParameterTypes.Length, (uint)ResultTypes.Length);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var paramTypes = ParameterTypes.Span;
            for (var i = paramTypes.Length - 1; i >= 0; i--) stackState.Pop(paramTypes[i]);
        }

        public sealed override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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

    /// <summary>
    ///     Represents "block" instruction.
    /// </summary>
    [OpCode(0x02)]
    public partial class Block : BlockInstruction
    {
        public override uint Arity => (uint)ResultTypes.Length;
    }

    /// <summary>
    ///     Represents "loop" instruction.
    /// </summary>
    [OpCode(0x03)]
    public partial class Loop : BlockInstruction
    {
        public override uint Arity => (uint)ParameterTypes.Length;
    }

    /// <summary>
    ///     Represents "if" instruction.
    /// </summary>
    [OpCode(0x04)]
    public partial class If : BlockInstruction
    {
        public override uint Arity => (uint)ResultTypes.Length;

        public Else Else { get; private set; } // optional

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            var (popCount, pushCount) = base.PreValidateStackState(context);
            return (popCount + 1, pushCount);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
            base.ValidateStackState(context, ref stackState);
        }

        public override void InjectDelimiter(BlockDelimiterInstruction delimiter)
        {
            base.InjectDelimiter(delimiter);
            if (delimiter is Else @else) Else = @else;
        }
    }

    /// <summary>
    ///     Base of block delimiter instructions.
    /// </summary>
    public abstract class BlockDelimiterInstruction : Instruction
    {
        protected BlockDelimiterInstruction(uint index) : base(index)
        {
        }
    }

    /// <summary>
    ///     Represents "else" instruction.
    /// </summary>
    [OpCode(0x05)]
    public partial class Else : BlockDelimiterInstruction
    {
        public override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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
    ///     Represents "end" instruction.
    /// </summary>
    [OpCode(0x0B)]
    public partial class End : BlockDelimiterInstruction
    {
        public override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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
    ///     Represents "br" instruction.
    /// </summary>
    [OpCode(0x0C)]
    public partial class Br : Instruction
    {
        [Operand(0)] public uint LabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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
    ///     Represents "br_if" instruction.
    /// </summary>
    [OpCode(0x0D)]
    public partial class BrIf : Instruction
    {
        [Operand(0)] public uint LabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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

    /// <summary>
    ///     Represents "br_table" instruction.
    /// </summary>
    [OpCode(0x0E)]
    public partial class BrTable : Instruction
    {
        [Operand(0)] public ReadOnlyMemory<uint> LabelIndices { get; }
        [Operand(1)] public uint DefaultLabelIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            ThrowExecutedByInterpreter();
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

    /// <summary>
    ///     Represents "return" instruction.
    /// </summary>
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

    /// <summary>
    ///     Represents "call" instruction.
    /// </summary>
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

    /// <summary>
    ///     Represents "call_indirect" instruction.
    /// </summary>
    [OpCode(0x11)]
    public partial class CallIndirect : Instruction
    {
        [Operand(0)] public uint FunctionTypeIndex { get; }

        // NOTE: Rust 1.82 and later don't emit 1-byte fixed 0x00 defined in wasm 1.0 but LEB128 32-bit number.  
        // https://blog.rust-lang.org/2024/09/24/webassembly-targets-change-in-default-target-features.html
        [Operand(1)] public uint TableIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var functionIndex = current.Pop().ExpectValueI32();
            if (current.Instance.TableInstance.Tables.Span[checked((int)TableIndex)] is not Table<IInvocableFunction>
                table)
                throw new InvalidOperationException();

            var functionType = current.Instance.Module.TypeSection.FuncTypes.Span[checked((int)FunctionTypeIndex)];

            if (functionIndex >= table.Size) throw new TrapException("undefined element");
            var function = table.Elements[(int)functionIndex];
            if (function == null)
                throw new TrapException($"[{nameof(CallIndirect)}] Function is null. index: {functionIndex}");

            if (!function.Type.Match(functionType))
                throw new TrapException($"[{nameof(CallIndirect)}] Function type mismatch.");

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
            if (FunctionTypeIndex >= (context.Module.TypeSection?.FuncTypes.Length ?? 0))
                throw new InvalidModuleException($"unknown type {FunctionTypeIndex}");
            if (context.GetTableType(TableIndex).ElementType != ElementType.FuncRef)
                throw new InvalidModuleException("type mismatch");
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
            context.GetTableType(TableIndex);
            stackState.Pop(ValueType.I32);
            var type = context.Module.TypeSection.FuncTypes.Span[(int)FunctionTypeIndex];
            var paramTypes = type.ParameterTypes.Span;

            for (var i = paramTypes.Length - 1; i >= 0; i--) stackState.Pop(paramTypes[i]);

            foreach (var valueType in type.ResultTypes.Span) stackState.Push(valueType);
        }
    }
}