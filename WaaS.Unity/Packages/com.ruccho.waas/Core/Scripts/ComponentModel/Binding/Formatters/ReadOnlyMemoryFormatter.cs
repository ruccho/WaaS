#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    internal class ReadOnlyMemoryFormatter<T> : IFormatter<ReadOnlyMemory<T>>
    {
        public async STask<ReadOnlyMemory<T>> PullAsync(Pullable adapter)
        {
            var prelude = await adapter.PullPrimitiveValueAsync<ListPrelude>();
            var result = new T[prelude.Length];
            for (var i = 0; i < prelude.Length; i++) result[i] = await prelude.ElementPullable.PullValueAsync<T>();
            // Logger.Log($"pulled: {Thread.CurrentThread.IsThreadPoolThread}");
            return result;
        }

        public void Push(ReadOnlyMemory<T> value, ValuePusher pusher)
        {
            using var listPusher = pusher.PushList(value.Length);
            var formatter = FormatterProvider.GetFormatter<T>();
            foreach (var item in value.Span) formatter.Push(item, listPusher);
        }
    }

    internal class ReadOnlyMemoryFormatterProvider : IProceduralFormatterProvider
    {
        public bool TryCreateFormatter<T>([NotNullWhen(true)] out IFormatter<T>? formatter)
        {
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
            {
                var elementType = typeof(T).GetGenericArguments()[0];
                var formatterType = typeof(ReadOnlyMemoryFormatter<>).MakeGenericType(elementType);
                formatter = (IFormatter<T>)Activator.CreateInstance(formatterType);
                return formatter != null;
            }

            formatter = default;
            return false;
        }
    }
    // TODO: other collections
}