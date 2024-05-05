using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class InvalidCodeException : Exception
    {
        public InvalidCodeException(Function function = null)
        {
            Function = function;
        }

        public InvalidCodeException(string message, Function function = null) : base(message)
        {
            Function = function;
        }

        public InvalidCodeException(string message, Exception innerException, Function function = null) : base(message,
            innerException)
        {
            Function = function;
        }

        public Function Function { get; set; }
    }
}