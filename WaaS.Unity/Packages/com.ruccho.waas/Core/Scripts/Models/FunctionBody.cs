using System;

namespace WaaS.Models
{
    public class FunctionBody
    {
        internal FunctionBody(ref ModuleReader reader)
        {
            var size = reader.ReadUnalignedLeb128U32();
            var next = reader.Position + size;

            var numLocals = reader.ReadVectorSize();

            uint numActualLocals = 0;
            var locals = new Local[numLocals];

            for (var i = 0; i < numLocals; i++)
            {
                var local = new Local(ref reader);
                locals[i] = local;
                checked
                {
                    numActualLocals += local.Count;
                }
            }

            if (numActualLocals > ushort.MaxValue) throw new InvalidModuleException("Too many locals");

            Locals = locals;

            Instructions = InstructionReader.ReadTillEnd(ref reader);

            if (next != reader.Position) throw new InvalidModuleException();
        }

        public ReadOnlyMemory<Local> Locals { get; }
        public ReadOnlyMemory<Instruction> Instructions { get; }
    }
}