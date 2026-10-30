using System.Globalization;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

// C# definitions corresponding to Guests/abi-extended/wit/abi-extended.wit

[ComponentInterface(@"waas:abi-extended/values")]
public partial interface IExtValues
{
    [ComponentRecord]
    public readonly partial struct Inner
    {
        [ComponentField] public byte A { get; init; }
        [ComponentField] public short? B { get; init; }
        [ComponentField] public ReadOnlyMemory<string> C { get; init; }
    }

    [ComponentVariant]
    public readonly partial struct Tree
    {
        [ComponentCase] public sbyte? Leaf { get; init; }
        [ComponentCase] public Inner? Node { get; init; }
        [ComponentCase] public (ushort, double)? Pair { get; init; }
        [ComponentCase] public string? Text { get; init; }
        [ComponentCase] public None? Empty { get; init; }
    }

    [ComponentRecord]
    public readonly partial struct Outer
    {
        [ComponentField] public string Name { get; init; }
        [ComponentField] public Inner Inner { get; init; }
        [ComponentField] public ReadOnlyMemory<Inner> Items { get; init; }
        [ComponentField] public Tree Tree { get; init; }
        [ComponentField] public Result<uint, string>? Maybe { get; init; }
        [ComponentField] public ReadOnlyMemory<ReadOnlyMemory<long>> Nested { get; init; }
    }

    public enum Level
    {
        Low,
        Mid,
        High
    }

    [Flags]
    public enum SmallFlags : byte
    {
        F0 = 1 << 0,
        F1 = 1 << 1,
        F2 = 1 << 2
    }

    [Flags]
    public enum MediumFlags : ushort
    {
        F0 = 1 << 0,
        F1 = 1 << 1,
        F2 = 1 << 2,
        F3 = 1 << 3,
        F4 = 1 << 4,
        F5 = 1 << 5,
        F6 = 1 << 6,
        F7 = 1 << 7,
        F8 = 1 << 8,
        F9 = 1 << 9,
        F10 = 1 << 10,
        F11 = 1 << 11
    }

    [Flags]
    public enum LargeFlags : uint
    {
        F0 = 1u << 0,
        F1 = 1u << 1,
        F2 = 1u << 2,
        F3 = 1u << 3,
        F4 = 1u << 4,
        F5 = 1u << 5,
        F6 = 1u << 6,
        F7 = 1u << 7,
        F8 = 1u << 8,
        F9 = 1u << 9,
        F10 = 1u << 10,
        F11 = 1u << 11,
        F12 = 1u << 12,
        F13 = 1u << 13,
        F14 = 1u << 14,
        F15 = 1u << 15,
        F16 = 1u << 16,
        F17 = 1u << 17,
        F18 = 1u << 18,
        F19 = 1u << 19,
        F20 = 1u << 20,
        F21 = 1u << 21,
        F22 = 1u << 22,
        F23 = 1u << 23,
        F24 = 1u << 24,
        F25 = 1u << 25,
        F26 = 1u << 26,
        F27 = 1u << 27,
        F28 = 1u << 28,
        F29 = 1u << 29,
        F30 = 1u << 30,
        F31 = 1u << 31
    }

    [ComponentApi(@"integers")]
    ValueTask<(byte, sbyte, ushort, short, uint, int, ulong, long, bool)> Integers(byte a, sbyte b, ushort c,
        short d, uint e, int f, ulong g, long h, bool i);

    [ComponentApi(@"small-integers")]
    ValueTask<(sbyte, short, byte, ushort)> SmallIntegers(sbyte a, short b, byte c, ushort d);

    [ComponentApi(@"floats")]
    ValueTask<(float, double)> Floats(float a, double b);

    [ComponentApi(@"chars")]
    ValueTask<ComponentChar> Chars(ComponentChar value);

    [ComponentApi(@"text")]
    ValueTask<string> Text(string value);

    [ComponentApi(@"texts")]
    ValueTask<ReadOnlyMemory<string>> Texts(ReadOnlyMemory<string> values);

    [ComponentApi(@"bytes")]
    ValueTask<ReadOnlyMemory<byte>> Bytes(ReadOnlyMemory<byte> values);

    [ComponentApi(@"shorts")]
    ValueTask<ReadOnlyMemory<short>> Shorts(ReadOnlyMemory<short> values);

