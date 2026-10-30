using System;
using System.Collections.Generic;

namespace WaaS.Models
{
    internal static partial class InstructionReader
    {
        public static ReadOnlyMemory<Instruction> ReadTillEnd(ref ModuleReader reader)
        {
            var results = new List<Instruction>();
            uint count = 0;

            // the blocks being read. the nesting is tracked without recursion, which would overflow the stack of the
            // thread on deeply nested blocks.
            var blocks = new Stack<BlockInstruction>();
            while (true)
            {
                var offset = reader.Position - reader.Origin;
                var instruction = Read(ref reader, count++);
                instruction.Offset = checked((uint)offset);

                results.Add(instruction);

                if (instruction is BlockInstruction newBlock)
                {
                    blocks.Push(newBlock);
                }
                else if (instruction is BlockDelimiterInstruction delimiter)
                {
                    // the end of the function
                    if (blocks.Count == 0)
                    {
                        if (instruction is End) return results.ToArray();
                        continue;
                    }

                    blocks.Peek().InjectDelimiter(delimiter);
                    if (instruction is End) blocks.Pop();
                }
            }
        }

        private static partial Instruction Read(ref ModuleReader reader,
            uint count);
    }
}