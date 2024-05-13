using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.Tests;

public class WastProc
{
    public string SourceFilename { get; set; }
    public ReadOnlyMemory<Command> Commands { get; set; }

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
public abstract class Command
{
    public uint Line { get; set; }

    public abstract void Execute(WastRunner runner);
}

public class ModuleCommand : Command
{
    public string? Name { get; set; }
    public string Filename { get; set; }

    public override void Execute(WastRunner runner)
    {
        runner.AddInstance(Filename, Name);
    }
}

public class ActionCommand : Command
{
    public Action Action { get; set; }

    public override void Execute(WastRunner runner)
    {
        Action.Invoke(runner);
    }
}

public class AssertReturnCommand : Command
{
    public Action Action { get; set; }
    public Const[] Expected { get; set; }

    public override void Execute(WastRunner runner)
    {
        var returns = Action.Invoke(runner);
        if (!returns.SequenceEqual(Expected.Select(e => e.GetValue())))
        {
            if (!returns.SequenceEqual(Expected.Select(e => e.GetValue()),
                    ApproximateStackValueItemEqualityComparer.Default))
                throw new InvalidOperationException(
                    $"Assertion failed. expected: [{string.Join(", ", Expected.Select(e => e.GetValue().ToString()))}], actual: [{string.Join(", ", returns.Select(r => r.ToString()))}] ");
            Console.WriteLine(
                $"Assertion failed but approximately equal. expected: [{string.Join(", ", Expected.Select(e => e.GetValue().ToString()))}], actual: [{string.Join(", ", returns.Select(r => r.ToString()))}] ");
        }
    }
}

public class ApproximateStackValueItemEqualityComparer : EqualityComparer<StackValueItem>
{
    public static readonly ApproximateStackValueItemEqualityComparer Default = new();

    public override bool Equals(StackValueItem x, StackValueItem y)
    {
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
    }

    public override int GetHashCode(StackValueItem obj)
    {
        return obj.GetHashCode();
    }
}

public class AssertExhaustionCommand : Command
{
    public Action Action { get; set; }
    public string Text { get; set; }

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

public class AssertTrapCommand : Command
{
    public Action Action { get; set; }
    public string Text { get; set; }

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

public class AssertInvalidCommand : Command
{
    public string Filename { get; set; }
    public string Text { get; set; }
    public ModuleType ModuleType { get; set; }

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

public class AssertMalformedCommand : Command
{
    public string Filename { get; set; }
    public string Text { get; set; }
    public ModuleType ModuleType { get; set; }

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

public class AssertUninstantiableCommand : Command
{
    public string Filename { get; set; }
    public string Text { get; set; }
    public ModuleType ModuleType { get; set; }

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

public class AssertUnlinkableCommand : Command
{
    public string Filename { get; set; }
    public string Text { get; set; }
    public ModuleType ModuleType { get; set; }

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

public class RegisterCommand : Command
{
    public string? Name { get; set; }
    public string As { get; set; }

    public override void Execute(WastRunner runner)
    {
        runner.Register(Name != null ? runner.GetInstance(Name) : runner.CurrentInstance, As);
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
public abstract class Action
{
    public string? Module { get; set; }
    public string Field { get; set; }

    public abstract StackValueItem[] Invoke(WastRunner runner);
}

public class InvokeAction : Action
{
    public Const[] Args { get; set; }

    public override StackValueItem[] Invoke(WastRunner runner)
    {
        var instance = string.IsNullOrEmpty(Module) ? runner.CurrentInstance : runner.GetInstance(Module);

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

public class GetAction : Action
{
    public override StackValueItem[] Invoke(WastRunner runner)
    {
        var instance = string.IsNullOrEmpty(Module) ? runner.CurrentInstance : runner.GetInstance(Module);

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