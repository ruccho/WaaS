using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    public readonly struct BlockResultType
    {
        public readonly bool IsLoop;
        private readonly bool useTypeIndex;
        private readonly uint typeIndex;
        private readonly ValueType? type;
        private readonly uint arity;

        public bool MatchSignature(in ValidationContext context, BlockResultType other)
        {
            var aritySelf = IsLoop ? 0 : GetArity(context);
            var arityOther = other.IsLoop ? 0 : other.GetArity(context);

            if (aritySelf != arityOther) return false;

            for (var i = 0; i < aritySelf; i++)
            {
                var elementSelf = GetElement(context, i);
                var elementOther = other.GetElement(context, i);
                if (elementSelf != elementOther) return false;
            }

            return true;
        }

        public uint GetArity(in ValidationContext context)
        {
            if (useTypeIndex) return (uint)context.Module.TypeSection.FuncTypes.Span[(int)typeIndex].ResultTypes.Length;

            return arity;
        }

        public ValueType GetElement(in ValidationContext context, int index)
        {
            if (useTypeIndex) return context.Module.TypeSection.FuncTypes.Span[(int)typeIndex].ResultTypes.Span[index];

            if (index != 0) throw new InvalidOperationException();
            return type!.Value;
        }


        public BlockResultType(BlockType blockType, bool isLoop)
        {
            IsLoop = isLoop;
            useTypeIndex = false;
            typeIndex = 0;
            type = blockType.Type;
            arity = blockType.Type.HasValue ? 1u : 0u;
        }

        public BlockResultType(uint typeIndex)
        {
            IsLoop = false;
            useTypeIndex = true;
            this.typeIndex = typeIndex;
            type = null;
            arity = 0;
        }
    }

    public class Function
    {
        public Function(FunctionType type, FunctionBody function, uint typeIndex)
        {
            Type = type;
            Body = function;
            TypeIndex = typeIndex;
        }

        private uint TypeIndex { get; }
        public FunctionType Type { get; }
        public FunctionBody Body { get; }
        public uint? MaxStackDepth { get; private set; }
        public uint? MaxBlockDepth { get; private set; }

        internal void Validate(in ValidationContext context)
        {
            try
            {
                checked
                {
                    // block depth
                    var instructions = Body.Instructions.Span;
                    var maxBlockDepth = 1;
                    var blockDepth = 1;
                    for (var i = 0; i < instructions.Length; i++)
                    {
                        var instr = instructions[i];
                        instr.Validate(context);

                        if (instr is BlockInstruction)
                        {
                            blockDepth++;
                            if (maxBlockDepth < blockDepth) maxBlockDepth = blockDepth;
                        }
                        else if (instr is End)
                        {
                            blockDepth--;
                        }
                    }

                    if (blockDepth != 0) throw new InvalidCodeException();

                    Span<BlockResultType> blockResultTypes = stackalloc BlockResultType[maxBlockDepth];
                    var blockResultTypeStack = new BlockResultTypeStack(blockResultTypes);

                    blockResultTypeStack.Push(new BlockResultType(context.CurrentFunction.TypeIndex));

                    ValidateBlock(context, ref blockResultTypeStack, 0,
                        out var maxStackDepth);
                    MaxStackDepth = maxStackDepth;
                    MaxBlockDepth = (uint)maxBlockDepth;
                }
            }
            catch (InvalidCodeException ex)
            {
                ex.Function ??= context.CurrentFunction;
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidCodeException("", ex, context.CurrentFunction);
            }
        }

        private void ValidateBlock(
            in ValidationContext context,
            ref BlockResultTypeStack blockResultTypeStack,
            int startIndex,
            out uint maxStackDepth)
        {
            checked
            {
                var instructions = Body.Instructions.Span;

                // Count max stack depth of current block to avoid heap allocation of type tracking of stack values.
                // Counting max stack depth of current block requires to know result count of the nested blocks.
                // Result count of the nested blocks are not always same as the arity of the block.
                // So first we run recursive ValidateBlock() calls to know the result state of the nested blocks.

                // stack depth to evaluate this block
                uint localStackDepth = 0;
                uint localMaxStackDepth = 0;

                // max stack depth include nested blocks
                maxStackDepth = 0;

                var isUnreachable = false;
                for (var i = startIndex;; i++)
                {
                    var instr = instructions[i];

                    var (popCount, pushCount) = instr.PreValidateStackState(context);

                    try
                    {
                        localStackDepth -= popCount;
                    }
                    catch (OverflowException) when (isUnreachable)
                    {
                        localStackDepth = 0;
                    }

                    var initialDepth = localStackDepth + 1 /* label */;

                    if (instr is BlockInstruction blockInstr)
                    {
                        var innerBlockType = blockInstr.BlockType;

                        var blockResultTypeDepth = blockResultTypeStack.Depth;
                        blockResultTypeStack.Push(new BlockResultType(innerBlockType, blockInstr is Loop));

                        {
                            ValidateBlock(
                                context,
                                ref blockResultTypeStack,
                                i + 1,
                                out var innerMaxStackDepth);

                            var globalMaxDepth = initialDepth + innerMaxStackDepth;
                            if (maxStackDepth < globalMaxDepth) maxStackDepth = globalMaxDepth;
                        }

                        if (instr is If @if)
                        {
                            if (@if.Else is not null)
                            {
                                var @else = @if.Else;

                                ValidateBlock(
                                    context,
                                    ref blockResultTypeStack,
                                    (int)(@else.Index + 1),
                                    out var innerMaxStackDepth);

                                var globalMaxDepth = initialDepth + innerMaxStackDepth;
                                if (maxStackDepth < globalMaxDepth) maxStackDepth = globalMaxDepth;
                            }
                            else if (@if.BlockType.Type.HasValue)
                            {
                                throw new InvalidCodeException("type mismatch");
                            }
                        }

                        blockResultTypeStack.Unwind(blockResultTypeDepth);

                        // push results
                        localStackDepth +=
                            innerBlockType.Type.HasValue ? 1u : 0u;
                        if (localMaxStackDepth < localStackDepth) localMaxStackDepth = localStackDepth;
                        if (maxStackDepth < localStackDepth) maxStackDepth = localStackDepth;

                        // go to next
                        i = (int)blockInstr.End.Index;
                        continue;
                    }

                    localStackDepth += pushCount;
                    if (localMaxStackDepth < localStackDepth) localMaxStackDepth = localStackDepth;
                    if (maxStackDepth < localStackDepth) maxStackDepth = localStackDepth;

                    if (instr is BrIf brIf)
                    {
                        // check arity
                        var info = blockResultTypeStack.Get((int)brIf.LabelIndex);
                        if (!info.IsLoop && !isUnreachable && localStackDepth < info.GetArity(context))
                            throw new InvalidCodeException();
                    }

                    if (instr is BrTable brTable)
                    {
                        // check arity

                        BlockResultType cursor;

                        {
                            var info = blockResultTypeStack.Get((int)brTable.DefaultLabelIndex);
                            cursor = info;
                            if (!info.IsLoop && !isUnreachable && localStackDepth < info.GetArity(context))
                                throw new InvalidCodeException();
                        }

                        foreach (var index in brTable.LabelIndices.Span)
                        {
                            var info = blockResultTypeStack.Get((int)index);
                            if (!cursor.MatchSignature(context, info))
                                throw new InvalidCodeException(); // signature consistency check
                            if (!info.IsLoop && !isUnreachable && localStackDepth < info.GetArity(context))
                                throw new InvalidCodeException();
                        }

                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is Br br)
                    {
                        // check arity
                        var info = blockResultTypeStack.Get((int)br.LabelIndex);
                        if (!info.IsLoop && !isUnreachable && localStackDepth < info.GetArity(context))
                            throw new InvalidCodeException();
                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is Unreachable)
                    {
                        var info = blockResultTypeStack.Get(0);
                        if (!info.IsLoop && !isUnreachable && info.GetArity(context) is not (0 or 1))
                            throw new InvalidCodeException();
                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is Return)
                    {
                        if (!isUnreachable && localStackDepth < context.CurrentFunction.Type.ResultTypes.Length)
                            throw new InvalidCodeException();
                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is End or Else) break;
                }

                // we got max size of stack then evaluate stack

                Span<ValueType> stack = stackalloc ValueType[(int)localMaxStackDepth];
                var stackState = new ValidationBlockStackState(stack);

                // isUnreachable = false;
                for (var i = startIndex;; i++)
                {
                    var instr = instructions[i];

                    instr.ValidateStackState(context, ref stackState);

                    if (instr is BlockInstruction blockInstr)
                    {
                        var type = blockInstr.BlockType.Type;
                        if (type.HasValue) stackState.Push(type.Value);

                        // go to next
                        i = (int)blockInstr.End.Index;
                        continue;
                    }

                    if (instr is BrIf brIf)
                    {
                        var info = blockResultTypeStack.Get((int)brIf.LabelIndex);
                        if (!info.IsLoop)
                        {
                            stackState.ValidateResults(context, info, false);

                            // pop and push results to close "any" values as actual type on the unreachable stack
                            var arity = info.GetArity(context);
                            for (var j = 0; j < arity; j++) stackState.PopAny();
                            for (var j = 0; j < arity; j++) stackState.Push(info.GetElement(context, j));
                        }
                    }

                    if (instr is BrTable brTable)
                    {
                        foreach (var index in brTable.LabelIndices.Span)
                        {
                            var info = blockResultTypeStack.Get((int)index);
                            if (!info.IsLoop) stackState.ValidateResults(context, info, false);
                        }

                        {
                            var info = blockResultTypeStack.Get((int)brTable.DefaultLabelIndex);
                            if (!info.IsLoop) stackState.ValidateResults(context, info, false);
                        }
                        stackState.MakeUnreachable();
                    }

                    if (instr is Br br)
                    {
                        // check block type
                        var info = blockResultTypeStack.Get((int)br.LabelIndex);
                        if (!info.IsLoop) stackState.ValidateResults(context, info, false);
                        stackState.MakeUnreachable();
                    }

                    if (instr is Unreachable)
                        // isUnreachable = true;
                        //break;
                        stackState.MakeUnreachable();

                    if (instr is Return)
                    {
                        stackState.ValidateResults(context.CurrentFunction.Type.ResultTypes.Span, false);
                        stackState.MakeUnreachable();
                    }

                    if (instr is End or Else)
                    {
                        if (blockResultTypeStack.Depth > 1)
                        {
                            // end of block
                            var info = blockResultTypeStack.Get(0);
                            stackState.ValidateResults(context, info, true);
                        }
                        else
                        {
                            // end of function
                            stackState.ValidateResults(context.CurrentFunction.Type.ResultTypes.Span, true);
                        }

                        break;
                    }
                }
            }
        }

        private ref struct BlockResultTypeStack
        {
            private readonly Span<BlockResultType> stack;

            public int Depth { get; private set; }

            public void Push(BlockResultType type)
            {
                stack[Depth++] = type;
            }

            public ref readonly BlockResultType Get(int relativeIndex)
            {
                return ref stack[Depth - relativeIndex - 1];
            }

            public BlockResultTypeStack(Span<BlockResultType> stack)
            {
                Depth = 0;
                this.stack = stack;
            }

            public void Unwind(int depth)
            {
                if (depth > Depth) throw new InvalidOperationException();
                Depth = depth;
            }
        }
    }
}