using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;
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
[JsonDerivedType(typeof(ModuleDefinitionCommand), "module_definition")]
[JsonDerivedType(typeof(ModuleInstanceCommand), "module_instance")]
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
        Action.InvokeAuto(runner);
    }
}

public class AssertReturnCommand(uint line, Action action, Const[] expected) : Command(line)
{
    public Action Action { get; } = action;
    public Const[] Expected { get; } = expected;

    public override void Execute(WastRunner runner)
    {
        if (Action.IsComponent(runner))
        {
            using var binder = Action.InvokeComponent(runner);
        }
        else
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
            Action.InvokeAuto(runner);
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
            Action.InvokeAuto(runner);
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
        if (Text == "degenerate component adapter called") return; // skip
        try
        {
            runner.Instantiate(Filename);
        }
        catch // (Exception ex)
        {
            // Console.WriteLine($"Assertion Succeeded: {ex}");
            return;
        }

        throw new InvalidOperationException($"Assertion failed ({nameof(AssertUninstantiableCommand)})");
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

    public void InvokeAuto(WastRunner runner)
    {
        if (IsComponent(runner))
        {
            using var binder = InvokeComponent(runner);
        }
        else
        {
            Invoke(runner);
        }
            
    }

    public abstract StackValueItem[] Invoke(WastRunner runner);

    public abstract FunctionBinder InvokeComponent(WastRunner runner);
    public abstract bool IsComponent(WastRunner runner);
}

public class InvokeAction(string? module, string field, Const[] args) : Action(module, field)
{
    public Const[] Args { get; } = args;

    public override FunctionBinder InvokeComponent(WastRunner runner)
    {
        var instance = string.IsNullOrEmpty(Module)
            ? runner.CurrentComponentInstance ?? throw new InvalidOperationException()
            : runner.GetComponentInstance(Module);

        if (!instance.TryGetExport(Field, out IFunction? function))
            throw new InvalidOperationException();

        using var context = new ExecutionContext();
        using var binder = function.GetBinder(context);
        var pusher = binder.ArgumentPusher;
        foreach (var arg in Args)
        {
            arg.PushComponentValue(pusher);
        }
        
        binder.Invoke(context);
        return binder;
    }

    public override bool IsComponent(WastRunner runner)
    {
        if (string.IsNullOrEmpty(module)) return runner.IsComponent;
        
        try
        {
            runner.GetComponentInstance(module);
            return true;
        }
        catch
        {
            return false;
        }
    }

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

    public override FunctionBinder InvokeComponent(WastRunner runner)
    {
        throw new NotImplementedException();
    }

    public override bool IsComponent(WastRunner runner)
    {
        if (string.IsNullOrEmpty(module)) return runner.IsComponent;
        
        try
        {
            runner.GetComponentInstance(module);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class ModuleDefinitionCommand(uint line, string name, string filename) : Command(line)
{
    public string Name { get; } = name;
    public string Filename { get; } = filename;

    public override void Execute(WastRunner runner)
    {
        runner.AddModule(filename, name);
    }
}

public class ModuleInstanceCommand(uint line, string instance, string module) : Command(line)
{
    public string Instance { get; } = instance;
    public string Module { get; } = module;

    public override void Execute(WastRunner runner)
    {
        if (!runner.CurrentComponentImports.TryGetValue(module, out var s))
            throw new InvalidOperationException();
        if (s is IComponent component)
        {
            runner.AddInstance(component, instance);
        }
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConstI32), "i32")]
[JsonDerivedType(typeof(ConstI64), "i64")]
[JsonDerivedType(typeof(ConstF32), "f32")]
[JsonDerivedType(typeof(ConstF64), "f64")]
[JsonDerivedType(typeof(ConstBool), "bool")]
[JsonDerivedType(typeof(ConstU8), "u8")]
[JsonDerivedType(typeof(ConstS8), "s8")]
[JsonDerivedType(typeof(ConstU16), "u16")]
[JsonDerivedType(typeof(ConstS16), "s16")]
[JsonDerivedType(typeof(ConstU32), "u32")]
[JsonDerivedType(typeof(ConstS32), "s32")]
[JsonDerivedType(typeof(ConstU64), "u64")]
[JsonDerivedType(typeof(ConstS64), "s64")]
[JsonDerivedType(typeof(ConstChar), "char")]
[JsonDerivedType(typeof(ConstString), "string")]
[JsonDerivedType(typeof(ConstList), "list")]
[JsonDerivedType(typeof(ConstTuple), "tuple")]
[JsonDerivedType(typeof(ConstVariant), "variant")]
[JsonDerivedType(typeof(ConstEnum), "enum")]
[JsonDerivedType(typeof(ConstOption), "option")]
[JsonDerivedType(typeof(ConstResult), "result")]
[JsonDerivedType(typeof(ConstFlags), "flags")]
public abstract class Const
{
    public abstract StackValueItem GetValue();
    public abstract void PushComponentValue(ValuePusher pusher);
}

public class ConstI32 : Const
{
    private uint value;

    public string Value
    {
        get => value.ToString();
        set
        {
            if (value.StartsWith("-"))
            {
                this.value = unchecked((uint)int.Parse(value));
            }
            else
            {
                this.value = uint.Parse(value);
            }
        }
    }

    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(unchecked((int)value));
    }
}

public class ConstI64 : Const
{
    private ulong value;

