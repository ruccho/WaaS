using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     An instruction of the register code. Its operands are the indices of the slots from the base of the frame, where
    ///     the locals are followed by the operand stack.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct RegisterInstruction
    {
        [FieldOffset(0)] public RegisterOp Op;

        // a value type, or the table index of call_indirect
        [FieldOffset(1)] public byte Tag;

        // the fuel consumed by the basic block starting at this instruction
        [FieldOffset(2)] public ushort Cost;

        [FieldOffset(4)] public int A;
        [FieldOffset(8)] public int B;
        [FieldOffset(12)] public int C;
        [FieldOffset(8)] public ulong Value;
    }

    /// <summary>
    ///     The start of a basic block in <see cref="CompiledFunction.BaseCode" />.
    /// </summary>
    internal struct RegisterBlock
    {
        public int Index;
        public int Height;
    }

    /// <summary>
    ///     Translates the code for the operand stack into the code that names the slots of its operands.
    /// </summary>
    /// <remarks>
    ///     Values are not moved to the operand stack as long as they are used in the same basic block: a value on the
    ///     operand stack is tracked as a reference to a local, a constant or its own slot. At the end of a basic block every
    ///     value is in its own slot, so that the register code and the base code have the same state there. This is what
    ///     lets the execution move to the base code at the start of any basic block, and what makes a position of the base
    ///     code a sufficient place to resume at.
    /// </remarks>
    internal sealed class RegisterCompiler
    {
        private readonly CompiledInstruction[] baseCode;
        private readonly BranchTarget[] baseBranchTargets;
        private readonly List<RegisterInstruction> code = new();
        private readonly List<int> origins = new();
        private readonly List<(int code, RegisterBlock block)> blocks = new();
        private readonly List<int> branchTargets = new();
        private readonly List<(int code, int field, int target)> fixups = new();
        private readonly List<(int table, int target)> tableFixups = new();
        private readonly List<Entry> stack = new();
        private readonly int numLocals;
        private readonly int[] entryIndices;

        private int current;
        private int pendingCost;
        private int blockStart;
        private int blockHeight;

        // the index of the first instruction which can be changed by the following ones
        private int peepholeLimit;

        private RegisterCompiler(CompiledInstruction[] baseCode, BranchTarget[] baseBranchTargets, int numLocals)
        {
            this.baseCode = baseCode;
            this.baseBranchTargets = baseBranchTargets;
            this.numLocals = numLocals;
            entryIndices = new int[baseCode.Length + 1];
            Array.Fill(entryIndices, -1);
        }

        private int Slot(int height)
        {
            return numLocals + height;
        }

        private int Height => stack.Count;

        public static void Compile(CompiledInstruction[] baseCode, BranchTarget[] baseBranchTargets, bool[] starts,
            ushort[] costs, int[] heights, bool[] unreachable, int[] pops, int[] pushes, int numLocals,
            out RegisterInstruction[] code, out int[] origins, out int[] entryIndices, out int[] blockCodeIndices,
            out RegisterBlock[] blocks, out int[] branchTargets)
        {
            var compiler = new RegisterCompiler(baseCode, baseBranchTargets, numLocals);
            compiler.Run(starts, costs, heights, unreachable, pops, pushes);

            code = compiler.code.ToArray();
            origins = compiler.origins.ToArray();
            entryIndices = compiler.entryIndices;
            branchTargets = compiler.branchTargets.ToArray();

            blockCodeIndices = new int[compiler.blocks.Count];
            blocks = new RegisterBlock[compiler.blocks.Count];
            for (var i = 0; i < blocks.Length; i++)
            {
                blockCodeIndices[i] = compiler.blocks[i].code;
                blocks[i] = compiler.blocks[i].block;
            }
        }

        private void Run(bool[] starts, ushort[] costs, int[] heights, bool[] unreachable, int[] pops, int[] pushes)
        {
            var reachable = true;
            for (current = 0; current < baseCode.Length; current++)
            {
                if (starts[current])
                {
                    if (reachable) Flush();
                    if (pendingCost != 0) Emit(new RegisterInstruction { Op = RegisterOp.Nop });

                    reachable = !unreachable[current];
                    if (reachable)
                    {
                        ResetStack(heights[current]);
                        entryIndices[current] = code.Count;
                        pendingCost = costs[current];
                        blockStart = current;
                        blockHeight = heights[current];
                        peepholeLimit = code.Count;
                    }
                }

                if (!reachable) continue;

                reachable = Translate(pops[current], pushes[current]);
            }

            if (pendingCost != 0) Emit(new RegisterInstruction { Op = RegisterOp.Nop });

            foreach (var (index, field, target) in fixups)
            {
                var instruction = code[index];
                var value = entryIndices[target];
                if (value < 0) throw new InvalidOperationException("A branch to an unreachable block.");
                switch (field)
                {
                    case 0:
                        instruction.A = value;
                        break;
                    case 1:
                        instruction.B = value;
                        break;
                    default:
                        instruction.C = value;
                        break;
                }

                code[index] = instruction;
            }

            foreach (var (table, target) in tableFixups) branchTargets[table] = entryIndices[target];
        }

        private void ResetStack(int height)
        {
            stack.Clear();
            for (var i = 0; i < height; i++) stack.Add(Entry.InSlot);
        }

        private int Emit(in RegisterInstruction instruction)
        {
            var value = instruction;
            if (pendingCost != 0)
            {
                value.Cost = (ushort)pendingCost;
                blocks.Add((code.Count, new RegisterBlock { Index = blockStart, Height = blockHeight }));
                pendingCost = 0;
            }

            code.Add(value);
            origins.Add(current);
            return code.Count - 1;
        }

        // moves a value to its own slot
        private void Materialize(int height)
        {
            var entry = stack[height];
            switch (entry.Kind)
            {
                case EntryKind.Local:
                    Emit(new RegisterInstruction { Op = RegisterOp.Copy, A = Slot(height), B = entry.Local });
                    break;
                case EntryKind.Const:
                    Emit(new RegisterInstruction
                        { Op = RegisterOp.Const, A = Slot(height), Tag = entry.Tag, Value = entry.Value });
                    break;
                default:
                    return;
            }

            stack[height] = Entry.InSlot;
        }

        private void Flush()
        {
            for (var i = 0; i < stack.Count; i++) Materialize(i);
        }

        // keeps the values referring to the local when it is written
        private void FlushLocal(int local)
        {
            for (var i = 0; i < stack.Count; i++)
                if (stack[i].Kind == EntryKind.Local && stack[i].Local == local)
                    Materialize(i);
        }

        private bool IsLocalReferenced(int local)
        {
            foreach (var entry in stack)
                if (entry.Kind == EntryKind.Local && entry.Local == local)
                    return true;

            return false;
        }

        private Entry Pop()
        {
            var entry = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return entry;
        }

        // the slot which holds the value at the top
        private int PopSlot()
        {
            Materialize(Height - 1 >= 0 ? Height - 1 : throw new InvalidCodeException("stack underflow"));
            Pop();
            return Slot(Height);
        }

        private int PopOperand()
        {
            var height = Height - 1;
            if (stack[height].Kind == EntryKind.Local) return Pop().Local;
            return PopSlot();
        }

        private void EmitResult(RegisterInstruction instruction)
        {
            instruction.A = Slot(Height);
            Emit(instruction);
            stack.Add(Entry.InSlot);
        }

        private bool TryPopImmediate32(out int value)
        {
            // an i32 constant is zero-extended, and any of its values fits in the operand
            var top = stack[^1];
            if (top.Kind == EntryKind.Const && top.Tag == (byte)ValueType.I32)
            {
                value = unchecked((int)(uint)top.Value);
                Pop();
                return true;
            }

            return TryPopImmediate64(out value);
        }

        private bool TryPopImmediate64(out int value)
        {
            var top = stack[^1];
            if (top.Kind == EntryKind.Const && top.Tag == (byte)ValueType.I64)
            {
                var signed = unchecked((long)top.Value);
                if (signed is >= int.MinValue and <= int.MaxValue)
                {
                    value = (int)signed;
                    Pop();
                    return true;
                }
            }

            value = default;
            return false;
        }

        private void TranslateBinary(RegisterOp op)
        {
            if (RegisterOpTable.TryGetImmediate(op, out var immediateOp) && TryPopImmediate32(out var immediate))
            {
                var lhs = PopOperand();
                EmitResult(new RegisterInstruction { Op = immediateOp, B = lhs, C = immediate });
                return;
            }

            var right = PopOperand();
            var left = PopOperand();
            EmitResult(new RegisterInstruction { Op = op, B = left, C = right });
        }

        private bool IsLastResult(int height, out RegisterInstruction last)
        {
            last = default;
            if (code.Count <= peepholeLimit || stack[height].Kind != EntryKind.Slot) return false;
            last = code[^1];
            return last.A == Slot(height) && RegisterOpTable.WritesResult(last.Op);
        }

        private void TranslateLocalSet(int local, bool tee)
        {
            var top = Height - 1;
            if (IsLastResult(top, out var last) && !IsLocalReferenced(local))
            {
                // the operation writes the local instead of the operand stack
                last.A = local;
                code[^1] = last;
                Pop();
                if (tee) stack.Add(Entry.OfLocal(local));
                return;
            }

            var entry = stack[top];
            if (entry.Kind == EntryKind.Local && entry.Local == local)
            {
                if (!tee) Pop();
                return;
            }

            // the other values which refer to the local keep the old value
            for (var i = 0; i < top; i++)
                if (stack[i].Kind == EntryKind.Local && stack[i].Local == local)
                    Materialize(i);

            switch (entry.Kind)
            {
                case EntryKind.Const:
                    Emit(new RegisterInstruction { Op = RegisterOp.Const, A = local, Tag = entry.Tag, Value = entry.Value });
                    break;
                case EntryKind.Local:
                    Emit(new RegisterInstruction { Op = RegisterOp.Copy, A = local, B = entry.Local });
                    break;
                default:
                    Emit(new RegisterInstruction { Op = RegisterOp.Copy, A = local, B = Slot(top) });
                    break;
            }

            Pop();
            if (tee) stack.Add(entry.Kind == EntryKind.Const ? entry : Entry.OfLocal(local));
        }

        // emits the moves of the values a branch carries, and returns whether they were necessary
        private void EmitBranchValues(int height, int arity)
        {
            var source = Height - arity;
            if (source == height) return;

            for (var i = 0; i < arity; i++)
                Emit(new RegisterInstruction { Op = RegisterOp.Copy, A = Slot(height + i), B = Slot(source + i) });
        }

        private void EmitJump(int target, bool loop)
        {
            var index = Emit(new RegisterInstruction { Op = loop ? RegisterOp.JumpLoop : RegisterOp.Jump });
            fixups.Add((index, 0, target));
        }

        private void EmitReturn()
        {
            Emit(new RegisterInstruction { Op = RegisterOp.Return, A = Height });
        }

        // a conditional branch whose condition is at the top. the values below it stay in their own slots
        private void TranslateConditionalBranch(int target, int height, int arity, bool loop, bool negate,
            bool toFunction)
        {
            var needsMoves = toFunction || Height - 1 - arity != height;

            int condition;
            var canFuse = false;
            RegisterInstruction last = default;
            if (stack[^1].Kind == EntryKind.Local)
            {
                condition = Pop().Local;
                Flush();
            }
            else
            {
                Flush();
                canFuse = !needsMoves && IsLastResult(Height - 1, out last) && CanFuse(last.Op, negate, loop);
                Pop();
                condition = Slot(Height);
            }

            if (needsMoves)
            {
                // the moves are executed only when the branch is taken
                pendingSkips.Add(Emit(new RegisterInstruction
                    { Op = negate ? RegisterOp.BrIf : RegisterOp.BrIfZero, A = condition }));
                if (toFunction)
                {
                    EmitReturn();
                }
                else
                {
                    EmitBranchValues(height, arity);
                    EmitJump(target, loop);
                }

                return;
            }

            if (canFuse)
            {
                FuseBranch(last, target, negate, loop);
                return;
            }

            var op = loop
                ? negate ? RegisterOp.BrIfZeroLoop : RegisterOp.BrIfLoop
                : negate ? RegisterOp.BrIfZero : RegisterOp.BrIf;
            fixups.Add((Emit(new RegisterInstruction { Op = op, A = condition }), 1, target));
        }

        private readonly List<int> pendingSkips = new();

        private static bool CanFuse(RegisterOp op, bool negate, bool loop)
        {
            return op is RegisterOp.I32Eqz or RegisterOp.I64Eqz ||
                   RegisterOpTable.TryGetCompareBranch(op, negate, loop, out _);
        }

        // the comparison writes the condition, which is used only by the branch
        private void FuseBranch(RegisterInstruction last, int target, bool negate, bool loop)
        {
            var index = code.Count - 1;
            var cost = last.Cost;
            if (last.Op is RegisterOp.I32Eqz)
            {
                var op = loop
                    ? negate ? RegisterOp.BrIfLoop : RegisterOp.BrIfZeroLoop
                    : negate ? RegisterOp.BrIf : RegisterOp.BrIfZero;
                code[index] = new RegisterInstruction { Op = op, A = last.B, Cost = cost };
                fixups.Add((index, 1, target));
                return;
            }

            RegisterOp branch;
            if (last.Op is RegisterOp.I64Eqz)
            {
                RegisterOpTable.TryGetCompareBranch(RegisterOp.I64EqImm, negate, loop, out branch);
                code[index] = new RegisterInstruction { Op = branch, A = last.B, B = 0, Cost = cost };
            }
            else
            {
                RegisterOpTable.TryGetCompareBranch(last.Op, negate, loop, out branch);
                code[index] = new RegisterInstruction { Op = branch, A = last.B, B = last.C, Cost = cost };
            }

            fixups.Add((index, 2, target));
        }

        private void ResolveSkips()
        {
            // the instruction after a skipped branch, which is the start of the next block
            foreach (var skip in pendingSkips)
            {
                var instruction = code[skip];
                instruction.B = code.Count;
                code[skip] = instruction;
            }

            pendingSkips.Clear();
        }

        // returns whether the next instruction is reachable
        private bool Translate(int pops, int pushes)
        {
            ref var instr = ref baseCode[current];
            var reachable = TranslateCore(ref instr, pops, pushes);
            ResolveSkips();
            return reachable;
        }

        private bool TranslateCore(ref CompiledInstruction instr, int pops, int pushes)
        {
            switch (instr.Op)
            {
                case Op.Nop:
                    return true;
                case Op.Drop:
                    Pop();
                    return true;
                case Op.LocalGet:
                    stack.Add(Entry.OfLocal(instr.A));
                    return true;
                case Op.LocalSet:
                    TranslateLocalSet(instr.A, false);
                    return true;
                case Op.LocalTee:
                    TranslateLocalSet(instr.A, true);
                    return true;
                case Op.Const:
                    stack.Add(Entry.OfConst((byte)instr.A, instr.Value));
                    return true;
                case Op.GlobalGet:
                    EmitResult(new RegisterInstruction { Op = RegisterOp.GlobalGet, B = instr.A });
                    return true;
                case Op.GlobalSet:
                {
                    var value = PopOperand();
                    Emit(new RegisterInstruction { Op = RegisterOp.GlobalSet, A = instr.A, B = value });
                    return true;
                }
                case Op.Select:
                {
                    var condition = PopOperand();
                    var second = PopOperand();
                    var height = Height - 1;
                    Materialize(height);
                    Emit(new RegisterInstruction
                        { Op = RegisterOp.SelectLocal, A = Slot(height), B = second, C = condition });
                    return true;
                }
                case Op.If:
                {
                    var target = instr.A;
                    TranslateConditionalBranch(target, Height - 1, 0, false, true, false);
                    return true;
                }
                case Op.Jump:
                    Flush();
                    EmitJump(instr.A, false);
                    return false;
                case Op.Br:
                case Op.BrLoop:
                    Flush();
                    EmitBranchValues(instr.B, instr.C);
                    EmitJump(instr.A, instr.Op == Op.BrLoop);
                    return false;
                case Op.BrIf:
                case Op.BrIfLoop:
                    TranslateConditionalBranch(instr.A, instr.B, instr.C, instr.Op == Op.BrIfLoop, false, false);
                    return true;
                case Op.ReturnIf:
                    TranslateConditionalBranch(-1, 0, 0, false, false, true);
                    return true;
                case Op.BrTable:
                    TranslateBrTable(ref instr);
                    return false;
                case Op.Return:
                    Flush();
                    EmitReturn();
                    return false;
                case Op.Call:
                case Op.CallIndirect:
                {
                    var table = 0;
                    var function = instr.A;
                    var isIndirect = instr.Op == Op.CallIndirect;
                    var index = -1;
                    if (isIndirect)
                    {
                        table = instr.C;
                        index = PopOperand();
                    }

                    Flush();
                    Emit(new RegisterInstruction
                    {
                        Op = isIndirect ? RegisterOp.CallIndirect : RegisterOp.Call,
                        A = Height,
                        B = function,
                        C = isIndirect ? index : instr.B,
                        Tag = checked((byte)table)
                    });
                    AdjustStack(pops - (isIndirect ? 1 : 0), pushes);
                    return true;
                }
                case Op.Fallback:
                    Flush();
                    Emit(new RegisterInstruction { Op = RegisterOp.Fallback, A = Height });
                    AdjustStack(pops, pushes);
                    return true;
                case Op.Load32:
                case Op.Load64:
                {
                    var op = instr.Op == Op.Load32 ? RegisterOp.Load32 : RegisterOp.Load64;
                    var address = PopOperand();
                    EmitResult(new RegisterInstruction { Op = op, B = address, C = instr.A, Tag = (byte)instr.B });
                    return true;
                }
                case Op.I32Load8S:
                case Op.I32Load8U:
                case Op.I32Load16S:
                case Op.I32Load16U:
                case Op.I64Load8S:
                case Op.I64Load8U:
                case Op.I64Load16S:
                case Op.I64Load16U:
                case Op.I64Load32S:
                case Op.I64Load32U:
                {
                    var address = PopOperand();
                    EmitResult(new RegisterInstruction { Op = GetLoad(instr.Op), B = address, C = instr.A });
                    return true;
                }
                case Op.Store8:
                case Op.Store16:
                case Op.Store32:
                case Op.Store64:
                {
                    var value = PopOperand();
                    var address = PopOperand();
                    Emit(new RegisterInstruction { Op = GetStore(instr.Op), A = address, B = value, C = instr.A });
                    return true;
                }
                case Op.Reinterpret:
                {
                    var value = PopOperand();
                    EmitResult(new RegisterInstruction { Op = RegisterOp.Reinterpret, B = value, Tag = (byte)instr.A });
                    return true;
                }
            }

            if (RegisterOpTable.TryGetBinary(instr.Op, out var binary))
            {
                TranslateBinary(binary);
                return true;
            }

            if (RegisterOpTable.TryGetUnary(instr.Op, out var unary))
            {
                var value = PopOperand();
                EmitResult(new RegisterInstruction { Op = unary, B = value });
                return true;
            }

            throw new InvalidOperationException($"Unsupported operation: {instr.Op}");
        }

        private void AdjustStack(int pops, int pushes)
        {
            for (var i = 0; i < pops; i++) Pop();
            for (var i = 0; i < pushes; i++) stack.Add(Entry.InSlot);
        }

        private void TranslateBrTable(ref CompiledInstruction instr)
        {
            var condition = PopOperand();
            Flush();

            var start = branchTargets.Count;
            var count = instr.B;
            for (var i = 0; i <= count; i++) branchTargets.Add(-1);
            Emit(new RegisterInstruction { Op = RegisterOp.BrTable, A = condition, B = start, C = count });

            for (var i = 0; i <= count; i++)
            {
                var target = baseBranchTargets[instr.A + i];

                // a branch to the loop observes the cancellation, and the values may have to be moved
                if (target.Index >= 0 && !target.IsLoop && Height - target.Arity == target.Height)
                {
                    tableFixups.Add((start + i, target.Index));
                    continue;
                }

                branchTargets[start + i] = code.Count;
                if (target.Index < 0)
                {
                    EmitReturn();
                }
                else
                {
                    EmitBranchValues(target.Height, target.Arity);
                    EmitJump(target.Index, target.IsLoop);
                }
            }
        }

        private static RegisterOp GetLoad(Op op)
        {
            return op switch
            {
                Op.I32Load8S => RegisterOp.I32Load8S,
                Op.I32Load8U => RegisterOp.I32Load8U,
                Op.I32Load16S => RegisterOp.I32Load16S,
                Op.I32Load16U => RegisterOp.I32Load16U,
                Op.I64Load8S => RegisterOp.I64Load8S,
                Op.I64Load8U => RegisterOp.I64Load8U,
                Op.I64Load16S => RegisterOp.I64Load16S,
                Op.I64Load16U => RegisterOp.I64Load16U,
                Op.I64Load32S => RegisterOp.I64Load32S,
                _ => RegisterOp.I64Load32U
            };
        }

        private static RegisterOp GetStore(Op op)
        {
            return op switch
            {
                Op.Store8 => RegisterOp.Store8,
                Op.Store16 => RegisterOp.Store16,
                Op.Store32 => RegisterOp.Store32,
                _ => RegisterOp.Store64
            };
        }

        private enum EntryKind : byte
        {
            Slot,
            Local,
            Const
        }

        private readonly struct Entry
        {
            public readonly EntryKind Kind;
            public readonly byte Tag;
            public readonly int Local;
            public readonly ulong Value;

            private Entry(EntryKind kind, byte tag, int local, ulong value)
            {
                Kind = kind;
                Tag = tag;
                Local = local;
                Value = value;
            }

            public static Entry InSlot => default;

            public static Entry OfLocal(int local)
            {
                return new Entry(EntryKind.Local, 0, local, 0);
            }

            public static Entry OfConst(byte tag, ulong value)
            {
                return new Entry(EntryKind.Const, tag, 0, value);
            }
        }
    }
}
