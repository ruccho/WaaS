using System;

namespace WaaS.Runtime
{
    public class TrapException : Exception
    {
        public TrapException()
        {
        }

        public TrapException(string message) : base(message)
        {
        }
    }
}