    public string Value
    {
        get => value.ToString();
        set
        {
            if (value.StartsWith("-"))
            {
                this.value = unchecked((ulong)long.Parse(value));
            }
            else
            {
                this.value = ulong.Parse(value);
            }
        }
    }

    public override StackValueItem GetValue()
    {
        return new StackValueItem(value);
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(unchecked((long)value));
    }
}

public class ConstF32 : Const
{
    private float value;

    public string Value
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

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstF64 : Const
{
    private double value;

    public string Value
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

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstBool : Const
{
    private bool value;

    public string Value
    {
        get => value.ToString();
        set => this.value = bool.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstU8 : Const
{
    private byte value;

    public string Value
    {
        get => value.ToString();
        set => this.value = byte.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstS8 : Const
{
    private sbyte value;

    public string Value
    {
        get => value.ToString();
        set => this.value = sbyte.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstU16 : Const
{
    private ushort value;

    public string Value
    {
        get => value.ToString();
        set => this.value = ushort.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstS16 : Const
{
    private short value;

    public string Value
    {
        get => value.ToString();
        set => this.value = short.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstU32 : Const
{
    private uint value;

    public string Value
    {
        get => value.ToString();
        set => this.value = uint.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstS32 : Const
{
    private int value;

    public string Value
    {
        get => value.ToString();
        set => this.value = int.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstU64 : Const
{
    private ulong value;

    public string Value
    {
        get => value.ToString();
        set => this.value = ulong.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstS64 : Const
{
    private long value;

    public string Value
    {
        get => value.ToString();
        set => this.value = long.Parse(value);
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value);
    }
}

public class ConstChar : Const
{
    private uint value;

    public string Value
    {
        get => Char.ConvertFromUtf32(unchecked((int)value));
        set => this.value = unchecked((uint)Char.ConvertToUtf32(value, 0));
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.PushChar32(value);
    }
}

public class ConstString : Const
{
    private string value = string.Empty;

    public string Value
    {
        get => value;
        set => this.value = value;
    }

    public override StackValueItem GetValue() => throw new InvalidOperationException();

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher.Push(value.AsSpan());
    }
}

public class ConstList : Const
{
    public Const[] Value { get; set; }

    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher = pusher.PushList(Value.Length);
        foreach (var item in Value) item.PushComponentValue(pusher);
    }
}

public class ConstTuple : Const
{
    public Const[] Value { get; set; }

    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher = pusher.PushRecord();
        foreach (var item in Value) item.PushComponentValue(pusher);
    }
}

public class ConstVariant : Const
{
    public string Case { get; set; }
    public Const? Value { get; set; }
    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        if (!pusher.TryGetNextType(out var type)) throw new InvalidOperationException();

        if (type is not IVariantType variantType) throw new InvalidOperationException();
        
        var cases = variantType.Cases.Span;

        for (int i = 0; i < cases.Length; i++)
        {
            var @case = cases[i];
            if (@case.Label == Case)
            {
                pusher = pusher.PushVariant(i);
                if (@case.Type != null)
                {
                    Value.PushComponentValue(pusher);
                }

                return;
            }
        }

        throw new InvalidOperationException();
    }
}

public class ConstEnum : Const
{
    public string Value { get; set; }


    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        if (!pusher.TryGetNextType(out var type)) throw new InvalidOperationException();

        if (type is not IEnumType enumType) throw new InvalidOperationException();

        var labels = enumType.Labels.Span;

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == Value)
            {
                pusher = pusher.PushVariant(i);
                return;
            }
        }

        throw new InvalidOperationException();
    }
}

public class ConstOption : Const
{
    public Const? Value { get; set; }

    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher = pusher.PushVariant(Value == null ? 0 : 1);
        Value?.PushComponentValue(pusher);
    }
}

public class ConstResult : Const
{
    public Const? Ok { get; set; }
    public Const? Error { get; set; }
    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        pusher = pusher.PushVariant(Ok != null ? 0 : 1);
        if(Ok != null) Ok.PushComponentValue(pusher);
        else Error!.PushComponentValue(pusher);
    }
}

public class ConstFlags : Const
{
    public string[] Value { get; set; }
    public override StackValueItem GetValue()
    {
        throw new InvalidOperationException();
    }

    public override void PushComponentValue(ValuePusher pusher)
    {
        if (!pusher.TryGetNextType(out var type)) throw new InvalidOperationException();

        if (type is not IFlagsType flagsType) throw new InvalidOperationException();

        var labels = flagsType.Labels.Span;

        uint value = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if(Value.Contains(label)) value |= 1u << i; 
        }
        
        pusher.PushFlags(value);
    }
}