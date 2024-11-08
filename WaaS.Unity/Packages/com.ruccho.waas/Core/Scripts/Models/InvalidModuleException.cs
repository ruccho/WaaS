using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Exception to be thrown when the module is invalid or unsupported.
    /// </summary>
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