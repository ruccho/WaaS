using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Used to validate the result type of a block.
    /// </summary>
    internal readonly struct BlockResultType
    {
        /// <summary>
        ///     Types of the values a branch to the block carries. Parameters for a loop, results otherwise.
        /// </summary>
        public readonly ReadOnlyMemory<ValueType> LabelTypes;

        /// <summary>
        ///     Types of the values on the stack at the end of the block.
        /// </summary>
        public readonly ReadOnlyMemory<ValueType> ResultTypes;

        public BlockResultType(BlockInstruction blockInstruction)
        {
            LabelTypes = blockInstruction is Loop ? blockInstruction.ParameterTypes : blockInstruction.ResultTypes;
            ResultTypes = blockInstruction.ResultTypes;
        }

        public BlockResultType(FunctionType functionType)
        {
            LabelTypes = functionType.ResultTypes;
            ResultTypes = functionType.ResultTypes;
        }
    }

    /// <summary>
    ///     Represents a function in the WebAssembly module.
    /// </summary>
    public class Function
    {
        public Function(FunctionType type, FunctionBody function, uint typeIndex, int functionIndex)
        {
            Type = type;
            Body = function;
            TypeIndex = typeIndex;
            FunctionIndex = functionIndex;
        }

        private uint TypeIndex { get; }
        public int FunctionIndex { get; }
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

                    var blockResultTypes = new BlockResultType[maxBlockDepth];
                    var blockResultTypeStack = new BlockResultTypeStack(blockResultTypes);

                    // the blocks are validated one by one without recursion, which would overflow the stack of the
                    // thread on deeply nested blocks
                    var pendingBlocks = new Stack<PendingBlock>();
                    var nestedBlocks = new List<PendingBlock>();
                    var stackBuffer = Array.Empty<ValueType>();
                    var maxStackDepth = 0u;

                    pendingBlocks.Push(new PendingBlock
                    {
                        StartIndex = 0,
                        ParameterTypes = ReadOnlyMemory<ValueType>.Empty,
                        ResultType = new BlockResultType(context.CurrentFunction.Type),
                        LabelDepth = 1,
                        BaseStackDepth = 0
                    });

                    while (pendingBlocks.Count > 0)
                    {
                        var block = pendingBlocks.Pop();

                        // the labels of the enclosing blocks are already on the stack
                        blockResultTypeStack.Unwind(block.LabelDepth - 1);
                        blockResultTypeStack.Push(block.ResultType);

                        nestedBlocks.Clear();
                        ValidateBlock(context, ref blockResultTypeStack, block, nestedBlocks, ref stackBuffer,
                            out var blockMaxStackDepth);
                        if (maxStackDepth < block.BaseStackDepth + blockMaxStackDepth)
                            maxStackDepth = block.BaseStackDepth + blockMaxStackDepth;

                        for (var i = nestedBlocks.Count - 1; i >= 0; i--) pendingBlocks.Push(nestedBlocks[i]);
                    }

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

        /// <summary>
        ///     Validates the instructions of a block except the nested blocks, which are added to
        ///     <paramref name="nestedBlocks" /> to be validated after this one.
        /// </summary>
        /// <param name="maxStackDepth">The maximum stack depth of the block from its start, excluding the nested blocks.</param>
        private void ValidateBlock(
            in ValidationContext context,
            ref BlockResultTypeStack blockResultTypeStack,
            in PendingBlock block,
            List<PendingBlock> nestedBlocks,
            ref ValueType[] stackBuffer,
            out uint maxStackDepth)
        {
            checked
            {
                var instructions = Body.Instructions.Span;
                var startIndex = block.StartIndex;
                var parameterTypes = block.ParameterTypes.Span;

                // Count max stack depth of current block to know the size of the type tracking of stack values.
                // The nested blocks leave their results on the stack, whose types are known from the block types.

                // stack depth to evaluate this block
                // the parameters of the block are on the stack at the beginning
                var localStackDepth = (uint)parameterTypes.Length;
                var localMaxStackDepth = localStackDepth;

                // max stack depth include nested blocks
                maxStackDepth = localStackDepth;

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
                        var nestedBlock = new PendingBlock
                        {
                            StartIndex = i + 1,
                            ParameterTypes = blockInstr.ParameterTypes,
                            ResultType = new BlockResultType(blockInstr),
                            LabelDepth = block.LabelDepth + 1,
                            BaseStackDepth = block.BaseStackDepth + initialDepth
                        };
                        nestedBlocks.Add(nestedBlock);

                        if (instr is If @if)
                        {
                            if (@if.Else is not null)
                            {
                                nestedBlock.StartIndex = (int)(@if.Else.Index + 1);
                                nestedBlocks.Add(nestedBlock);
                            }
                            else if (!MemoryMarshal.AsBytes(@if.ParameterTypes.Span)
                                         .SequenceEqual(MemoryMarshal.AsBytes(@if.ResultTypes.Span)))
                            {
                                // without "else", the parameters are passed through as the results
                                throw new InvalidCodeException("type mismatch");
                            }
                        }

                        // push results
                        localStackDepth += pushCount;
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
                        var arity = (uint)info.LabelTypes.Length;
                        if (!isUnreachable && localStackDepth < arity)
                            throw new InvalidCodeException();

                        // the label types are pushed back even on the unreachable stack
                        if (localStackDepth < arity) localStackDepth = arity;
                        if (localMaxStackDepth < localStackDepth) localMaxStackDepth = localStackDepth;
                        if (maxStackDepth < localStackDepth) maxStackDepth = localStackDepth;
                    }

                    if (instr is BrTable brTable)
                    {
                        // check arity

                        BlockResultType cursor;

                        {
                            var info = blockResultTypeStack.Get((int)brTable.DefaultLabelIndex);
                            cursor = info;
                            if (!isUnreachable && localStackDepth < info.LabelTypes.Length)
                                throw new InvalidCodeException();
                        }

                        foreach (var index in brTable.LabelIndices.Span)
                        {
                            var info = blockResultTypeStack.Get((int)index);
                            if (cursor.LabelTypes.Length != info.LabelTypes.Length)
                                throw new InvalidCodeException(); // arity consistency check
                            if (!isUnreachable && localStackDepth < info.LabelTypes.Length)
                                throw new InvalidCodeException();
                        }

                        // the label types are pushed back temporarily even on the unreachable stack
                        if (localMaxStackDepth < cursor.LabelTypes.Length)
                            localMaxStackDepth = (uint)cursor.LabelTypes.Length;

                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is Br br)
                    {
                        // check arity
                        var info = blockResultTypeStack.Get((int)br.LabelIndex);
                        if (!isUnreachable && localStackDepth < info.LabelTypes.Length)
                            throw new InvalidCodeException();
                        isUnreachable = true;
                        localStackDepth = 0;
                    }

                    if (instr is Unreachable)
                    {
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

                if (stackBuffer.Length < localMaxStackDepth) stackBuffer = new ValueType[localMaxStackDepth];
                var stack = stackBuffer.AsSpan(0, (int)localMaxStackDepth);
                var stackState = new ValidationBlockStackState(stack);
                foreach (var parameterType in parameterTypes) stackState.Push(parameterType);

                // isUnreachable = false;
                for (var i = startIndex;; i++)
                {
                    var instr = instructions[i];

                    instr.ValidateStackState(context, ref stackState);

                    if (instr is BlockInstruction blockInstr)
                    {
                        foreach (var resultType in blockInstr.ResultTypes.Span) stackState.Push(resultType);

                        // go to next
                        i = (int)blockInstr.End.Index;
                        continue;
                    }

                    if (instr is BrIf brIf)
                    {
                        var labelTypes = blockResultTypeStack.Get((int)brIf.LabelIndex).LabelTypes.Span;
                        stackState.ValidateResults(labelTypes, false);

                        // pop and push results to close "any" values as actual type on the unreachable stack
                        for (var j = 0; j < labelTypes.Length; j++) stackState.PopAny();
                        foreach (var labelType in labelTypes) stackState.Push(labelType);
                    }

                    if (instr is BrTable brTable)
                    {
                        foreach (var index in brTable.LabelIndices.Span)
                        {
                            var labelTypes = blockResultTypeStack.Get((int)index).LabelTypes.Span;
                            stackState.ValidateResults(labelTypes, false);

                            // pop and push to close "any" values so that every label sees the same types
                            for (var j = 0; j < labelTypes.Length; j++) stackState.PopAny();
                            foreach (var labelType in labelTypes) stackState.Push(labelType);
                        }

                        {
                            var info = blockResultTypeStack.Get((int)brTable.DefaultLabelIndex);
                            stackState.ValidateResults(info.LabelTypes.Span, false);
                        }
                        stackState.MakeUnreachable();
                    }

                    if (instr is Br br)
                    {
                        // check block type
                        var info = blockResultTypeStack.Get((int)br.LabelIndex);
                        stackState.ValidateResults(info.LabelTypes.Span, false);
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
                            stackState.ValidateResults(info.ResultTypes.Span, true);
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

        private struct PendingBlock
        {
            public int StartIndex;
            public ReadOnlyMemory<ValueType> ParameterTypes;
            public BlockResultType ResultType;

            // the number of the labels including the block
            public int LabelDepth;

            // the stack depth of the function at the start of the block
            public uint BaseStackDepth;
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