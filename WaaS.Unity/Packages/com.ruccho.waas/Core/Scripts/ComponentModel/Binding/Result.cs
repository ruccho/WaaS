#nullable enable

using System;

namespace WaaS.ComponentModel.Binding
{
    public readonly struct Result<TOk, TError>
    {
        public readonly bool isOk;
        private readonly TOk ok;
        private readonly TError error;

        public TOk Unwrap()
        {
            return isOk ? ok : throw new InvalidOperationException("Result is not Ok");
        }

        public TError UnwrapError()
        {
            return isOk ? throw new InvalidOperationException("Result is Ok") : error;
        }
    }

    public readonly struct None
    {
    }
}