    [ComponentApi(@"outer-value")]
    ValueTask<Outer> OuterValue(Outer value);

    [ComponentApi(@"trees")]
    ValueTask<ReadOnlyMemory<Tree>> Trees(ReadOnlyMemory<Tree> values);

    [ComponentApi(@"flags-value")]
    ValueTask<(SmallFlags, MediumFlags, LargeFlags)> FlagsValue(SmallFlags a, MediumFlags b, LargeFlags c);

    [ComponentApi(@"levels")]
    ValueTask<ReadOnlyMemory<Level>> Levels(ReadOnlyMemory<Level> values);

    [ComponentApi(@"options")]
    ValueTask<(byte?, Option<string>, Option<int?>)> Options(byte? a, Option<string> b, Option<int?> c);

    [ComponentApi(@"results")]
    ValueTask<(Result<sbyte, ulong>, Result<None, string>, Result<string, None>, Result<None, None>)> Results(
        Result<sbyte, ulong> a, Result<None, string> b, Result<string, None> c, Result<None, None> d);

    [ComponentApi(@"many-params")]
    ValueTask<string> ManyParams(int p0, long p1, float p2, double p3, byte p4, sbyte p5, ushort p6, short p7,
        string p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16, double p17);

    [ComponentApi(@"long-tuple")]
    ValueTask<(int, int, int, int, int, int, int, byte, long, string)> LongTuple(
        (int, int, int, int, int, int, int, byte, long, string) value);
}

[ComponentInterface(@"waas:abi-extended/resources")]
public partial interface IExtResources
{
    [ComponentResource(@"blob")]
    public partial interface IBlobResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[method]blob.value")]
        ValueTask<int> Value(Borrowed<IBlobResourceImpl> self);
    }

    [ComponentRecord]
    public readonly partial struct Holder
    {
        [ComponentField] public string Name { get; init; }
        [ComponentField] public Owned<IBlobResourceImpl> Blob { get; init; }
        [ComponentField] public Owned<IBlobResourceImpl>? Extra { get; init; }
    }

    [ComponentRecord]
    public readonly partial struct Borrowing
    {
        [ComponentField] public Borrowed<IBlobResourceImpl> Blob { get; init; }
        [ComponentField] public int Factor { get; init; }
    }

    [ComponentApi(@"make-blob")]
    ValueTask<Owned<IBlobResourceImpl>> MakeBlob(int value);

    [ComponentApi(@"make-blobs")]
    ValueTask<ReadOnlyMemory<Owned<IBlobResourceImpl>>> MakeBlobs(ReadOnlyMemory<int> values);

    [ComponentApi(@"take-blobs")]
    ValueTask<int> TakeBlobs(ReadOnlyMemory<Owned<IBlobResourceImpl>> blobs);

    [ComponentApi(@"sum-borrowed")]
    ValueTask<int> SumBorrowed(ReadOnlyMemory<Borrowed<IBlobResourceImpl>> blobs);

    [ComponentApi(@"swap-holder")]
    ValueTask<Holder> SwapHolder(Holder value);

    [ComponentApi(@"multiply")]
    ValueTask<int> Multiply(Borrowing value);
}

[ComponentInterface(@"waas:abi-extended/abi-extended")]
public partial interface IExtExports
{
    [ComponentApi(@"last-call")]
    ValueTask<string> LastCall();

    [ComponentApi(@"test-values")]
    ValueTask<ReadOnlyMemory<string>> TestValues();

    [ComponentApi(@"test-resources")]
    ValueTask<string> TestResources();

    [ComponentApi(@"sum-owned")]
    ValueTask<int> SumOwned(ReadOnlyMemory<Owned<IExtResources.IBlobResourceImpl>> blobs);
}

/// <summary>
///     Tests of the Canonical ABI in both directions (host to guest and guest to host)
///     using a guest component built with wit-bindgen.
/// </summary>
public class ComponentAbiExtendedTests
{
    private static IComponent? component;

    private ValuesHost valuesHost = null!;
    private ResourcesHost resourcesHost = null!;
    private IInstance instance = null!;

    [OneTimeSetUp]
    public void BuildGuest()
    {
        component ??= Component.Create(File.ReadAllBytes(GuestBuilder.Build("abi-extended")));
    }

