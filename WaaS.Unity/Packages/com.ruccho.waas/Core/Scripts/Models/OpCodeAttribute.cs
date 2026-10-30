using System;

namespace WaaS.Models
{
    public class OpCodeAttribute : Attribute
    {
        public OpCodeAttribute(byte opCode)
        {
            OpCode = opCode;
        }

        /// <summary>
        ///     Specifies a prefixed opcode. The sub-opcode follows the prefix byte as LEB128 u32.
        /// </summary>
        public OpCodeAttribute(byte opCode, uint opCode1)
        {
            OpCode = opCode;
            OpCode1 = opCode1;
        }

        public byte OpCode { get; }
        public uint? OpCode1 { get; }
    }
}