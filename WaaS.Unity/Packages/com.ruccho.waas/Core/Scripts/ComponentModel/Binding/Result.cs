#nullable enable

using System.Runtime.InteropServices;
using STask;
using WaaS.ComponentModel.Runtime;

#pragma warning disable CS1998

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Component result type.
    /// </summary>
    /// <typeparam name="TOk"></typeparam>
    /// <typeparam name="TError"></typeparam>
    [ComponentVariant]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct Result<TOk, TError>
    {
        [ComponentCase] public TOk? Ok { get; private init; }
        [ComponentCase] public TError? Error { get; private init; }
    }

    public readonly struct None
    {
        static None()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<None>
        {
            public async STask<None> PullAsync(Pullable adapter)
            {
                return default;
            }

            public void Push(None value, ValuePusher pusher)
            {
            }
        }
    }
}