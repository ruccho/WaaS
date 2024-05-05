using System;

namespace WaaS.Models
{
    public class OpCodeAttribute : Attribute
    {
        public OpCodeAttribute(byte opCode)
        {
            OpCode = opCode;
        }

        public byte OpCode { get; }
    }
}