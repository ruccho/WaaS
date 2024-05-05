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
            ReadBlock(null, ref reader, results, ref count);
            return results.ToArray();
        }


        private static void ReadBlock(
            BlockInstruction currentBlock,
            ref ModuleReader reader,
            List<Instruction> results,
            ref uint count,
            int depth = 0)
        {
            while (true)
            {
                var instruction = Read(ref reader, count++);

                results.Add(instruction);

                if (instruction is BlockInstruction newBlock)
                {
                    ReadBlock(newBlock, ref reader, results, ref count, ++depth);
                }
                else if (instruction is BlockDelimiterInstruction delimiter)
                {
                    currentBlock?.InjectDelimiter(delimiter);
                    if (instruction is End) return;
                }
            }
        }

        private static partial Instruction Read(ref ModuleReader reader,
            uint count);
    }
}