    [SetUp]
    public void Instantiate()
    {
        valuesHost = new ValuesHost();
        resourcesHost = new ResourcesHost();
        var resourcesInstance = IExtResources.CreateWaaSInstance(resourcesHost);
        resourcesInstance.TryGetExport("blob", out IType? blob);

        instance = component!.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "waas:abi-extended/values", IExtValues.CreateWaaSInstance(valuesHost) },
            { "waas:abi-extended/resources", resourcesInstance },
            { "blob", blob! }
        });
    }

    private async ValueTask<T> CallExport<T>(Func<IExtValues, ValueTask<T>> call)
    {
        using var context = new ExecutionContext();
        Assert.That(instance.TryGetExport("waas:abi-extended/values", out IInstance? values), Is.True);
        return await call(new IExtValues.Wrapper(values!, context));
    }

    private async ValueTask<T> Call<T>(Func<IExtExports, ValueTask<T>> call)
    {
        using var context = new ExecutionContext();
        return await call(new IExtExports.Wrapper(instance, context));
    }

    private ValueTask<string> LastCall()
    {
        return Call(e => e.LastCall());
    }

    // guest -> host

    [Test]
    public async Task ImportsRoundTripValues()
    {
        var mismatches = await Call(e => e.TestValues());
        Assert.That(mismatches.ToArray(), Is.Empty, string.Join("\n", mismatches.ToArray()));

        // the values received by the host
        Assert.That(valuesHost.ReceivedIntegers, Does.Contain(((byte)255, (sbyte)-128, (ushort)65535,
            (short)-32768, uint.MaxValue, int.MinValue, ulong.MaxValue, long.MinValue, true)));
        Assert.That(valuesHost.ReceivedSmallIntegers,
            Is.EqualTo(new[] { ((sbyte)-1, (short)-1, (byte)255, (ushort)65535), (sbyte.MinValue, short.MinValue, (byte)0x80, (ushort)0x8000) }));
        Assert.That(valuesHost.ReceivedTexts, Does.Contain("こんにちは"));
        Assert.That(valuesHost.ReceivedTexts, Does.Contain("😀👍🏽"));
        Assert.That(valuesHost.ReceivedTexts, Does.Contain("a\0b"));
        Assert.That(valuesHost.ReceivedChars, Is.EqualTo(new uint[] { 'a', 0xE9, 0, 0xD7FF, 0xE000, 0x1F600, 0x10FFFF }));
        Assert.That(valuesHost.ReceivedShorts, Is.EqualTo(new short[] { -1, short.MinValue, short.MaxValue, 0, 1 }));
        Assert.That(valuesHost.ReceivedManyParams,
            Is.EqualTo("-1,-9223372036854775808,1.5,-2.25,255,-128,65535,-32768,日本,9,10,11,12,13,14,15,16,0.5"));
    }

    [Test]
    public async Task ImportsRoundTripResources()
    {
        Assert.That(await Call(e => e.TestResources()),
            Is.EqualTo("values=9;borrowed=7;taken=9;holder=h!:5:1;multiply=15"));
        Assert.That(resourcesHost.BlobType.Dropped.OrderBy(v => v), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    // host -> guest

    [Test]
    public async Task ExportsRoundTripIntegers()
    {
        var max = ((byte)255, (sbyte)-128, (ushort)65535, (short)-32768, uint.MaxValue, int.MinValue, ulong.MaxValue,
            long.MinValue, true);
        Assert.That(await CallExport(e => e.Integers(max.Item1, max.Item2, max.Item3, max.Item4, max.Item5,
            max.Item6, max.Item7, max.Item8, max.Item9)), Is.EqualTo(max));
        Assert.That(await LastCall(), Is.EqualTo(
            "(255, -128, 65535, -32768, 4294967295, -2147483648, 18446744073709551615, -9223372036854775808, true)"));

        var minusOne = ((byte)0, (sbyte)-1, (ushort)0, (short)-1, 0u, -1, 0ul, -1L, false);
        Assert.That(await CallExport(e => e.Integers(0, -1, 0, -1, 0, -1, 0, -1, false)), Is.EqualTo(minusOne));
        Assert.That(await LastCall(), Is.EqualTo("(0, -1, 0, -1, 0, -1, 0, -1, false)"));
    }

    [Test]
    public async Task ExportsRoundTripSmallIntegers()
    {
        Assert.That(await CallExport(e => e.SmallIntegers(-1, -1, 255, 65535)),
            Is.EqualTo(((sbyte)-1, (short)-1, (byte)255, (ushort)65535)));
        Assert.That(await LastCall(), Is.EqualTo("(-1, -1, 255, 65535)"));

        Assert.That(await CallExport(e => e.SmallIntegers(sbyte.MinValue, short.MinValue, 0x80, 0x8000)),
            Is.EqualTo((sbyte.MinValue, short.MinValue, (byte)0x80, (ushort)0x8000)));
        Assert.That(await LastCall(), Is.EqualTo("(-128, -32768, 128, 32768)"));
    }

    [Test]
    public async Task ExportsRoundTripFloats()
    {
        var values = new (float, double)[]
        {
            (0f, -0d), (-0f, 0d), (float.PositiveInfinity, double.NegativeInfinity),
            (BitConverter.Int32BitsToSingle(1), BitConverter.Int64BitsToDouble(1)),
            (float.MaxValue, double.MinValue), (float.NaN, double.NaN)
        };

        foreach (var (a, b) in values)
        {
            var (ra, rb) = await CallExport(e => e.Floats(a, b));
            if (float.IsNaN(a))
            {
                // NaNs are canonicalized when lifted
                Assert.That(BitConverter.SingleToUInt32Bits(ra), Is.EqualTo(0x7fc00000u));
                Assert.That(BitConverter.DoubleToUInt64Bits(rb), Is.EqualTo(0x7ff8000000000000ul));
                continue;
            }

            Assert.That(BitConverter.SingleToUInt32Bits(ra), Is.EqualTo(BitConverter.SingleToUInt32Bits(a)));
            Assert.That(BitConverter.DoubleToUInt64Bits(rb), Is.EqualTo(BitConverter.DoubleToUInt64Bits(b)));
            Assert.That(await LastCall(),
                Is.EqualTo($"({BitConverter.SingleToUInt32Bits(a)}, {BitConverter.DoubleToUInt64Bits(b)})"));
        }
    }

    [Test]
    public async Task ExportsRoundTripChars()
    {
        foreach (var value in new uint[] { 'a', 0xE9, 0, 0xD7FF, 0xE000, 0x1F600, 0x10FFFF })
        {
            Assert.That((uint)await CallExport(e => e.Chars(value)), Is.EqualTo(value));
            Assert.That(await LastCall(), Is.EqualTo(value.ToString()));
        }
    }

    [Test]
    public async Task ExportsRoundTripStrings()
    {
        var long_ = string.Concat(Enumerable.Repeat("あ😀", 500));
        foreach (var value in new[] { "", "hello", "こんにちは", "😀👍🏽", "a\0b", long_ })
        {
            Assert.That(await CallExport(e => e.Text(value)), Is.EqualTo(value));
            // code points received by the guest
            var codePoints = new List<int>();
            for (var i = 0; i < value.Length; i += char.IsSurrogatePair(value, i) ? 2 : 1)
                codePoints.Add(char.ConvertToUtf32(value, i));
            Assert.That(await LastCall(), Is.EqualTo($"[{string.Join(", ", codePoints)}]"));
        }

        var texts = new[] { "", "a", "日本語", "😀" };
        Assert.That((await CallExport(e => e.Texts(texts))).ToArray(), Is.EqualTo(texts));
        Assert.That(await LastCall(), Is.EqualTo("[\"\", \"a\", \"日本語\", \"😀\"]"));
        Assert.That((await CallExport(e => e.Texts(Array.Empty<string>()))).ToArray(), Is.Empty);
    }

    [Test]
    public async Task ExportsRoundTripLists()
    {
        var bytes = new byte[] { 0, 1, 127, 128, 255 };
        Assert.That((await CallExport(e => e.Bytes(bytes))).ToArray(), Is.EqualTo(bytes));
        Assert.That(await LastCall(), Is.EqualTo("[0, 1, 127, 128, 255]"));

        var shorts = new short[] { -1, short.MinValue, short.MaxValue, 0, 1 };
        Assert.That((await CallExport(e => e.Shorts(shorts))).ToArray(), Is.EqualTo(shorts));
        Assert.That(await LastCall(), Is.EqualTo("[-1, -32768, 32767, 0, 1]"));

        var levels = new[] { IExtValues.Level.High, IExtValues.Level.Low, IExtValues.Level.Mid };
        Assert.That((await CallExport(e => e.Levels(levels))).ToArray(), Is.EqualTo(levels));
        Assert.That(await LastCall(), Is.EqualTo("[Level::High, Level::Low, Level::Mid]"));
    }

    private static IExtValues.Inner Inner(byte a, short? b, params string[] c)
    {
        return new IExtValues.Inner { A = a, B = b, C = c };
    }

    [Test]
    public async Task ExportsRoundTripNestedRecords()
    {
        var values = new[]
        {
            new IExtValues.Outer
            {
                Name = "outer😀",
                Inner = Inner(255, -2, "x", "yz"),
                Items = new[] { Inner(1, null), Inner(2, short.MinValue, "é") },
                Tree = IExtValues.Tree.CreateNode(Inner(3, -1, "leaf")),
                Maybe = Result<uint, string>.CreateError("error"),
                Nested = new ReadOnlyMemory<long>[] { Array.Empty<long>(), new[] { -1, long.MinValue }, new[] { long.MaxValue } }
            },
            new IExtValues.Outer
            {
                Name = "",
                Inner = Inner(0, null),
                Items = Array.Empty<IExtValues.Inner>(),
                Tree = IExtValues.Tree.CreateEmpty(default),
                Maybe = Result<uint, string>.CreateOk(uint.MaxValue),
                Nested = Array.Empty<ReadOnlyMemory<long>>()
            },
            new IExtValues.Outer
            {
                Name = "",
                Inner = Inner(0, null),
                Items = Array.Empty<IExtValues.Inner>(),
                Tree = IExtValues.Tree.CreatePair((ushort.MaxValue, -1.5)),
                Maybe = null,
                Nested = Array.Empty<ReadOnlyMemory<long>>()
            }
        };

        var expectedCalls = new[]
        {
            "Outer { name: \"outer😀\", inner: Inner { a: 255, b: Some(-2), c: [\"x\", \"yz\"] }, " +
            "items: [Inner { a: 1, b: None, c: [] }, Inner { a: 2, b: Some(-32768), c: [\"é\"] }], " +
            "tree: Tree::Node(Inner { a: 3, b: Some(-1), c: [\"leaf\"] }), maybe: Some(Err(\"error\")), " +
            "nested: [[], [-1, -9223372036854775808], [9223372036854775807]] }",
            "Outer { name: \"\", inner: Inner { a: 0, b: None, c: [] }, items: [], tree: Tree::Empty, " +
            "maybe: Some(Ok(4294967295)), nested: [] }",
            "Outer { name: \"\", inner: Inner { a: 0, b: None, c: [] }, items: [], tree: Tree::Pair((65535, -1.5)), " +
            "maybe: None, nested: [] }"
        };

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var result = await CallExport(e => e.OuterValue(value));
            Assert.That(Describe(result), Is.EqualTo(Describe(value)));
            Assert.That(await LastCall(), Is.EqualTo(expectedCalls[i]));
        }
    }

    [Test]
    public async Task ExportsRoundTripVariants()
    {
        var trees = new[]
        {
            IExtValues.Tree.CreateLeaf(-1),
            IExtValues.Tree.CreateNode(Inner(4, 4, "a", "b")),
            IExtValues.Tree.CreatePair((0x8000, double.Epsilon)),
            IExtValues.Tree.CreateText("テキスト"),
            IExtValues.Tree.CreateEmpty(default),
            IExtValues.Tree.CreateLeaf(sbyte.MinValue)
        };

        var result = await CallExport(e => e.Trees(trees));
        Assert.That(result.ToArray().Select(Describe), Is.EqualTo(trees.Select(Describe)));
        Assert.That(await LastCall(), Is.EqualTo(
            "[Tree::Leaf(-1), Tree::Node(Inner { a: 4, b: Some(4), c: [\"a\", \"b\"] }), Tree::Pair((32768, 5e-324)), " +
            "Tree::Text(\"テキスト\"), Tree::Empty, Tree::Leaf(-128)]"));
    }

    [Test]
    public async Task ExportsRoundTripFlags()
    {
        var values = new[]
        {
            (default(IExtValues.SmallFlags), default(IExtValues.MediumFlags), default(IExtValues.LargeFlags)),
            (IExtValues.SmallFlags.F0 | IExtValues.SmallFlags.F2, IExtValues.MediumFlags.F0 | IExtValues.MediumFlags.F11,
                IExtValues.LargeFlags.F0 | IExtValues.LargeFlags.F31),
            ((IExtValues.SmallFlags)0b111, (IExtValues.MediumFlags)0xFFF, (IExtValues.LargeFlags)uint.MaxValue)
        };

        foreach (var value in values)
        {
            Assert.That(await CallExport(e => e.FlagsValue(value.Item1, value.Item2, value.Item3)),
                Is.EqualTo(value));
            Assert.That(await LastCall(), Is.EqualTo($"({(byte)value.Item1}, {(ushort)value.Item2}, {(uint)value.Item3})"));
        }
    }

    [Test]
    public async Task ExportsRoundTripOptionsAndResults()
    {
        var options = await CallExport(e => e.Options(255, new Option<string>("x"), new Option<int?>(-1)));
        Assert.That(options, Is.EqualTo(((byte?)255, new Option<string>("x"), new Option<int?>(-1))));
        Assert.That(await LastCall(), Is.EqualTo("(Some(255), Some(\"x\"), Some(Some(-1)))"));

        options = await CallExport(e => e.Options(null, Option<string>.NoneValue, new Option<int?>(null)));
        Assert.That(options, Is.EqualTo(((byte?)null, Option<string>.NoneValue, new Option<int?>(null))));
        Assert.That(await LastCall(), Is.EqualTo("(None, None, Some(None))"));

        options = await CallExport(e => e.Options(0, new Option<string>(""), Option<int?>.NoneValue));
        Assert.That(options, Is.EqualTo(((byte?)0, new Option<string>(""), Option<int?>.NoneValue)));
        Assert.That(await LastCall(), Is.EqualTo("(Some(0), Some(\"\"), None)"));

        var ok = (Result<sbyte, ulong>.CreateOk(-1), Result<None, string>.CreateOk(default),
            Result<string, None>.CreateOk("ok"), Result<None, None>.CreateOk(default));
        Assert.That(await CallExport(e => e.Results(ok.Item1, ok.Item2, ok.Item3, ok.Item4)), Is.EqualTo(ok));
        Assert.That(await LastCall(), Is.EqualTo("(Ok(-1), Ok(()), Ok(\"ok\"), Ok(()))"));

        var error = (Result<sbyte, ulong>.CreateError(ulong.MaxValue), Result<None, string>.CreateError("error"),
            Result<string, None>.CreateError(default), Result<None, None>.CreateError(default));
        Assert.That(await CallExport(e => e.Results(error.Item1, error.Item2, error.Item3, error.Item4)),
            Is.EqualTo(error));
        Assert.That(await LastCall(), Is.EqualTo("(Err(18446744073709551615), Err(\"error\"), Err(()), Err(()))"));
    }

    [Test]
    public async Task ExportsSpillManyParametersToMemory()
    {
        var result = await CallExport(e => e.ManyParams(-1, long.MinValue, 1.5f, -2.25, 255, -128, 65535, -32768,
            "日本", 9, 10, 11, 12, 13, 14, 15, 16, 0.5));
        Assert.That(result, Is.EqualTo("-1,-9223372036854775808,1.5,-2.25,255,-128,65535,-32768,日本,9,10,11,12,13,14,15,16,0.5"));
    }

    [Test]
    public async Task ExportsRoundTripLongTuple()
    {
        var value = (1, 2, 3, 4, 5, 6, 7, (byte)0xFF, long.MinValue, "tail");
        Assert.That(await CallExport(e => e.LongTuple(value)), Is.EqualTo(value));
        Assert.That(await LastCall(), Is.EqualTo("(1, 2, 3, 4, 5, 6, 7, 255, -9223372036854775808, \"tail\")"));
    }

    [Test]
    public async Task ExportsTakeOwnedResourcesInList()
    {
        var blobs = new[] { 10, 20, 30 }
            .Select(v => (Owned<IExtResources.IBlobResourceImpl>)resourcesHost.BlobType.Wrap(v)).ToArray();
        Assert.That(await Call(e => e.SumOwned(blobs)), Is.EqualTo(60));
        Assert.That(resourcesHost.BlobType.Dropped.OrderBy(v => v), Is.EqualTo(new[] { 10, 20, 30 }));
    }

    private static string Describe(IExtValues.Inner value)
    {
        return $"Inner({value.A}, {value.B?.ToString() ?? "none"}, [{string.Join(", ", value.C.ToArray())}])";
    }

    private static string Describe(IExtValues.Tree value)
    {
        return value.Case switch
        {
            IExtValues.Tree.VariantCase.Leaf => $"leaf({value.Leaf})",
            IExtValues.Tree.VariantCase.Node => $"node({Describe(value.Node!.Value)})",
            IExtValues.Tree.VariantCase.Pair =>
                $"pair({value.Pair!.Value.Item1}, {value.Pair.Value.Item2.ToString("R", CultureInfo.InvariantCulture)})",
            IExtValues.Tree.VariantCase.Text => $"text({value.Text})",
            _ => "empty"
        };
    }

    private static string Describe(IExtValues.Outer value)
    {
        var maybe = value.Maybe switch
        {
            null => "none",
            { Case: Result<uint, string>.VariantCase.Ok } ok => $"ok({ok.Ok})",
            { } error => $"error({error.Error})"
        };
        var nested = string.Join(", ", value.Nested.ToArray().Select(l => $"[{string.Join(", ", l.ToArray())}]"));
        return $"Outer({value.Name}, {Describe(value.Inner)}, [{string.Join(", ", value.Items.ToArray().Select(Describe))}], " +
               $"{Describe(value.Tree)}, {maybe}, [{nested}])";
    }

    private class ValuesHost : IExtValues
    {
        public List<(byte, sbyte, ushort, short, uint, int, ulong, long, bool)> ReceivedIntegers { get; } = new();
        public List<(sbyte, short, byte, ushort)> ReceivedSmallIntegers { get; } = new();
        public List<uint> ReceivedChars { get; } = new();
        public List<string> ReceivedTexts { get; } = new();
        public short[]? ReceivedShorts { get; private set; }
        public string? ReceivedManyParams { get; private set; }

        public ValueTask<(byte, sbyte, ushort, short, uint, int, ulong, long, bool)> Integers(byte a, sbyte b,
            ushort c, short d, uint e, int f, ulong g, long h, bool i)
        {
            var value = (a, b, c, d, e, f, g, h, i);
            ReceivedIntegers.Add(value);
            return new ValueTask<(byte, sbyte, ushort, short, uint, int, ulong, long, bool)>(value);
        }

        public ValueTask<(sbyte, short, byte, ushort)> SmallIntegers(sbyte a, short b, byte c, ushort d)
        {
            ReceivedSmallIntegers.Add((a, b, c, d));
            return new ValueTask<(sbyte, short, byte, ushort)>((a, b, c, d));
        }

        public ValueTask<(float, double)> Floats(float a, double b)
        {
            return new ValueTask<(float, double)>((a, b));
        }

        public ValueTask<ComponentChar> Chars(ComponentChar value)
        {
            ReceivedChars.Add(value);
            return new ValueTask<ComponentChar>(value);
        }

        public ValueTask<string> Text(string value)
        {
            ReceivedTexts.Add(value);
            return new ValueTask<string>(value);
        }

        public ValueTask<ReadOnlyMemory<string>> Texts(ReadOnlyMemory<string> values)
        {
            return new ValueTask<ReadOnlyMemory<string>>(values.ToArray());
        }

        public ValueTask<ReadOnlyMemory<byte>> Bytes(ReadOnlyMemory<byte> values)
        {
            return new ValueTask<ReadOnlyMemory<byte>>(values.ToArray());
        }

        public ValueTask<ReadOnlyMemory<short>> Shorts(ReadOnlyMemory<short> values)
        {
            ReceivedShorts = values.ToArray();
            return new ValueTask<ReadOnlyMemory<short>>(values.ToArray());
        }

        public ValueTask<IExtValues.Outer> OuterValue(IExtValues.Outer value)
        {
            return new ValueTask<IExtValues.Outer>(value);
        }

        public ValueTask<ReadOnlyMemory<IExtValues.Tree>> Trees(ReadOnlyMemory<IExtValues.Tree> values)
        {
            return new ValueTask<ReadOnlyMemory<IExtValues.Tree>>(values.ToArray());
        }

        public ValueTask<(IExtValues.SmallFlags, IExtValues.MediumFlags, IExtValues.LargeFlags)> FlagsValue(
            IExtValues.SmallFlags a, IExtValues.MediumFlags b, IExtValues.LargeFlags c)
        {
            return new ValueTask<(IExtValues.SmallFlags, IExtValues.MediumFlags, IExtValues.LargeFlags)>((a, b, c));
        }

        public ValueTask<ReadOnlyMemory<IExtValues.Level>> Levels(ReadOnlyMemory<IExtValues.Level> values)
        {
            return new ValueTask<ReadOnlyMemory<IExtValues.Level>>(values.ToArray());
        }

        public ValueTask<(byte?, Option<string>, Option<int?>)> Options(byte? a, Option<string> b, Option<int?> c)
        {
            return new ValueTask<(byte?, Option<string>, Option<int?>)>((a, b, c));
        }

        public ValueTask<(Result<sbyte, ulong>, Result<None, string>, Result<string, None>, Result<None, None>)>
            Results(Result<sbyte, ulong> a, Result<None, string> b, Result<string, None> c, Result<None, None> d)
        {
            return new ValueTask<(Result<sbyte, ulong>, Result<None, string>, Result<string, None>,
                Result<None, None>)>((a, b, c, d));
        }

        public ValueTask<string> ManyParams(int p0, long p1, float p2, double p3, byte p4, sbyte p5, ushort p6,
            short p7, string p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16, double p17)
        {
            var values = new object[]
                { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17 };
            ReceivedManyParams = string.Join(",",
                values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)));
            return new ValueTask<string>(ReceivedManyParams);
        }

        public ValueTask<(int, int, int, int, int, int, int, byte, long, string)> LongTuple(
            (int, int, int, int, int, int, int, byte, long, string) value)
        {
            return new ValueTask<(int, int, int, int, int, int, int, byte, long, string)>(value);
        }
    }

    internal class BlobType : HostResourceTypeBase<int>, IExtResources.IBlobResourceImpl
    {
        public List<int> Dropped { get; } = new();
        public IResourceType Type => this;

        public ValueTask<int> Value(Borrowed<IExtResources.IBlobResourceImpl> self)
        {
            return new ValueTask<int>(Unwrap(self));
        }

        protected override void OnDrop(int value)
        {
            Dropped.Add(value);
        }
    }

    private class ResourcesHost : IExtResources
    {
        public BlobType BlobType { get; } = new();
        public IExtResources.IBlobResourceImpl Blob => BlobType;

        private Owned<IExtResources.IBlobResourceImpl> Wrap(int value)
        {
            return (Owned<IExtResources.IBlobResourceImpl>)BlobType.Wrap(value);
        }

        public ValueTask<Owned<IExtResources.IBlobResourceImpl>> MakeBlob(int value)
        {
            return new ValueTask<Owned<IExtResources.IBlobResourceImpl>>(Wrap(value));
        }

        public ValueTask<ReadOnlyMemory<Owned<IExtResources.IBlobResourceImpl>>> MakeBlobs(ReadOnlyMemory<int> values)
        {
            return new ValueTask<ReadOnlyMemory<Owned<IExtResources.IBlobResourceImpl>>>(
                values.ToArray().Select(Wrap).ToArray());
        }

        public ValueTask<int> TakeBlobs(ReadOnlyMemory<Owned<IExtResources.IBlobResourceImpl>> blobs)
        {
            var sum = 0;
            foreach (var blob in blobs.Span)
            {
                sum += BlobType.Unwrap(blob, false);
                blob.handle.Dispose();
            }

            return new ValueTask<int>(sum);
        }

        public ValueTask<int> SumBorrowed(ReadOnlyMemory<Borrowed<IExtResources.IBlobResourceImpl>> blobs)
        {
            var sum = 0;
            foreach (var blob in blobs.Span) sum += BlobType.Unwrap(blob);
            return new ValueTask<int>(sum);
        }

        public ValueTask<IExtResources.Holder> SwapHolder(IExtResources.Holder value)
        {
            return new ValueTask<IExtResources.Holder>(new IExtResources.Holder
            {
                Name = value.Name + "!",
                Blob = value.Extra!.Value,
                Extra = value.Blob
            });
        }

        public ValueTask<int> Multiply(IExtResources.Borrowing value)
        {
            return new ValueTask<int>(BlobType.Unwrap(value.Blob) * value.Factor);
        }
    }
}
