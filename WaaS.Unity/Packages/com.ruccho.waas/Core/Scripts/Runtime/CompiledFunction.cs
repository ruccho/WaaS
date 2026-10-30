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
    internal enum Op : byte
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
        CallIndirect, // A: type index, B: the number of the parameters

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

        F32Eq,
        F32Ne,
        F32Lt,
        F32Gt,
        F32Le,
        F32Ge,
        F64Eq,
        F64Ne,
        F64Lt,
        F64Gt,
        F64Le,
        F64Ge,
        F32Add,
        F32Sub,
        F32Mul,
        F32Div,
        F64Add,
        F64Sub,
        F64Mul,
        F64Div,
        F32ConvertI32S,
        F32ConvertI32U,
        F32ConvertI64S,
        F32ConvertI64U,
        F64ConvertI32S,
        F64ConvertI32U,
        F64ConvertI64S,
        F64ConvertI64U,
        F32DemoteF64,
        F64PromoteF32,
        Reinterpret,

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
        Store64,

        // superinstructions, which execute a sequence of instructions at once
        LocalGet2, // local.get A, local.get B
        LocalGetConst, // local.get A, i32.const B
        LocalGetI32AddConst, // local.get A, i32.const B, i32.add
        I32AddConst, // i32.const B, i32.add
        LocalSetGet, // local.set A, local.get B
        LocalGet3, // local.get A, local.get B, local.get C
        LocalGet2Const, // local.get A, local.get B, i32.const C
        LocalTeeConst, // local.tee A, i32.const B
        LocalGetI32AddTee, // local.get A, i32.add, local.tee B
        I32AndConst, // i32.const B, i32.and
        I32ShlAdd, // i32.shl, i32.add
        I32ShrUTee, // i32.shr_u, local.tee A

        // superinstructions with memory accesses, which may throw
        Load32Tee, // (i32|f32).load A (B: value type), local.tee C
        ConstLoad32Tee, // i32.const B, i32.load A, local.tee C
        Store32Const2, // (i32|f32|i64).store32 A, i32.const B, i32.const C
        Store32LocalGet2, // (i32|f32|i64).store32 A, local.get B, local.get C

        // superinstructions ending with br_if. A, B and C are the same as BrIf
        BrIfZero, // i32.eqz, br_if
        BrIfZeroLoop,
        BrIfI32Ne, // i32.ne, br_if
        BrIfI32NeLoop,
        BrIfI32GtU, // i32.gt_u, br_if
        BrIfI32GtULoop,
        BrIfI32LtU, // i32.lt_u, br_if
        BrIfI32LtULoop
    }

    /// <summary>
    ///     An instruction translated for the interpreter loop. Each one corresponds to the instruction at the same index.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct CompiledInstruction
    {
        [FieldOffset(0)] public Op Op;

        // the number of the instructions executed at once
        [FieldOffset(1)] public byte Length;

        // the fuel consumed when the instruction is executed. the fuel of a basic block is consumed at once by its
        // first instruction, and the other instructions of the block have zero.
        [FieldOffset(2)] public ushort Cost;

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
        private CompiledFunction(CompiledInstruction[] code, CompiledInstruction[] baseCode,
            BranchTarget[] branchTargets, StackValueItem[] initialLocals, int numParameters, int numResults,
            int maxStackDepth)
        {
            Code = code;
            BaseCode = baseCode;
            BranchTargets = branchTargets;
            InitialLocals = initialLocals;
            NumParameters = numParameters;
            NumResults = numResults;
            MaxStackDepth = maxStackDepth;
        }

        /// <summary>
        ///     The instructions with superinstructions. A sequence of instructions which falls through is executed at once
        ///     from the first one.
        /// </summary>
        public CompiledInstruction[] Code { get; }

        /// <summary>
        ///     The instructions without superinstructions, each of which executes only the instruction at the index and
        ///     consumes the fuel by itself. Used when the remaining fuel is not enough to execute a basic block.
        /// </summary>
        public CompiledInstruction[] BaseCode { get; }
        public BranchTarget[] BranchTargets { get; }

        /// <summary>
        ///     The initial values of the locals including the parameters.
        /// </summary>
        public StackValueItem[] InitialLocals { get; }

        public int NumParameters { get; }
        public int NumResults { get; }

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

                    compiled.Length = 1;
                    compiled.Cost = 1;
                    if (maxHeight < height) maxHeight = height;
                }

                // the stack depth from the validation also counts the labels and is not smaller
                var maxStackDepth = Math.Max(maxHeight, (int)(function.MaxStackDepth ?? 0));

                var branchTargetArray = branchTargets.ToArray();
                var fused = Fuse(code);
                AssignBlockCosts(fused, FindBlockStarts(code, branchTargetArray));

                return new CompiledFunction(fused, code, branchTargetArray, initialLocals, parameterTypes.Length,
                    function.Type.ResultTypes.Length, maxStackDepth);
            }
        }

        // whether the operation always continues to the next instruction without throwing
        private static bool IsStraight(Op op)
        {
            return op == Op.Nop || (op >= Op.Drop && op <= Op.Reinterpret) ||
                   (op >= Op.LocalGet2 && op <= Op.I32ShrUTee);
        }

        // whether the operation may continue at an instruction other than the next one. the operations executed with
        // Instruction.Execute may push a frame and continue at the next instruction after the frame completes.
        private static bool IsBlockEnd(Op op)
        {
            return op == Op.Fallback || (op >= Op.Return && op <= Op.CallIndirect) || op >= Op.BrIfZero;
        }

        /// <summary>
        ///     Finds the instructions where the execution may start other than by falling through: the start of the
        ///     function, the targets of branches and the instructions following the ones which may continue elsewhere.
        /// </summary>
        private static bool[] FindBlockStarts(CompiledInstruction[] baseCode, BranchTarget[] branchTargets)
        {
            var starts = new bool[baseCode.Length + 1];
            starts[0] = true;
            for (var i = 0; i < baseCode.Length; i++)
            {
                ref var instr = ref baseCode[i];
                switch (instr.Op)
                {
                    case Op.If or Op.Jump or Op.Br or Op.BrLoop or Op.BrIf or Op.BrIfLoop:
                        starts[instr.A] = true;
                        break;
                }

                if (IsBlockEnd(instr.Op)) starts[i + 1] = true;
            }

            foreach (var target in branchTargets)
                if (target.Index >= 0)
                    starts[target.Index] = true;

            return starts;
        }

        /// <summary>
        ///     Makes the first instruction of each basic block consume the fuel of the whole block. A block continues
        ///     while the instructions fall through to the next one which is not the start of another block. The
        ///     instructions in the block may still throw, in which case the fuel of the rest of the block is consumed
        ///     as well.
        /// </summary>
        private static void AssignBlockCosts(CompiledInstruction[] code, bool[] starts)
        {
            // a block with more instructions than the cost can represent is split, which may change the other blocks
            bool changed;
            do
            {
                changed = false;
                for (var i = 0; i < code.Length; i++) code[i].Cost = 0;

                for (var i = 0; i < code.Length; i++)
                {
                    if (!starts[i]) continue;

                    var cost = 0;
                    var p = i;
                    while (true)
                    {
                        cost += code[p].Length;
                        if (IsBlockEnd(code[p].Op)) break;

                        var next = p + code[p].Length;
                        if (next >= code.Length || starts[next]) break;

                        if (cost + code[next].Length > ushort.MaxValue)
                        {
                            starts[next] = true;
                            changed = true;
                            break;
                        }

                        p = next;
                    }

                    code[i].Cost = (ushort)cost;
                }
            } while (changed);
        }

        private static bool IsI32Const(in CompiledInstruction instr)
        {
            return instr.Op == Op.Const && instr.A == (int)ValueType.I32;
        }

        /// <summary>
        ///     Creates the code with superinstructions. Every index still has its own entry, which is the target of
        ///     branches.
        /// </summary>
        private static CompiledInstruction[] Fuse(CompiledInstruction[] baseCode)
        {
            var code = (CompiledInstruction[])baseCode.Clone();
            for (var i = 0; i < code.Length; i++)
            {
                ref var fused = ref code[i];
                var first = baseCode[i];
                var second = i + 1 < baseCode.Length ? baseCode[i + 1] : default;
                var third = i + 2 < baseCode.Length ? baseCode[i + 2] : default;

                // the sequences are chosen by the frequencies in the scripts compiled by rustc
                if (first.Op == Op.LocalGet && IsI32Const(second) && third.Op == Op.I32Add)
                    fused = new CompiledInstruction
                        { Op = Op.LocalGetI32AddConst, Length = 3, A = first.A, B = second.B };
                else if (first.Op == Op.LocalGet && second.Op == Op.LocalGet && third.Op == Op.LocalGet)
                    fused = new CompiledInstruction
                        { Op = Op.LocalGet3, Length = 3, A = first.A, B = second.A, C = third.A };
                else if (first.Op == Op.LocalGet && second.Op == Op.LocalGet && IsI32Const(third))
                    fused = new CompiledInstruction
                        { Op = Op.LocalGet2Const, Length = 3, A = first.A, B = second.A, C = third.B };
                else if (first.Op == Op.LocalGet && second.Op == Op.I32Add && third.Op == Op.LocalTee)
                    fused = new CompiledInstruction
                        { Op = Op.LocalGetI32AddTee, Length = 3, A = first.A, B = third.A };
                else if (IsI32Const(first) && second.Op == Op.Load32 && second.B == (int)ValueType.I32 &&
                         third.Op == Op.LocalTee)
                    fused = new CompiledInstruction
                        { Op = Op.ConstLoad32Tee, Length = 3, A = second.A, B = first.B, C = third.A };
                else if (first.Op == Op.Store32 && IsI32Const(second) && IsI32Const(third))
                    fused = new CompiledInstruction
                        { Op = Op.Store32Const2, Length = 3, A = first.A, B = second.B, C = third.B };
                else if (first.Op == Op.Store32 && second.Op == Op.LocalGet && third.Op == Op.LocalGet)
                    fused = new CompiledInstruction
                        { Op = Op.Store32LocalGet2, Length = 3, A = first.A, B = second.A, C = third.A };
                else if (first.Op == Op.LocalGet && second.Op == Op.LocalGet)
                    fused = new CompiledInstruction { Op = Op.LocalGet2, Length = 2, A = first.A, B = second.A };
                else if (first.Op == Op.LocalGet && IsI32Const(second))
                    fused = new CompiledInstruction { Op = Op.LocalGetConst, Length = 2, A = first.A, B = second.B };
                else if (IsI32Const(first) && second.Op == Op.I32Add)
                    fused = new CompiledInstruction { Op = Op.I32AddConst, Length = 2, B = first.B };
                else if (first.Op == Op.LocalSet && second.Op == Op.LocalGet)
                    fused = new CompiledInstruction { Op = Op.LocalSetGet, Length = 2, A = first.A, B = second.A };
                else if (first.Op == Op.LocalTee && IsI32Const(second))
                    fused = new CompiledInstruction { Op = Op.LocalTeeConst, Length = 2, A = first.A, B = second.B };
                else if (IsI32Const(first) && second.Op == Op.I32And)
                    fused = new CompiledInstruction { Op = Op.I32AndConst, Length = 2, B = first.B };
                else if (first.Op == Op.I32Shl && second.Op == Op.I32Add)
                    fused = new CompiledInstruction { Op = Op.I32ShlAdd, Length = 2 };
                else if (first.Op == Op.I32ShrU && second.Op == Op.LocalTee)
                    fused = new CompiledInstruction { Op = Op.I32ShrUTee, Length = 2, A = second.A };
                else if (first.Op == Op.Load32 && second.Op == Op.LocalTee)
                    fused = new CompiledInstruction
                        { Op = Op.Load32Tee, Length = 2, A = first.A, B = first.B, C = second.A };
                else if (second.Op == Op.BrIf || second.Op == Op.BrIfLoop)
                {
                    var op = first.Op switch
                    {
                        Op.I32Eqz => Op.BrIfZero,
                        Op.I32Ne => Op.BrIfI32Ne,
                        Op.I32GtU => Op.BrIfI32GtU,
                        Op.I32LtU => Op.BrIfI32LtU,
                        _ => Op.Fallback
                    };

                    // the loop variant follows each operation
                    if (op != Op.Fallback)
                        fused = new CompiledInstruction
                        {
                            Op = second.Op == Op.BrIf ? op : op + 1,
                            Length = 2,
                            A = second.A,
                            B = second.B,
                            C = second.C
                        };
                }

                if (!IsStraight(fused.Op)) continue;

                // the following blocks, loops and ends do nothing
                var next = i + fused.Length;
                while (next < baseCode.Length && baseCode[next].Op == Op.Nop && fused.Length < byte.MaxValue)
                {
                    fused.Length++;
                    next++;
                }
            }

            return code;
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
                case CallIndirect callIndirect:
                    compiled.Op = Op.CallIndirect;
                    compiled.A = checked((int)callIndirect.FunctionTypeIndex);
                    compiled.B = context.Module.TypeSection.FuncTypes.Span[compiled.A].ParameterTypes.Length;
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
                EqF32 => Op.F32Eq,
                NeF32 => Op.F32Ne,
                LtF32 => Op.F32Lt,
                GtF32 => Op.F32Gt,
                LeF32S => Op.F32Le,
                GeF32S => Op.F32Ge,
                EqF64 => Op.F64Eq,
                NeF64 => Op.F64Ne,
                LtF64 => Op.F64Lt,
                GtF64 => Op.F64Gt,
                LeF64S => Op.F64Le,
                GeF64S => Op.F64Ge,
                AddF32 => Op.F32Add,
                SubF32 => Op.F32Sub,
                MulF32 => Op.F32Mul,
                DivF32 => Op.F32Div,
                AddF64 => Op.F64Add,
                SubF64 => Op.F64Sub,
                MulF64 => Op.F64Mul,
                DivF64 => Op.F64Div,
                ConvertI32F32S => Op.F32ConvertI32S,
                ConvertI32F32U => Op.F32ConvertI32U,
                ConvertI64F32S => Op.F32ConvertI64S,
                ConvertI64F32U => Op.F32ConvertI64U,
                ConvertI32F64S => Op.F64ConvertI32S,
                ConvertI32F64U => Op.F64ConvertI32U,
                ConvertI64F64S => Op.F64ConvertI64S,
                ConvertI64F64U => Op.F64ConvertI64U,
                DemoteF64F32 => Op.F32DemoteF64,
                PromoteF32F64 => Op.F64PromoteF32,
                ReinterpretF32I32 or ReinterpretF64I64 or ReinterpretI32F32 or ReinterpretI64F64 => Op.Reinterpret,
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

            if (compiled.Op == Op.Reinterpret)
                compiled.A = (int)(instr switch
                {
                    ReinterpretF32I32 => ValueType.I32,
                    ReinterpretF64I64 => ValueType.I64,
                    ReinterpretI32F32 => ValueType.F32,
                    _ => ValueType.F64
                });

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
