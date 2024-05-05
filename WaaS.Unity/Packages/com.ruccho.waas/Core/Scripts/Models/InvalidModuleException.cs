using System;
using System.Runtime.Serialization;

namespace WaaS.Models
{
    public class InvalidModuleException : Exception
    {
        public InvalidModuleException()
        {
        }

        protected InvalidModuleException(SerializationInfo info, StreamingContext context) : base(info, context)
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