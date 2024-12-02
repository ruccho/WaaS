using System.Diagnostics.CodeAnalysis;
using STask;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;
using ExternalFunctionDelegate = WaaS.ComponentModel.Binding.ExternalFunctionDelegate;

namespace WaaS.Tests.Wast;

public static class SpecTest
{
    public static Imports CreateImports()
    {
        var imports = new Imports();
        var binder = CoreBinder.Instance;

        imports["spectest"] = new ModuleExports
        {
            { "print", binder.ToExternalFunction(() => { Console.WriteLine("<spectest> print"); }) },
            {
                "print_i32",
                binder.ToExternalFunction((uint a) => { Console.WriteLine($"<spectest> print_i32: {a}"); })
            },
            {
                "print_i64",
                binder.ToExternalFunction((ulong a) => { Console.WriteLine($"<spectest> print_i64: {a}"); })
            },
            {
                "print_f32",
                binder.ToExternalFunction((float a) => { Console.WriteLine($"<spectest> print_f32: {a}"); })
            },
            {
                "print_f64",
                binder.ToExternalFunction(
                    (double a) => { Console.WriteLine($"<spectest> print_f64: {a}"); })
            },
            {
                "print_i32_f32",
                binder.ToExternalFunction((uint a, float b) =>
                {
                    Console.WriteLine($"<spectest> print_i32_f32: {a}, {b}");
                })
            },
            {
                "print_f64_f64",
                binder.ToExternalFunction((double a, double b) =>
                {
                    Console.WriteLine($"<spectest> print_f64_f64: {a}, {b}");
                })
            },
            { "global_i32", new Global<uint>(666) },
            { "global_i64", new Global<ulong>(0) },
            { "global_f32", new Global<float>(0) },
            { "global_f64", new Global<double>(0) },
            { "table", new Table<IInvocableFunction>(new Limits(10, 20)) },
            { "memory", new Memory(new Limits(1, 2)) }
        };

        return imports;
    }

    public static Dictionary<string, ISortedExportable> CreateComponentImports()
    {
        return new Dictionary<string, ISortedExportable>()
        {
            {
                "host-return-two",
                new ExternalFunctionDelegate(
                    new ResolvedFunctionType(ReadOnlyMemory<IParameter>.Empty,
                        PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)), InvokeAsync)
            },
            {
                "host",
                new Host()
            }
        };

