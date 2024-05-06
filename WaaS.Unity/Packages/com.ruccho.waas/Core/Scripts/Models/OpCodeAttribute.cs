using System;

namespace WaaS.Models
{
    public class OpCodeAttribute : Attribute
    {
        public OpCodeAttribute(byte opCode)
        {
            OpCode = opCode;
        }

        public OpCodeAttribute(byte opCode, byte opCode1)
        {
            OpCode = opCode;
            OpCode1 = opCode1;
        }

        public byte OpCode { get; }
        public byte? OpCode1 { get; }
    }
}