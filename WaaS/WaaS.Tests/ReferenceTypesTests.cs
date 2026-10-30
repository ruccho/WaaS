using System.Runtime.CompilerServices;
using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;
using Global = WaaS.Runtime.Global;

namespace WaaS.Tests;

public class ReferenceTypesTests
{
    private const string Wat = """
                               (module
                                 (type $i2i (func (param i32) (result i32)))
                                 (table $objects (export "objects") 4 externref)
                                 (table $functions 2 funcref)
                                 (global $last (export "last") (mut externref) (ref.null extern))
                                 (func (export "identity") (param externref) (result externref) local.get 0)
                                 (func (export "is-null") (param externref) (result i32) local.get 0 ref.is_null)
                                 (func (export "store") (param i32 externref)
                                   (table.set $objects (local.get 0) (local.get 1))
                                   (global.set $last (local.get 1)))
                                 (func (export "load") (param i32) (result externref) (table.get $objects (local.get 0)))
                                 (func $double (param i32) (result i32) (i32.mul (local.get 0) (i32.const 2)))
                                 (func $square (param i32) (result i32) (i32.mul (local.get 0) (local.get 0)))
                                 (elem declare func $double $square)
                                 (func (export "call") (param i32 i32) (result i32)
                                   (table.set $functions (i32.const 0) (ref.func $double))
                                   (table.set $functions (i32.const 1) (ref.func $square))
                                   (call_indirect $functions (type $i2i) (local.get 1) (local.get 0)))
                               )
                               """;

    private static Instance CreateInstance()
    {
        return new Instance(Module.Create(Utils.GetModuleBinary(Wat)), new Imports());
    }

    private static StackValueItem Invoke(Instance instance, string name, params StackValueItem[] args)
    {
        Assert.That(instance.TryGetExport(name, out IInvocableFunction? function), Is.True);
        using var context = new ExecutionContext();
        context.Invoke(function!, args);
        if (context.ResultLength == 0) return default;
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0];
    }

    [Test]
    public void HostObjectsArePassedAsExternRef()
    {
        var instance = CreateInstance();
        var obj = new object();

        var result = Invoke(instance, "identity", StackValueItem.FromReference(ValueType.ExternRef, obj));
        Assert.That(result.ExpectReference(), Is.SameAs(obj));

        Assert.That(Invoke(instance, "is-null", StackValueItem.FromReference(ValueType.ExternRef, null)).ExpectValueI32(),
            Is.EqualTo(1));
        Assert.That(Invoke(instance, "is-null", StackValueItem.FromReference(ValueType.ExternRef, obj)).ExpectValueI32(),
            Is.EqualTo(0));
    }

    [Test]
    public void TablesAndGlobalsHoldHostObjects()
    {
        var instance = CreateInstance();
        StoreObject(instance, out var reference);

        // the table and the global keep the object alive
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.That(reference.TryGetTarget(out _), Is.True);

        var loaded = Invoke(instance, "load", new StackValueItem(1u)).ExpectReference();
        Assert.That(loaded, Is.InstanceOf<Payload>().With.Property(nameof(Payload.Value)).EqualTo(42));

        Assert.That(instance.TryGetExport("objects", out Table? table), Is.True);
        Assert.That(table!.ElementType, Is.EqualTo(ValueType.ExternRef));
        Assert.That(((Table<object>)table).Elements[1], Is.SameAs(loaded));

        Assert.That(instance.TryGetExport("last", out Global? global), Is.True);
        Assert.That(global!.GetStackValue().ExpectReference(), Is.SameAs(loaded));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreObject(Instance instance, out WeakReference<object> reference)
    {
        var payload = new Payload(42);
        reference = new WeakReference<object>(payload);
        Invoke(instance, "store", new StackValueItem(1u), StackValueItem.FromReference(ValueType.ExternRef, payload));
    }

    [Test]
    public void FunctionReferencesAreCalledThroughTables()
    {
        var instance = CreateInstance();
        Assert.That(Invoke(instance, "call", new StackValueItem(0u), new StackValueItem(7u)).ExpectValueI32(),
            Is.EqualTo(14));
        Assert.That(Invoke(instance, "call", new StackValueItem(1u), new StackValueItem(7u)).ExpectValueI32(),
            Is.EqualTo(49));
        var exception = Assert.Throws<WasmException>(() =>
            Invoke(instance, "call", new StackValueItem(2u), new StackValueItem(7u)));
        Assert.That(exception!.InnerException, Is.InstanceOf<TrapException>());
    }

    [Test]
    public void FuncRefRequiresFunctions()
    {
        Assert.Throws<ArgumentException>(() => StackValueItem.FromReference(ValueType.FuncRef, new object()));
        Assert.Throws<ArgumentException>(() => StackValueItem.FromReference(ValueType.I32, null));
    }

    private sealed record Payload(int Value);
}
