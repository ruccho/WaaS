using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;

namespace WaaS.Tests;

[ComponentInterface(@"test:validation/host")]
public partial interface IValidationHost
{
    [ComponentResource(@"thing")]
    public partial interface IThingResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[method]thing.get")]
        ValueTask<int> Get(Borrowed<IThingResourceImpl> self);
    }

    [ComponentResource(@"other")]
    public partial interface IOtherResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[method]other.get")]
        ValueTask<int> Get(Borrowed<IOtherResourceImpl> self);
    }

    [ComponentRecord]
    public readonly partial struct Point
    {
        [ComponentField] public int X { get; init; }
        [ComponentField] public int Y { get; init; }
    }

    [ComponentApi(@"add")]
    ValueTask<int> Add(int a, int b);

    [ComponentApi(@"length")]
    ValueTask<int> Length(Point p);

    [ComponentApi(@"make-thing")]
    ValueTask<Owned<IThingResourceImpl>> MakeThing();
}

/// <summary>
///     Tests of the validation of the imports when instantiating a component.
/// </summary>
public class ImportValidationTests
{
    private const string HostName = "test:validation/host";

    private static IInstance CreateHostInstance()
    {
        return IValidationHost.CreateWaaSInstance(new ValidationHost());
    }

    private static string ImportHost(string instanceType)
    {
        return $$"""
                 (component
                   (type $host (instance {{instanceType}}))
                   (import "{{HostName}}" (instance (type $host)))
                 )
                 """;
    }

    private static LinkException InstantiateWithError(string wat, Dictionary<string, ISortedExportable> imports)
    {
        var component = Utils.GetComponent(wat);
        var exception = Assert.Throws<LinkException>(() => component.Instantiate(imports))!;
        TestContext.Out.WriteLine(exception.Message);
        return exception;
    }

    private static LinkException InstantiateHostWithError(string instanceType)
    {
        return InstantiateWithError(ImportHost(instanceType),
            new Dictionary<string, ISortedExportable> { { HostName, CreateHostInstance() } });
    }

    private static void InstantiateHost(string instanceType)
    {
        Utils.GetComponent(ImportHost(instanceType)).Instantiate(new Dictionary<string, ISortedExportable>
        {
            { HostName, CreateHostInstance() }
        });
    }

    private const string ThingTypes = """
                                      (export $thing "thing" (type (sub resource)))
                                      (export $other "other" (type (sub resource)))
                                      """;

    [Test]
    public void MatchingInstanceIsAccepted()
    {
        InstantiateHost($$"""
                          {{ThingTypes}}
                          (type $point (record (field "x" s32) (field "y" s32)))
                          (export $point-export "point" (type (eq $point)))
                          (export "add" (func (param "a" s32) (param "b" s32) (result s32)))
                          (export "length" (func (param "p" $point-export) (result s32)))
                          (export "make-thing" (func (result (own $thing))))
                          """);
    }

    [Test]
    public void InstanceWithMoreExportsIsAccepted()
    {
        InstantiateHost("""
                        (export "add" (func (param "a" s32) (param "b" s32) (result s32)))
                        """);
    }

    [Test]
    public void ParameterNamesAreNotCompared()
    {
        InstantiateHost("""
                        (export "add" (func (param "lhs" s32) (param "rhs" s32) (result s32)))
                        """);
    }

