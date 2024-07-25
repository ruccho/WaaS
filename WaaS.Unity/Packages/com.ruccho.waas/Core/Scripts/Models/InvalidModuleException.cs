using System;

namespace WaaS.Models
{
    public class InvalidModuleException : Exception
    {
        public InvalidModuleException()
        {
        }

        public InvalidModuleException(string message) : base(message)
        {
        }

        public InvalidModuleException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}