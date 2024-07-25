using System;

namespace WaaS.ComponentModel.Runtime
{
    public class LinkException : Exception
    {
        public LinkException()
        {
        }

        public LinkException(string message) : base(message)
        {
        }
    }
}