    [Test]
    public void MismatchedParameterTypeIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (export "add" (func (param "a" s32) (param "b" s64) (result s32)))
                                                 """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/add: function type does not match"));
        Assert.That(exception.Message, Does.Contain("parameter 'b': expected s64, provided s32"));
        Assert.That(exception.Message, Does.Contain("expected: func(a: s32, b: s64) -> s32"));
        Assert.That(exception.Message, Does.Contain("provided: func(a: s32, b: s32) -> s32"));
    }

    [Test]
    public void MismatchedResultTypeIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (export "add" (func (param "a" s32) (param "b" s32)))
                                                 """);
        Assert.That(exception.Message, Does.Contain("result: expected (none), provided s32"));
    }

    [Test]
    public void MismatchedParameterCountIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (export "add" (func (param "a" s32) (result s32)))
                                                 """);
        Assert.That(exception.Message, Does.Contain("expected 1 parameters, provided 2"));
    }

    [Test]
    public void MissingExportIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (export "sub" (func (param "a" s32) (param "b" s32) (result s32)))
                                                 """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/sub: function is missing"));
    }

    [Test]
    public void MismatchedSortIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (export "add" (type (sub resource)))
                                                 """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/add: expected type, provided function"));
    }

    [Test]
    public void ReorderedRecordFieldsAreReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (type $point (record (field "y" s32) (field "x" s32)))
                                                 (export $point-export "point" (type (eq $point)))
                                                 (export "length" (func (param "p" $point-export) (result s32)))
                                                 """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/point: type does not match: field 0: expected 'y', provided 'x'"));
        // the function refers to the type export, which is bound to the type provided by the host
        Assert.That(exception.Message, Does.Not.Contain($"{HostName}/length"));
    }

    [Test]
    public void MismatchedRecordFieldTypeIsReported()
    {
        var exception = InstantiateHostWithError("""
                                                 (type $point (record (field "x" s32) (field "y" f32)))
                                                 (export "length" (func (param "p" $point) (result s32)))
                                                 """);
        Assert.That(exception.Message,
            Does.Contain("parameter 'p': field 'y': expected f32, provided s32"));
    }

    [Test]
    public void DifferentResourceTypeIsReported()
    {
        var exception = InstantiateHostWithError($$"""
                                                   {{ThingTypes}}
                                                   (export "make-thing" (func (result (own $other))))
                                                   """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/make-thing: function type does not match: result: own:"));
        Assert.That(exception.Message, Does.Contain("OtherType"));
        Assert.That(exception.Message, Does.Contain("ThingType"));
    }

    [Test]
    public void MissingResourceTypeIsReported()
    {
        // the host instance does not export "missing", which the function refers to
        var exception = InstantiateHostWithError("""
                                                 (export $missing "missing" (type (sub resource)))
                                                 (export "make-thing" (func (result (own $missing))))
                                                 """);
        Assert.That(exception.Message, Does.Contain($"{HostName}/missing: type is missing"));
        Assert.That(exception.Message, Does.Contain($"{HostName}/make-thing: failed to resolve the expected type"));
    }

    [Test]
    public void MissingInstanceIsReported()
    {
        var exception = InstantiateWithError(ImportHost("""
                                                        (export "add" (func (param "a" s32) (param "b" s32) (result s32)))
                                                        """), new Dictionary<string, ISortedExportable>());
        Assert.That(exception.Message, Does.Contain($"{HostName}: instance is missing"));
    }

    [Test]
    public void InstanceOfWrongSortIsReported()
    {
        CreateHostInstance().TryGetExport("add", out IFunction? add);
        var exception = InstantiateWithError(ImportHost("""
                                                        (export "add" (func (param "a" s32) (param "b" s32) (result s32)))
                                                        """),
            new Dictionary<string, ISortedExportable> { { HostName, add! } });
        Assert.That(exception.Message, Does.Contain($"{HostName}: expected instance, provided function"));
    }

    private const string AdderComponent = """
                                          (component
                                            (core module $A (func (export "add") (param i32 i32) (result i32) (i32.add (local.get 0) (local.get 1))))
                                            (core instance $a (instantiate $A))
                                            (alias core export $a "add" (core func $f))
                                            (type $t (func (param "lhs" s32) (param "rhs" s32) (result s32)))
                                            (func $l (type $t) (canon lift (core func $f)))
                                            (export "add" (func $l))
                                          )
                                          """;

    private const string ImportAdderComponent = """
                                                (component
                                                  (import "adder" (component
                                                    (export "add" (func (param "lhs" s32) (param "rhs" s32) (result s32)))
                                                  ))
                                                )
                                                """;

    [Test]
    public void MatchingComponentIsAccepted()
    {
        Utils.GetComponent(ImportAdderComponent).Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "adder", Utils.GetComponent(AdderComponent) }
        });
    }

    [Test]
    public void ComponentWithoutExpectedExportIsReported()
    {
        var exception = InstantiateWithError(ImportAdderComponent, new Dictionary<string, ISortedExportable>
        {
            { "adder", Utils.GetComponent("(component)") }
        });
        Assert.That(exception.Message, Does.Contain("adder: component does not match: export 'add' is missing"));
    }

    [Test]
    public void ComponentWithUnexpectedImportIsReported()
    {
        var exception = InstantiateWithError(ImportAdderComponent, new Dictionary<string, ISortedExportable>
        {
            {
                "adder", Utils.GetComponent("""
                                            (component
                                              (import "f" (func))
                                              (core module $A (func (export "add") (param i32 i32) (result i32) (i32.add (local.get 0) (local.get 1))))
                                              (core instance $a (instantiate $A))
                                              (alias core export $a "add" (core func $f))
                                              (type $t (func (param "lhs" s32) (param "rhs" s32) (result s32)))
                                              (func $l (type $t) (canon lift (core func $f)))
                                              (export "add" (func $l))
                                            )
                                            """)
            }
        });
        Assert.That(exception.Message,
            Does.Contain("adder: component does not match: import 'f' (function) is not expected to be imported"));
    }

    [Test]
    public void ExportOfAnotherComponentIsValidated()
    {
        var adder = Utils.GetComponent(AdderComponent).Instantiate(new Dictionary<string, ISortedExportable>());
        adder.TryGetExport("add", out IFunction? add);

        const string wat = """
                           (component
                             (import "add" (func (param "lhs" s32) (param "rhs" s64) (result s32)))
                           )
                           """;
        var exception = InstantiateWithError(wat, new Dictionary<string, ISortedExportable> { { "add", add! } });
        Assert.That(exception.Message, Does.Contain("add: function type does not match: parameter 'rhs': expected s64, provided s32"));

        Utils.GetComponent("""
                           (component
                             (import "add" (func (param "lhs" s32) (param "rhs" s32) (result s32)))
                           )
                           """).Instantiate(new Dictionary<string, ISortedExportable> { { "add", add! } });
    }

    private class ThingType : HostResourceTypeBase<int>, IValidationHost.IThingResourceImpl
    {
        public IResourceType Type => this;

        public ValueTask<int> Get(Borrowed<IValidationHost.IThingResourceImpl> self)
        {
            return new ValueTask<int>(Unwrap(self));
        }
    }

    private class OtherType : HostResourceTypeBase<int>, IValidationHost.IOtherResourceImpl
    {
        public IResourceType Type => this;

        public ValueTask<int> Get(Borrowed<IValidationHost.IOtherResourceImpl> self)
        {
            return new ValueTask<int>(Unwrap(self));
        }
    }

    private class ValidationHost : IValidationHost
    {
        private readonly ThingType thing = new();

        public IValidationHost.IThingResourceImpl Thing => thing;
        public IValidationHost.IOtherResourceImpl Other { get; } = new OtherType();

        public ValueTask<int> Add(int a, int b)
        {
            return new ValueTask<int>(a + b);
        }

        public ValueTask<int> Length(IValidationHost.Point p)
        {
            return new ValueTask<int>(p.X + p.Y);
        }

        public ValueTask<Owned<IValidationHost.IThingResourceImpl>> MakeThing()
        {
            return new ValueTask<Owned<IValidationHost.IThingResourceImpl>>(
                (Owned<IValidationHost.IThingResourceImpl>)thing.Wrap(1));
        }
    }
}
