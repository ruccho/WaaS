using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.Tests;

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
public abstract class Const
{
    public abstract string Value { get; set; }
    public abstract StackValueItem GetValue();
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