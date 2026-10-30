using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Constant expression in a WebAssembly module.
    /// </summary>
    public interface IConstantExpression
    {
        StackValueItem Evaluate(GlobalInstance globalInstance);
        StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> initializingGlobals);

        /// <param name="globals">The globals that the expression can read.</param>
        /// <param name="functions">The functions in the function index space, referenced by <c>ref.func</c>.</param>
        StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> globals, ReadOnlySpan<IInvocableFunction> functions);
    }

    public class ConstantExpression : IConstantExpression
    {
        internal ConstantExpression(ref ModuleReader reader)
        {
            Instructions = InstructionReader.ReadTillEnd(ref reader);
            if (Instructions.Length < 2) throw new InvalidModuleException();
        }

        public ReadOnlyMemory<Instruction> Instructions { get; }

        public StackValueItem Evaluate(GlobalInstance globalInstance)
        {
            return Evaluate(globalInstance.Globals.Span, ReadOnlySpan<IInvocableFunction>.Empty);
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> initializingGlobals)
        {
            return Evaluate(initializingGlobals, ReadOnlySpan<IInvocableFunction>.Empty);
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> globals, ReadOnlySpan<IInvocableFunction> functions)
        {
            var instrs = Instructions.Span;

            StackValueItem? result = null;
            foreach (var instr in instrs)
            {
                if (instr is End) break;
                if (result.HasValue) throw new InvalidModuleException("type mismatch"); // duplicate assignment
                result = instr switch
                {
                    ConstI32 c => new StackValueItem(c.Value),
                    ConstI64 c => new StackValueItem(c.Value),
                    ConstF32 c => new StackValueItem(c.Value),
                    ConstF64 c => new StackValueItem(c.Value),
                    ConstV128 c => new StackValueItem(c.Value),
                    RefNull r => StackValueItem.FromReference(r.Type, null),
                    RefFunc r => StackValueItem.FromReference(ValueType.FuncRef,
                        functions[checked((int)r.FunctionIndex)]),
                    GlobalGet g => globals[checked((int)g.GlobalIndex)].GetStackValue(),
                    _ => throw new InvalidModuleException(
                        $"Instruction {instr.GetType().Name} cannot be used for constant expression!")
                };
            }

            if (result == null) throw new InvalidModuleException();

            return result.Value;
        }

        /// <summary>
        ///     Validates that the expression is constant and produces a value of <paramref name="type" />.
        /// </summary>
        internal void Validate(in ValidationContext context, ValueType type, int numAccessibleGlobals)
        {
            var instrs = Instructions.Span;
            ValueType? result = null;
            foreach (var instr in instrs)
            {
                if (instr is End) break;
                if (result.HasValue) throw new InvalidModuleException("type mismatch");
                result = instr switch
                {
                    ConstI32 => ValueType.I32,
                    ConstI64 => ValueType.I64,
                    ConstF32 => ValueType.F32,
                    ConstF64 => ValueType.F64,
                    ConstV128 => ValueType.V128,
                    RefNull r => r.Type,
                    RefFunc r => context.ValidateFunctionReference(r.FunctionIndex, false),
                    GlobalGet g => ValidateGlobalGet(context, g.GlobalIndex, numAccessibleGlobals),
                    _ => throw new InvalidModuleException("constant expression required")
                };
            }

            if (result != type) throw new InvalidModuleException("type mismatch");
        }

        private static ValueType ValidateGlobalGet(in ValidationContext context, uint index, int numAccessibleGlobals)
        {
            if (index >= numAccessibleGlobals) throw new InvalidModuleException("unknown global");
            var globalType = context.GetGlobalType(index);
            if (globalType.Mutability != Mutability.Const) throw new InvalidModuleException("constant expression required");
            return globalType.ValueType;
        }
    }

    public class ConstantExpressionConstant : IConstantExpression
    {
        private readonly StackValueItem item;

        internal ConstantExpressionConstant(StackValueItem item)
        {
            this.item = item;
        }

        public StackValueItem Evaluate(GlobalInstance globalInstance)
        {
            return item;
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> initializingGlobals)
        {
            return item;
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> globals, ReadOnlySpan<IInvocableFunction> functions)
        {
            return item;
        }
    }

    /// <summary>
    ///     A reference to a function given by its index, which element segments without expressions hold.
    /// </summary>
    public class ConstantExpressionFunction : IConstantExpression
    {
        internal ConstantExpressionFunction(uint functionIndex)
        {
            FunctionIndex = functionIndex;
        }

        public uint FunctionIndex { get; }

        public StackValueItem Evaluate(GlobalInstance globalInstance)
        {
            throw new InvalidOperationException("The functions are required to evaluate the reference.");
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> initializingGlobals)
        {
            throw new InvalidOperationException("The functions are required to evaluate the reference.");
        }

        public StackValueItem Evaluate(ReadOnlySpan<Runtime.Global> globals, ReadOnlySpan<IInvocableFunction> functions)
        {
            return StackValueItem.FromReference(ValueType.FuncRef, functions[checked((int)FunctionIndex)]);
        }
    }
}
