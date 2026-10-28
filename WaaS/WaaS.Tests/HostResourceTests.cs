using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

namespace WaaS.Tests;

[ComponentInterface(@"test:host/types")]
public partial interface ITestTypes
{
    [ComponentResource(@"counter")]
    public partial interface ICounterResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[method]counter.get")]
        ValueTask<int> Get(Borrowed<ICounterResourceImpl> self);
    }

    [ComponentRecord]
    public readonly partial struct NamedValue
    {
        [ComponentField] public string ValueName { get; init; }
        [ComponentField] public int Value { get; init; }
    }

    public enum ValueKind
    {
        FirstKind,
        SecondKind
    }
}

[ComponentInterface(@"test:host/functions")]
public partial interface ITestFunctions
{
    [ComponentApi(@"describe")]
    ValueTask<ITestTypes.NamedValue> Describe(Borrowed<ITestTypes.ICounterResourceImpl> counter,
        ITestTypes.ValueKind kind);
}

public class HostResourceTests
{
    [Test]
    public void DisposingOwnedHandleCallsDestructorOnce()
    {
        var type = new CounterType();
        var owned = type.Wrap(10);

        owned.Dispose();

        Assert.That(type.DroppedValues, Is.EqualTo(new[] { 10 }));
    }

    [Test]
    public void DroppingLentBorrowDoesNotDestroyResource()
    {
        var type = new CounterType();
        var owned = type.Wrap(10);
        var original = owned.GetValue(false);

        var lent = type.LendBorrow(original);
        Assert.That(lent, Is.Not.EqualTo(original));
        Assert.That(type.ToHostResource(lent), Is.EqualTo(10));

        // a component ends the borrow
        type.Drop(lent);

        Assert.That(type.DroppedValues, Is.Empty);
        Assert.That(type.ToHostResource(original), Is.EqualTo(10));

        owned.Dispose();
        Assert.That(type.DroppedValues, Is.EqualTo(new[] { 10 }));
    }

    [Test]
    public void HostResourceTypeBelongsToNoInstance()
    {
        Assert.That(new CounterType().Instance, Is.Null);
    }

    [Test]
    public void GeneratedInstanceExportsValueTypes()
    {
        var instance = ITestTypes.CreateWaaSInstance(new TestTypes());

        Assert.That(instance.TryGetExport("named-value", out IType? record), Is.True);
        var recordType = record as IRecordType;
        Assert.That(recordType, Is.Not.Null);
        Assert.That(recordType!.Fields.ToArray().Select(f => f.Label), Is.EqualTo(new[] { "value-name", "value" }));

        Assert.That(instance.TryGetExport("value-kind", out IType? @enum), Is.True);
        Assert.That(@enum, Is.InstanceOf<IEnumType>());
        Assert.That(((IEnumType)@enum!).Labels.ToArray(), Is.EqualTo(new[] { "first-kind", "second-kind" }));

        Assert.That(instance.TryGetExport("counter", out IResourceType? resource), Is.True);
        Assert.That(resource, Is.SameAs(TestTypes.Counter));
    }

    [Test]
    public void GeneratedInstanceExportsReferencedTypes()
    {
        // types declared in another interface and referenced by functions are exported as well,
        // because components import them through `use`
        var instance = ITestFunctions.CreateWaaSInstance(new TestFunctions());

        Assert.That(instance.TryGetExport("counter", out IResourceType? resource), Is.True);
        Assert.That(resource, Is.SameAs(TestTypes.Counter));
        Assert.That(instance.TryGetExport("named-value", out IType? record), Is.True);
        Assert.That(record, Is.InstanceOf<IRecordType>());
        Assert.That(instance.TryGetExport("value-kind", out IType? @enum), Is.True);
        Assert.That(@enum, Is.InstanceOf<IEnumType>());
    }

    private class CounterType : HostResourceTypeBase<int>, ITestTypes.ICounterResourceImpl
    {
        public List<int> DroppedValues { get; } = new();
        public IResourceType Type => this;

        public ValueTask<int> Get(Borrowed<ITestTypes.ICounterResourceImpl> self)
        {
            return new ValueTask<int>(Unwrap(self));
        }

        protected override void OnDrop(int value)
        {
            DroppedValues.Add(value);
        }
    }

    private class TestTypes : ITestTypes
    {
        public static readonly CounterType Counter = new();
        ITestTypes.ICounterResourceImpl ITestTypes.Counter => Counter;
    }

    private class TestFunctions : ITestFunctions
    {
        public ITestTypes.ICounterResourceImpl Counter => TestTypes.Counter;

        public ValueTask<ITestTypes.NamedValue> Describe(Borrowed<ITestTypes.ICounterResourceImpl> counter,
            ITestTypes.ValueKind kind)
        {
            return new ValueTask<ITestTypes.NamedValue>(new ITestTypes.NamedValue
                { ValueName = kind.ToString(), Value = 0 });
        }
    }
}
