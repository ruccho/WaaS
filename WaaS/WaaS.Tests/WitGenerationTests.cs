using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class WitGenerationTests
{
    [Test]
    public void GeneratesPackageFromInterfaces()
    {
        var packages = WitPackageBuilder.Build(new[] { typeof(ITestTypes), typeof(ITestFunctions) });

        Assert.That(packages.Keys, Is.EqualTo(new[] { "test:host" }));
        Assert.That(packages["test:host"], Is.EqualTo(
            """
            package test:host;

            interface functions {
                use types.{counter, named-value, value-kind};

                describe: func(counter: borrow<counter>, kind: value-kind) -> named-value;
            }

            interface types {
                resource counter {
                    get: func() -> s32;
                }

                record named-value {
                    value-name: string,
                    value: s32,
                }

                enum value-kind {
                    first-kind,
                    second-kind,
                }
            }

            """.ReplaceLineEndings("\n")));
    }

    [Test]
    public async Task GuestBuiltFromGeneratedWitLinksAndRuns()
    {
        // replace the hand-written interfaces of the abi-test guest with the generated ones
        var packages = WitPackageBuilder.Build(new[] { typeof(IAbiHost), typeof(IAbiGuestTypes) });
        var path = GuestBuilder.Build("abi-test", new Dictionary<string, string>
        {
            { "interfaces.wit", packages["waas:abi-test"] }
        });

        var component = Component.Create(File.ReadAllBytes(path));
        var hostInstance = IAbiHost.CreateWaaSInstance(new ComponentAbiTests.AbiHost());
        hostInstance.TryGetExport("counter", out IType? counter);
        hostInstance.TryGetExport("point", out IType? point);
        var instance = component.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "waas:abi-test/host", hostInstance },
            { "counter", counter! },
            { "point", point! }
        });

        using var context = new ExecutionContext();
        var exports = new IAbiTestExports.Wrapper(instance, context);
        Assert.That(await exports.TestResource(), Is.EqualTo(24));
        Assert.That(await exports.TestString("abc"), Is.EqualTo("abc|3"));
        Assert.That(await exports.TestVariant(), Is.EqualTo(9f));
        Assert.That(await exports.TestEnumFlags(), Is.EqualTo("Red:Read;GreenBlue:Read, Write"));
    }
}
