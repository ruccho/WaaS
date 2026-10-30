using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.Tests.Wast;

public class WastProc(string sourceFilename, ReadOnlyMemory<Command> commands)
{
    public string SourceFilename { get; } = sourceFilename;
    public ReadOnlyMemory<Command> Commands { get; } = commands;

    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        Converters = { new JsonStringEnumConverter<ModuleType>(JsonNamingPolicy.SnakeCaseLower) },
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ModuleCommand), "module")]
[JsonDerivedType(typeof(ActionCommand), "action")]
[JsonDerivedType(typeof(AssertReturnCommand), "assert_return")]
[JsonDerivedType(typeof(AssertExhaustionCommand), "assert_exhaustion")]
[JsonDerivedType(typeof(AssertTrapCommand), "assert_trap")]
[JsonDerivedType(typeof(AssertInvalidCommand), "assert_invalid")]
[JsonDerivedType(typeof(AssertMalformedCommand), "assert_malformed")]
[JsonDerivedType(typeof(AssertUninstantiableCommand), "assert_uninstantiable")]
[JsonDerivedType(typeof(AssertUnlinkableCommand), "assert_unlinkable")]
[JsonDerivedType(typeof(RegisterCommand), "register")]
public abstract class Command(uint line)
{
    public uint Line { get; } = line;

    public abstract void Execute(WastRunner runner);
}

public class ModuleCommand(uint line, string? name, string filename) : Command(line)
{
    public string? Name { get; } = name;
    public string Filename { get; } = filename;

    public override void Execute(WastRunner runner)
    {
        runner.AddInstance(Filename, Name);
    }
}

public class ActionCommand(uint line, Action action) : Command(line)
{
    public Action Action { get; } = action;

    public override void Execute(WastRunner runner)
    {
        Action.Invoke(runner);
    }
}

public class AssertReturnCommand(uint line, Action action, Const[] expected) : Command(line)
{
    public Action Action { get; } = action;
    public Const[] Expected { get; } = expected;

    public override void Execute(WastRunner runner)
    {
        var returns = Action.Invoke(runner);

        if (Expected.Any(e => e is ConstV128 or ConstReference))
        {
            // v128 lanes are compared exactly, with the NaN patterns taken into account, and references by the objects
            if (returns.Length != Expected.Length || !Expected.Zip(returns).All(pair => pair.First.Matches(pair.Second)))
                throw new InvalidOperationException(
                    $"Assertion failed. expected: [{string.Join(", ", Expected.Select(e => e.ToString()))}], actual: [{string.Join(", ", returns.Select(r => r.ToString()))}] ");
            return;
        }

        if (!returns.SequenceEqual(Expected.Select(e => e.GetValue())))
        {
            if (!returns.SequenceEqual(Expected.Select(e => e.GetValue()),
                    ApproximateStackValueItemEqualityComparer.Instance))
                throw new InvalidOperationException(
                    $"Assertion failed. expected: [{string.Join(", ", Expected.Select(e => e.GetValue().ToString()))}], actual: [{string.Join(", ", returns.Select(r => r.ToString()))}] ");
            Console.WriteLine(
                $"Assertion failed but approximately equal. expected: [{string.Join(", ", Expected.Select(e => e.GetValue().ToString()))}], actual: [{string.Join(", ", returns.Select(r => r.ToString()))}] ");
        }
    }
}

public class ApproximateStackValueItemEqualityComparer : EqualityComparer<StackValueItem>
{
    public static readonly ApproximateStackValueItemEqualityComparer Instance = new();

    public override bool Equals(StackValueItem x, StackValueItem y)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        x.ExpectValue(out var typeX);
        y.ExpectValue(out var typeY);
        if (typeX != typeY) return false;

