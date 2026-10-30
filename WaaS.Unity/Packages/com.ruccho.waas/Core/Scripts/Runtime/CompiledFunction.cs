using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Operations executed by the interpreter loop of <see cref="WasmStackFrame" />.
    ///     Instructions without a dedicated operation are executed with <see cref="Instruction.Execute" />.
    /// </summary>
    internal enum Op
    {
        Fallback,
        Nop,
        Return,

        // control
        If, // A: the instruction index to jump to when the condition is zero
        Jump, // A: the instruction index to jump to
        Br, // A: target, B: stack height, C: arity
        BrLoop, // same as Br, but the target is a loop
        BrIf,
        BrIfLoop,
        ReturnIf,
        BrTable, // A: the start index in BranchTargets, B: the number of the targets excluding the default
        Call, // A: function index, B: the number of the parameters

        // parametric
        Drop,
        Select,

        // variable
        LocalGet, // A: local index
        LocalSet,
        LocalTee,
        GlobalGet, // A: global index
        GlobalSet,

        // numeric
        Const, // A: value type, Value: bits

        I32Eqz,
        I32Eq,
        I32Ne,
        I32LtS,
        I32LtU,
        I32GtS,
        I32GtU,
        I32LeS,
        I32LeU,
        I32GeS,
        I32GeU,

        I64Eqz,
        I64Eq,
        I64Ne,
        I64LtS,
        I64LtU,
        I64GtS,
        I64GtU,
        I64LeS,
        I64LeU,
        I64GeS,
        I64GeU,

        I32Add,
        I32Sub,
        I32Mul,
        I32And,
        I32Or,
        I32Xor,
        I32Shl,
        I32ShrS,
        I32ShrU,
        I32Rotl,
        I32Rotr,

        I64Add,
        I64Sub,
        I64Mul,
        I64And,
        I64Or,
        I64Xor,
        I64Shl,
        I64ShrS,
        I64ShrU,
        I64Rotl,
        I64Rotr,

        I32WrapI64,
        I64ExtendI32S,
        I64ExtendI32U,

        // memory. A: offset, B: value type (loads of 32 and 64 bits)
        Load32,
        Load64,
        I32Load8S,
        I32Load8U,
        I32Load16S,
        I32Load16U,
        I64Load8S,
        I64Load8U,
        I64Load16S,
        I64Load16U,
        I64Load32S,
        I64Load32U,
        Store8,
        Store16,
        Store32,
        Store64
    }

    /// <summary>
    ///     An instruction translated for the interpreter loop. Each one corresponds to the instruction at the same index.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct CompiledInstruction
    {
        [FieldOffset(0)] public Op Op;
        [FieldOffset(4)] public int A;
        [FieldOffset(8)] public int B;
        [FieldOffset(12)] public int C;
        [FieldOffset(8)] public ulong Value;
    }

    /// <summary>
    ///     A target of a branch. The values to carry are moved to the stack height of the target block.
    /// </summary>
    internal struct BranchTarget
    {
        // the instruction index to jump to, or -1 to return from the function
        public int Index;
        public int Height;
        public int Arity;

        // whether the target is a loop, where the cancellation is checked
        public bool IsLoop;
    }

    /// <summary>
    ///     A function body translated for the interpreter loop of <see cref="WasmStackFrame" />.
    /// </summary>
    internal sealed class CompiledFunction
    {
        private CompiledFunction(CompiledInstruction[] code, BranchTarget[] branchTargets,
            StackValueItem[] initialLocals, int maxStackDepth)
        {
            Code = code;
            BranchTargets = branchTargets;
            InitialLocals = initialLocals;
            MaxStackDepth = maxStackDepth;
        }

        public CompiledInstruction[] Code { get; }
        public BranchTarget[] BranchTargets { get; }

        /// <summary>
        ///     The initial values of the locals including the parameters.
        /// </summary>
        public StackValueItem[] InitialLocals { get; }

        /// <summary>
        ///     The maximum height of the operand stack.
        /// </summary>
        public int MaxStackDepth { get; }

        /// <summary>
        ///     Translates the validated function.
        /// </summary>
        public static CompiledFunction Compile(in ValidationContext context, Function function)
        {
            checked
            {
                var instructions = function.Body.Instructions.Span;
                var code = new CompiledInstruction[instructions.Length];
                var branchTargets = new List<BranchTarget>();

                var parameterTypes = function.Type.ParameterTypes.Span;
                var numLocals = parameterTypes.Length;
                foreach (var local in function.Body.Locals.Span) numLocals += (int)local.Count;

                var initialLocals = new StackValueItem[numLocals];
                {
                    var cursor = 0;
                    foreach (var type in parameterTypes) initialLocals[cursor++] = new StackValueItem(type);
                    foreach (var local in function.Body.Locals.Span)
                        for (var i = 0; i < local.Count; i++)
                            initialLocals[cursor++] = new StackValueItem(local.Type);
                }

                var blocks = new List<BlockState>
                {
                    // the function itself
                    new() { Height = 0, Arity = function.Type.ResultTypes.Length, Index = -1 }
                };

                var height = 0;
                var maxHeight = 0;
                var isUnreachable = false;

                void PopValues(int count)
                {
                    // the stack is polymorphic in the unreachable code, which is never executed
                    var minHeight = blocks[blocks.Count - 1].Height;
                    height = Math.Max(height - count, isUnreachable ? minHeight : int.MinValue);
                    if (height < minHeight) throw new InvalidCodeException("stack underflow");
                }

                void PushValues(int count)
                {
                    height += count;
                    if (maxHeight < height) maxHeight = height;
                }

                BranchTarget GetBranchTarget(uint depth)
                {
                    var block = blocks[blocks.Count - 1 - (int)depth];
                    return new BranchTarget
                    {
                        Index = block.Index,
                        Height = block.Height,
                        Arity = block.Arity,
                        IsLoop = block.IsLoop
                    };
                }

                void EmitBranch(ref CompiledInstruction compiled, uint depth, Op op, Op loopOp)
                {
                    var target = GetBranchTarget(depth);
                    if (target.Index < 0)
                    {
                        // branch to the function
                        compiled.Op = op == Op.Br ? Op.Return : Op.ReturnIf;
                        return;
                    }

                    compiled.Op = target.IsLoop ? loopOp : op;
                    compiled.A = target.Index;
                    compiled.B = target.Height;
                    compiled.C = target.Arity;
                }

                for (var i = 0; i < instructions.Length; i++)
                {
                    var instr = instructions[i];
                    ref var compiled = ref code[i];

                    switch (instr)
                    {
                        case BlockInstruction block:
                        {
                            var numParameters = block.ParameterTypes.Length;
                            if (block is If @if)
                            {
                                PopValues(1);
                                compiled.Op = Op.If;
                                compiled.A = (int)(@if.Else != null ? @if.Else.Index + 1 : @if.End.Index);
                            }
                            else
                            {
                                compiled.Op = Op.Nop;
                            }

                            var baseHeight = Math.Max(height - numParameters, blocks[blocks.Count - 1].Height);
                            var isLoop = block is Loop;
                            blocks.Add(new BlockState
                            {
                                Height = baseHeight,
                                Arity = (int)block.Arity,
                                // a branch to a loop executes the loop instruction again as before
                                Index = (int)(isLoop ? block.Index : block.End.Index + 1),
                                IsLoop = isLoop,
                                NumParameters = numParameters,
                                NumResults = block.ResultTypes.Length,
                                End = block.End,
                                IsUnreachable = isUnreachable
                            });
                            break;
                        }
                        case Else:
                        {
                            var block = blocks[blocks.Count - 1];
                            compiled.Op = Op.Jump;
                            compiled.A = (int)(block.End.Index + 1);
                            height = block.Height + block.NumParameters;
                            isUnreachable = block.IsUnreachable;
                            break;
                        }
                        case End:
                        {
                            if (blocks.Count == 1)
                            {
                                compiled.Op = Op.Return;
                                isUnreachable = true;
                                break;
                            }

                            var block = blocks[blocks.Count - 1];
                            blocks.RemoveAt(blocks.Count - 1);
                            compiled.Op = Op.Nop;
                            height = block.Height;
                            PushValues(block.NumResults);
                            isUnreachable = block.IsUnreachable;
                            break;
                        }
                        case Br br:
                        {
                            EmitBranch(ref compiled, br.LabelIndex, Op.Br, Op.BrLoop);
                            isUnreachable = true;
                            height = blocks[blocks.Count - 1].Height;
                            break;
                        }
                        case BrIf brIf:
                        {
                            PopValues(1);
                            EmitBranch(ref compiled, brIf.LabelIndex, Op.BrIf, Op.BrIfLoop);
                            break;
                        }
                        case BrTable brTable:
                        {
                            PopValues(1);
                            var labelIndices = brTable.LabelIndices.Span;
                            compiled.Op = Op.BrTable;
                            compiled.A = branchTargets.Count;
                            compiled.B = labelIndices.Length;
                            foreach (var labelIndex in labelIndices) branchTargets.Add(GetBranchTarget(labelIndex));
                            branchTargets.Add(GetBranchTarget(brTable.DefaultLabelIndex));
                            isUnreachable = true;
                            height = blocks[blocks.Count - 1].Height;
                            break;
                        }
                        case Return:
                        {
                            compiled.Op = Op.Return;
                            isUnreachable = true;
                            height = blocks[blocks.Count - 1].Height;
                            break;
                        }
                        default:
                        {
                            var (popCount, pushCount) = instr.PreValidateStackState(context);
                            PopValues((int)popCount);
                            PushValues((int)pushCount);
                            Translate(context, instr, ref compiled);
                            if (instr is Unreachable)
                            {
                                isUnreachable = true;
                                height = blocks[blocks.Count - 1].Height;
                            }

                            break;
                        }
                    }

                    if (maxHeight < height) maxHeight = height;
                }

                // the stack depth from the validation also counts the labels and is not smaller
                var maxStackDepth = Math.Max(maxHeight, (int)(function.MaxStackDepth ?? 0));

                return new CompiledFunction(code, branchTargets.ToArray(), initialLocals, maxStackDepth);
            }
        }

        private static void Translate(in ValidationContext context, Instruction instr, ref CompiledInstruction compiled)
        {
            switch (instr)
            {
                case Nop:
                    compiled.Op = Op.Nop;
                    return;
                case Call call:
                    compiled.Op = Op.Call;
                    compiled.A = checked((int)call.FuncIndex);
                    compiled.B = context.GetFunctionType(call.FuncIndex).ParameterTypes.Length;
                    return;
                case Drop:
                    compiled.Op = Op.Drop;
                    return;
                case Select:
                    compiled.Op = Op.Select;
                    return;
                case LocalGet localGet:
                    compiled.Op = Op.LocalGet;
                    compiled.A = checked((int)localGet.LocalIndex);
                    return;
                case LocalSet localSet:
                    compiled.Op = Op.LocalSet;
                    compiled.A = checked((int)localSet.LocalIndex);
                    return;
                case LocalTee localTee:
                    compiled.Op = Op.LocalTee;
                    compiled.A = checked((int)localTee.LocalIndex);
                    return;
                case GlobalGet globalGet:
                    compiled.Op = Op.GlobalGet;
                    compiled.A = checked((int)globalGet.GlobalIndex);
                    return;
                case GlobalSet globalSet:
                    compiled.Op = Op.GlobalSet;
                    compiled.A = checked((int)globalSet.GlobalIndex);
                    return;
                case ConstI32 constI32:
                    compiled.Op = Op.Const;
                    compiled.A = (int)ValueType.I32;
                    compiled.Value = constI32.Value;
                    return;
                case ConstI64 constI64:
                    compiled.Op = Op.Const;
                    compiled.A = (int)ValueType.I64;
                    compiled.Value = constI64.Value;
                    return;
                case ConstF32 constF32:
                    compiled.Op = Op.Const;
                    compiled.A = (int)ValueType.F32;
                    compiled.Value = new StackValueItem(constF32.Value).valueI64;
                    return;
                case ConstF64 constF64:
                    compiled.Op = Op.Const;
                    compiled.A = (int)ValueType.F64;
                    compiled.Value = new StackValueItem(constF64.Value).valueI64;
                    return;
            }

            compiled.Op = instr switch
            {
                EqzI32 => Op.I32Eqz,
                EqI32 => Op.I32Eq,
                NeI32 => Op.I32Ne,
                LtI32S => Op.I32LtS,
                LtI32U => Op.I32LtU,
                GtI32S => Op.I32GtS,
                GtI32U => Op.I32GtU,
                LeI32S => Op.I32LeS,
                LeI32U => Op.I32LeU,
                GeI32S => Op.I32GeS,
                GeI32U => Op.I32GeU,
                EqzI64 => Op.I64Eqz,
                EqI64 => Op.I64Eq,
                NeI64 => Op.I64Ne,
                LtI64S => Op.I64LtS,
                LtI64U => Op.I64LtU,
                GtI64S => Op.I64GtS,
                GtI64U => Op.I64GtU,
                LeI64S => Op.I64LeS,
                LeI64U => Op.I64LeU,
                GeI64S => Op.I64GeS,
                GeI64U => Op.I64GeU,
                AddI32 => Op.I32Add,
                SubI32 => Op.I32Sub,
                MulI32 => Op.I32Mul,
                AndI32 => Op.I32And,
                OrI32 => Op.I32Or,
                XorI32 => Op.I32Xor,
                ShlI32 => Op.I32Shl,
                ShrI32S => Op.I32ShrS,
                ShrI32U => Op.I32ShrU,
                RotlI32 => Op.I32Rotl,
                RotrI32 => Op.I32Rotr,
                AddI64 => Op.I64Add,
                SubI64 => Op.I64Sub,
                MulI64 => Op.I64Mul,
                AndI64 => Op.I64And,
                OrI64 => Op.I64Or,
                XorI64 => Op.I64Xor,
                ShlI64 => Op.I64Shl,
                ShrI64S => Op.I64ShrS,
                ShrI64U => Op.I64ShrU,
                RotlI64 => Op.I64Rotl,
                RotrI64 => Op.I64Rotr,
                WrapI64I32 => Op.I32WrapI64,
                ExtendS => Op.I64ExtendI32S,
                ExtendU => Op.I64ExtendI32U,
                LoadI32 or LoadF32 => Op.Load32,
                LoadI64 or LoadF64 => Op.Load64,
                LoadI8AsI32 => Op.I32Load8S,
                LoadU8AsI32 => Op.I32Load8U,
                LoadI16AsI32 => Op.I32Load16S,
                LoadU16AsI32 => Op.I32Load16U,
                LoadI8AsI64 => Op.I64Load8S,
                LoadU8AsI64 => Op.I64Load8U,
                LoadI16AsI64 => Op.I64Load16S,
                LoadU16AsI64 => Op.I64Load16U,
                LoadI32AsI64 => Op.I64Load32S,
                LoadU32AsI64 => Op.I64Load32U,
                StoreI32AsU8 or StoreI64AsU8 => Op.Store8,
                StoreI32AsU16 or StoreI64AsU16 => Op.Store16,
                StoreI32 or StoreF32 or StoreI64AsU32 => Op.Store32,
                StoreI64 or StoreF64 => Op.Store64,
                _ => Op.Fallback
            };

            if (instr is IMemoryArgumentInstruction memoryInstr) compiled.A = unchecked((int)memoryInstr.Offset);

            compiled.B = instr switch
            {
                LoadI32 => (int)ValueType.I32,
                LoadF32 => (int)ValueType.F32,
                LoadI64 => (int)ValueType.I64,
                LoadF64 => (int)ValueType.F64,
                _ => 0
            };
        }

        private struct BlockState
        {
            // stack height without the parameters
            public int Height;

            // the number of the values a branch carries
            public int Arity;

            // the branch target, or -1 for the function
            public int Index;
            public bool IsLoop;
            public int NumParameters;
            public int NumResults;
            public End End;

            // whether the code before the block was unreachable
            public bool IsUnreachable;
        }
    }
}
