#nullable enable

using STask;
using WaaS.ComponentModel.Runtime;

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます

namespace WaaS.ComponentModel.Binding
{
    [ComponentVariant]
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
            public IValueType? Type => null;

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