        switch (typeX)
        {
            case ValueType.I32:
                return x.ExpectValueI32() == y.ExpectValueI32();
            case ValueType.I64:
                return x.ExpectValueI64() == y.ExpectValueI64();
            case ValueType.F32:
            {
                var vx = x.ExpectValueF32();
                var vy = y.ExpectValueF32();
                if (vx == float.NegativeZero) vx = 0;
                if (vy == float.NegativeZero) vy = 0;
                if (float.IsNaN(vx) && float.IsNaN(vy)) return true;
                if (Math.Abs(vx - vy) / vx <= float.Epsilon) return true;
                return Math.Abs((long)Unsafe.As<float, uint>(ref vx) - Unsafe.As<float, uint>(ref vy)) <= 1;
            }
            case ValueType.F64:
            {
                var vx = x.ExpectValueF64();
                var vy = y.ExpectValueF64();
                if (vx == float.NegativeZero) vx = 0;
                if (vy == float.NegativeZero) vy = 0;
                if (double.IsNaN(vx) && double.IsNaN(vy)) return true;
                if (Math.Abs(vx - vy) / vx <= double.Epsilon) return true;
                return Math.Abs((long)Unsafe.As<double, uint>(ref vx) - Unsafe.As<double, uint>(ref vy)) <= 1;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    public override int GetHashCode(StackValueItem obj)
    {
        return obj.GetHashCode();
    }
}

public class AssertExhaustionCommand(uint line, Action action, string text) : Command(line)
{
    public Action Action { get; } = action;
    public string Text { get; } = text;

    public override void Execute(WastRunner runner)
    {
        try
        {
            Action.Invoke(runner);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException();
    }
}

public class AssertTrapCommand(uint line, Action action, string text) : Command(line)
{
    public Action Action { get; } = action;
    public string Text { get; } = text;

    public override void Execute(WastRunner runner)
    {
        try
        {
            Action.Invoke(runner);
        }
        catch /* (TrapException) */
        {
            return;
        }

        throw new InvalidOperationException();
    }
}

public class AssertInvalidCommand(uint line, string filename, string text, ModuleType moduleType)
    : Command(line)
{
    public string Filename { get; } = filename;
    public string Text { get; } = text;
    public ModuleType ModuleType { get; } = moduleType;

    public override void Execute(WastRunner runner)
    {
        if (ModuleType == ModuleType.Text) throw new NotSupportedException();
        try
        {
            runner.Instantiate(Filename);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException();
    }
}

public class AssertMalformedCommand(uint line, string filename, string text, ModuleType moduleType)
    : Command(line)
{
    public string Filename { get; } = filename;
    public string Text { get; } = text;
    public ModuleType ModuleType { get; } = moduleType;

    public override void Execute(WastRunner runner)
    {
        if (ModuleType == ModuleType.Text)
        {
            Console.WriteLine($"command #{Line}: Assertion skipped. (text module is not supported)");
            return;
        }

        try
        {
            runner.Instantiate(Filename);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException();
    }
}

public class AssertUninstantiableCommand(uint line, string filename, string text, ModuleType moduleType)
    : Command(line)
{
    public string Filename { get; } = filename;
    public string Text { get; } = text;
    public ModuleType ModuleType { get; } = moduleType;

    public override void Execute(WastRunner runner)
    {
        if (ModuleType == ModuleType.Text) throw new NotSupportedException();
        try
        {
            runner.Instantiate(Filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Assertion Succeeded: {ex}");
            return;
        }

        throw new InvalidOperationException();
    }
}

public class AssertUnlinkableCommand(uint line, string filename, string text, ModuleType moduleType)
    : Command(line)
{
    public string Filename { get; } = filename;
    public string Text { get; } = text;
    public ModuleType ModuleType { get; } = moduleType;

    public override void Execute(WastRunner runner)
    {
        if (ModuleType == ModuleType.Text) throw new NotSupportedException();
        try
        {
            runner.Instantiate(Filename);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException();
    }
}

public class RegisterCommand(uint line, string? name, string @as) : Command(line)
{
    public string? Name { get; } = name;
    public string As { get; } = @as;

    public override void Execute(WastRunner runner)
    {
        runner.Register(
            Name != null ? runner.GetInstance(Name) : runner.CurrentInstance ?? throw new InvalidOperationException(),
            As);
    }
}

public enum ModuleType
{
    Binary,
    Text
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(InvokeAction), "invoke")]
[JsonDerivedType(typeof(GetAction), "get")]
public abstract class Action(string? module, string field)
{
    public string? Module { get; } = module;
    public string Field { get; } = field;

    public abstract StackValueItem[] Invoke(WastRunner runner);
}

public class InvokeAction(string? module, string field, Const[] args) : Action(module, field)
{
    public Const[] Args { get; } = args;


    public override StackValueItem[] Invoke(WastRunner runner)
    {
        var instance = string.IsNullOrEmpty(Module)
            ? runner.CurrentInstance ?? throw new InvalidOperationException()
            : runner.GetInstance(Module);

        if (instance.ExportInstance.Items[Field] is not InstanceFunction function)
            throw new InvalidOperationException();

        Span<StackValueItem> args = stackalloc StackValueItem[Args.Length];
        for (var i = 0; i < args.Length; i++) args[i] = Args[i].GetValue();

        runner.Context.Invoke(function, args);

        var resultLength = runner.Context.ResultLength;
        if (resultLength == 0) return [];

        Span<StackValueItem> results = stackalloc StackValueItem[resultLength];

        runner.Context.TakeResults(results);
        return results.ToArray();
    }
}

public class GetAction(string? module, string field) : Action(module, field)
{
    public override StackValueItem[] Invoke(WastRunner runner)
    {
        var instance = string.IsNullOrEmpty(Module)
            ? runner.CurrentInstance ?? throw new InvalidOperationException()
            : runner.GetInstance(Module);

        if (instance.ExportInstance.Items[Field] is not Global global) throw new InvalidOperationException();

        return [global.GetStackValue()];
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConstI32), "i32")]
[JsonDerivedType(typeof(ConstI64), "i64")]
[JsonDerivedType(typeof(ConstF32), "f32")]
[JsonDerivedType(typeof(ConstF64), "f64")]
[JsonDerivedType(typeof(ConstV128), "v128")]
[JsonDerivedType(typeof(ConstFuncRef), "funcref")]
[JsonDerivedType(typeof(ConstExternRef), "externref")]
public abstract class Const
{
    public abstract string Value { get; set; }
    public abstract StackValueItem GetValue();

    /// <summary>
    ///     Determines whether the actual value matches the expected value.
    /// </summary>
    public virtual bool Matches(StackValueItem actual)
    {
        var expected = GetValue();
        return actual.Equals(expected) || ApproximateStackValueItemEqualityComparer.Instance.Equals(actual, expected);
    }

    public override string ToString()
    {
        return GetValue().ToString();
    }
}

/// <summary>
///     A value of a type that WaaS does not support yet. Commands using it are reported as unsupported.
/// </summary>
public abstract class ConstUnsupported : Const
{
    [JsonIgnore] public override string Value { get; set; } = "";

    // "value" is kept as is
    [JsonExtensionData] public Dictionary<string, JsonElement>? Properties { get; set; }

    public override StackValueItem GetValue()
    {
        throw new NotSupportedException();
    }
}

public class ConstV128 : Const
{
    [JsonIgnore] public override string Value { get; set; } = "";

    public string LaneType { get; set; } = "";

    [JsonPropertyName("value")] public string[] Lanes { get; set; } = [];

    private int LaneSize => LaneType switch
    {
        "i8" => 1,
        "i16" => 2,
        "i32" or "f32" => 4,
        "i64" or "f64" => 8,
        _ => throw new NotSupportedException($"unknown lane type: {LaneType}")
    };

    private static ulong ParseLane(string lane)
    {
        // lanes are usually emitted as unsigned decimals, but accept signed ones as well
        return lane.StartsWith('-') ? unchecked((ulong)long.Parse(lane)) : ulong.Parse(lane);
    }

    public override StackValueItem GetValue()
    {
        var size = LaneSize;
        if (Lanes.Length * size != 16) throw new InvalidOperationException("invalid number of lanes");

        Span<byte> bytes = stackalloc byte[16];
        for (var i = 0; i < Lanes.Length; i++)
        {
            if (Lanes[i].StartsWith("nan")) throw new InvalidOperationException("NaN pattern cannot be used as a value");
            var lane = ParseLane(Lanes[i]);
            for (var b = 0; b < size; b++) bytes[i * size + b] = (byte)(lane >> (b * 8));
        }

        return new StackValueItem(V128.Create(bytes));
    }

    public override bool Matches(StackValueItem actual)
    {
        if (!actual.TryGetValue(out V128 vector)) return false;

        var size = LaneSize;
        if (Lanes.Length * size != 16) return false;

        Span<byte> bytes = stackalloc byte[16];
        vector.CopyTo(bytes);

        for (var i = 0; i < Lanes.Length; i++)
        {
            ulong lane = 0;
            for (var b = 0; b < size; b++) lane |= (ulong)bytes[i * size + b] << (b * 8);

            var expected = Lanes[i];
            if (expected.StartsWith("nan"))
            {
                // "nan:canonical" requires the canonical NaN (any sign).
                // "nan:arithmetic" requires a NaN with the most significant bit of the payload set.
                var canonical = expected == "nan:canonical";
                var matches = LaneType switch
                {
                    "f32" => canonical
                        ? (lane & 0x7FFF_FFFF) == 0x7FC0_0000
                        : (lane & 0x7FC0_0000) == 0x7FC0_0000,
                    "f64" => canonical
                        ? (lane & 0x7FFF_FFFF_FFFF_FFFF) == 0x7FF8_0000_0000_0000
                        : (lane & 0x7FF8_0000_0000_0000) == 0x7FF8_0000_0000_0000,
                    _ => false
                };
                if (!matches) return false;
                continue;
            }

            var mask = size == 8 ? ulong.MaxValue : (1UL << (size * 8)) - 1;
            if ((ParseLane(expected) & mask) != lane) return false;
        }

        return true;
    }

    public override string ToString()
    {
        return $"v128 {LaneType} [{string.Join(" ", Lanes)}]";
    }
}

/// <summary>
///     A reference value. The value is "null" for the null reference, a number for a host reference
///     (<c>ref.extern N</c>), or empty for any non-null reference in the expected results.
/// </summary>
public abstract class ConstReference : Const
{
    private static readonly Dictionary<string, HostReference> HostReferences = new();

    [JsonPropertyName("value")] public override string Value { get; set; } = "";

    protected abstract ValueType Type { get; }

    public override StackValueItem GetValue()
    {
        if (Value == "null") return StackValueItem.FromReference(Type, null);
        if (Value == "") throw new InvalidOperationException("a non-null reference without a value cannot be created");
        if (Type != ValueType.ExternRef) throw new NotSupportedException();

        HostReference reference;
        lock (HostReferences)
        {
            if (!HostReferences.TryGetValue(Value, out reference!)) HostReferences[Value] = reference = new HostReference(Value);
        }

        return StackValueItem.FromReference(Type, reference);
    }

    public override bool Matches(StackValueItem actual)
    {
        if (!actual.IsType(Type)) return false;
        if (Value == "") return !actual.IsNullReference;
        return actual.Equals(GetValue());
    }

    public override string ToString()
    {
        return $"{Type} {(Value == "" ? "(non-null)" : Value)}";
    }

    private sealed record HostReference(string Value);
}

public class ConstFuncRef : ConstReference
{
    protected override ValueType Type => ValueType.FuncRef;
}

public class ConstExternRef : ConstReference
{
    protected override ValueType Type => ValueType.ExternRef;
}

public class ConstI32 : Const
{
    private uint value;

    public override string Value
    {
        get => value.ToString();
        set => this.value = uint.Parse(value);
    }

    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }
}

public class ConstI64 : Const
{
    private ulong value;

    public override string Value
    {
        get => value.ToString();
        set => this.value = ulong.Parse(value);
    }

    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }
}

public class ConstF32 : Const
{
    private float value;

    public override string Value
    {
        get => Unsafe.As<float, uint>(ref value).ToString();
        set
        {
            if (value.StartsWith("nan")) this.value = float.NaN;
            else Unsafe.As<float, uint>(ref this.value) = uint.Parse(value);
        }
    }

    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }
}

public class ConstF64 : Const
{
    private double value;

    public override string Value
    {
        get => Unsafe.As<double, ulong>(ref value).ToString();
        set
        {
            if (value.StartsWith("nan")) this.value = double.NaN;
            else Unsafe.As<double, ulong>(ref this.value) = ulong.Parse(value);
        }
    }


    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }
}