        async STaskVoid InvokeAsync(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            await framemove;
            var pusher = await resultpusher;
            pusher.Push((uint)2);
        }
    }

    private class Nested : IInstance
    {

        public bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? result) where T : ISortedExportable
        {
            ISortedExportable? exportable = name switch
            {
                "return-four" =>
                new ExternalFunctionDelegate(
                    new ResolvedFunctionType(ReadOnlyMemory<IParameter>.Empty,
                        PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)), ReturnFour),
                _ => null
            };

            result = (T)exportable!;
            return exportable != null;
        }
        async STaskVoid ReturnFour(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            await framemove;
            var pusher = await resultpusher;
            pusher.Push((uint)4);
        }
    }

    public class Host : IInstance
    {
        private readonly HostResourceType resource1 = new HostResourceType();
        private readonly HostResourceType resource2 = new HostResourceType();
        private readonly Nested nested = new Nested();

        public bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? result) where T : ISortedExportable
        {
            ISortedExportable? exportable = name switch
            {
                "resource1" => resource1,
                "resource2" => resource2,
                "resource1-again" => resource1,
                "[constructor]resource1" => new ExternalFunctionDelegate(
                    new ResolvedFunctionType(
                        new IParameter[]
                            { new ResolvedParameter("r", PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)) },
                        new ResolvedOwnedType(resource1)),
                    Resource1Constructor),
                "[static]resource1.assert" => new ExternalFunctionDelegate(
                    new ResolvedFunctionType(
                        new IParameter[]
                        {
                            new ResolvedParameter("r", new ResolvedOwnedType(resource1)),
                            new ResolvedParameter("rep", PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32))
                        }, null),
                    Resource1Assert),
                "[static]resource1.last-drop" => new ExternalFunctionDelegate(
                    new ResolvedFunctionType(Array.Empty<IParameter>(),
                        PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)), Resource1LastDrop),
                "[static]resource1.drops" => new ExternalFunctionDelegate(
                    new ResolvedFunctionType(Array.Empty<IParameter>(),
                        PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)), Resource1Drops),
                "[method]resource1.simple" => new ExternalFunctionDelegate(new ResolvedFunctionType(
                    new IParameter[]
                    {
                        new ResolvedParameter("self", new ResolvedBorrowedType(resource1)),
                        new ResolvedParameter("rep", PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)),
                    },
                    null), Resource1Simple),
                "[method]resource1.take-borrow" => new ExternalFunctionDelegate(new ResolvedFunctionType(
                    new IParameter[]
                    {
                        new ResolvedParameter("self", new ResolvedBorrowedType(resource1)),
                        new ResolvedParameter("b", new ResolvedBorrowedType(resource1)),
                    },
                    null), Resource1TakeBorrow),
                "[method]resource1.take-own" => new ExternalFunctionDelegate(new ResolvedFunctionType(
                    new IParameter[]
                    {
                        new ResolvedParameter("self", new ResolvedBorrowedType(resource1)),
                        new ResolvedParameter("b", new ResolvedOwnedType(resource1)),
                    },
                    null), Resource1TakeOwn),
                "return-three" =>
                    new ExternalFunctionDelegate(
                        new ResolvedFunctionType(ReadOnlyMemory<IParameter>.Empty,
                            PrimitiveValueType.GetBoxed(PrimitiveValueTypeKind.U32)), ReturnThree),
                "nested" => nested,
                _ => null
            };

            result = (T)exportable!;
            return exportable != null;
        }

        async STaskVoid ReturnThree(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            await framemove;
            var pusher = await resultpusher;
            pusher.Push((uint)3);
        }

        async STaskVoid Resource1TakeOwn(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            var self = await arguments.PullValueAsync<Borrowed>();
            var b = await arguments.PullValueAsync<Owned>();
            await framemove;
            resource1.Unwrap(self);
            resource1.Unwrap(b);
            await resultpusher;
        }

        async STaskVoid Resource1TakeBorrow(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            var self = await arguments.PullValueAsync<Borrowed>();
            var b = await arguments.PullValueAsync<Borrowed>();
            await framemove;
            resource1.Unwrap(self);
            resource1.Unwrap(b);
            await resultpusher;
        }

        async STaskVoid Resource1Simple(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            var handle = await arguments.PullValueAsync<Borrowed>();
            var rep = await arguments.PullValueAsync<uint>();
            await framemove;
            var type = handle.Type as HostResourceType ?? throw new TrapException();
            if (rep != type.Unwrap(handle)) throw new TrapException();
            await resultpusher;
        }

        async STaskVoid Resource1Drops(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            await framemove;
            var pusher = await resultpusher;
            pusher.Push(resource1.Drops);
        }

        async STaskVoid Resource1LastDrop(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            await framemove;
            var pusher = await resultpusher;
            pusher.Push(resource1.LastDrop);
        }

        async STaskVoid Resource1Constructor(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            var rep = await arguments.PullPrimitiveValueAsync<uint>();
            await framemove;
            var pusher = await resultpusher;
            pusher.PushOwned(resource1.Wrap(rep));
        }

        async STaskVoid Resource1Assert(ExecutionContext context, Pullable arguments, STaskVoid framemove,
            STask<ValuePusher> resultpusher)
        {
            var r = await arguments.PullValueAsync<Owned>();
            var rep = await arguments.PullPrimitiveValueAsync<uint>();
            await framemove;
            if ((uint)resource1.Unwrap(r) != rep) throw new TrapException();
            await resultpusher;
        }

        public void Reset()
        {
            resource1.Reset();
            resource2.Reset();
        }
    }

    private class HostResourceType : HostResourceTypeBase<uint>
    {
        public uint Drops { get; private set; } = 0;
        public uint LastDrop { get; private set; } = 0;

        protected override void OnDrop(uint value)
        {
            Drops++;
            LastDrop = value;
        }
    }
}