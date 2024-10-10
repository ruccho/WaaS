

#nullable enable

using System;
using System.Threading.Tasks;
using WaaS.ComponentModel.Runtime;
using WaaS.ComponentModel.Models;

namespace WaaS.ComponentModel.Binding
{


    public class TupleFormatter<T1> : IFormatter<Tuple<T1?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type });

        public async ValueTask<Tuple<T1?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?>(await prelude.BodyPullable.PullValueAsync<T1>());
        }

        public void Push(Tuple<T1?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1> : IFormatter<ValueTuple<T1?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type });

        public async ValueTask<ValueTuple<T1?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?>(await prelude.BodyPullable.PullValueAsync<T1>());
        }

        public void Push(ValueTuple<T1?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2> : IFormatter<Tuple<T1?, T2?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type });

        public async ValueTask<Tuple<T1?, T2?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>());
        }

        public void Push(Tuple<T1?, T2?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2> : IFormatter<ValueTuple<T1?, T2?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type });

        public async ValueTask<ValueTuple<T1?, T2?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>());
        }

        public void Push(ValueTuple<T1?, T2?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3> : IFormatter<Tuple<T1?, T2?, T3?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>());
        }

        public void Push(Tuple<T1?, T2?, T3?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3> : IFormatter<ValueTuple<T1?, T2?, T3?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3, T4> : IFormatter<Tuple<T1?, T2?, T3?, T4?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?, T4?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?, T4?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>());
        }

        public void Push(Tuple<T1?, T2?, T3?, T4?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3, T4> : IFormatter<ValueTuple<T1?, T2?, T3?, T4?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?, T4?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?, T4?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?, T4?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3, T4, T5> : IFormatter<Tuple<T1?, T2?, T3?, T4?, T5?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?, T4?, T5?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?, T4?, T5?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>());
        }

        public void Push(Tuple<T1?, T2?, T3?, T4?, T5?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3, T4, T5> : IFormatter<ValueTuple<T1?, T2?, T3?, T4?, T5?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?, T4?, T5?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?, T4?, T5?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?, T4?, T5?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3, T4, T5, T6> : IFormatter<Tuple<T1?, T2?, T3?, T4?, T5?, T6?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?, T4?, T5?, T6?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?, T4?, T5?, T6?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>());
        }

        public void Push(Tuple<T1?, T2?, T3?, T4?, T5?, T6?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3, T4, T5, T6> : IFormatter<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3, T4, T5, T6, T7> : IFormatter<Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type, FormatterProvider.GetFormatter<T7>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>(), await prelude.BodyPullable.PullValueAsync<T7>());
        }

        public void Push(Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
            FormatterProvider.GetFormatter<T7>().Push(value.Item7, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7> : IFormatter<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>>
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type, FormatterProvider.GetFormatter<T7>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>(), await prelude.BodyPullable.PullValueAsync<T7>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
            FormatterProvider.GetFormatter<T7>().Push(value.Item7, tuplePusher);
        }
    }


    public class TupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : IFormatter<Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>>
        where TRest : notnull
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type, FormatterProvider.GetFormatter<T7>().Type, FormatterProvider.GetFormatter<TRest>().Type });

        public async ValueTask<Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>(), await prelude.BodyPullable.PullValueAsync<T7>(), await prelude.BodyPullable.PullValueAsync<TRest>());
        }

        public void Push(Tuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
            FormatterProvider.GetFormatter<T7>().Push(value.Item7, tuplePusher);
            FormatterProvider.GetFormatter<TRest>().Push(value.Rest, tuplePusher);
        }
    }

    public class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : IFormatter<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>>
        where TRest : struct
    {
        public IValueType Type { get; } = new ResolverTupleType(new IValueType[] { FormatterProvider.GetFormatter<T1>().Type, FormatterProvider.GetFormatter<T2>().Type, FormatterProvider.GetFormatter<T3>().Type, FormatterProvider.GetFormatter<T4>().Type, FormatterProvider.GetFormatter<T5>().Type, FormatterProvider.GetFormatter<T6>().Type, FormatterProvider.GetFormatter<T7>().Type, FormatterProvider.GetFormatter<TRest>().Type });

        public async ValueTask<ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>> PullAsync(Pullable pullable)
        {
            var prelude = await pullable.PullPrimitiveValueAsync<RecordPrelude>();
            return new ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest>(await prelude.BodyPullable.PullValueAsync<T1>(), await prelude.BodyPullable.PullValueAsync<T2>(), await prelude.BodyPullable.PullValueAsync<T3>(), await prelude.BodyPullable.PullValueAsync<T4>(), await prelude.BodyPullable.PullValueAsync<T5>(), await prelude.BodyPullable.PullValueAsync<T6>(), await prelude.BodyPullable.PullValueAsync<T7>(), await prelude.BodyPullable.PullValueAsync<TRest>());
        }

        public void Push(ValueTuple<T1?, T2?, T3?, T4?, T5?, T6?, T7?, TRest> value, ValuePusher pusher)
        {
            using var tuplePusher = pusher.PushRecord();
            FormatterProvider.GetFormatter<T1>().Push(value.Item1, tuplePusher);
            FormatterProvider.GetFormatter<T2>().Push(value.Item2, tuplePusher);
            FormatterProvider.GetFormatter<T3>().Push(value.Item3, tuplePusher);
            FormatterProvider.GetFormatter<T4>().Push(value.Item4, tuplePusher);
            FormatterProvider.GetFormatter<T5>().Push(value.Item5, tuplePusher);
            FormatterProvider.GetFormatter<T6>().Push(value.Item6, tuplePusher);
            FormatterProvider.GetFormatter<T7>().Push(value.Item7, tuplePusher);
            FormatterProvider.GetFormatter<TRest>().Push(value.Rest, tuplePusher);
        }
    }

}