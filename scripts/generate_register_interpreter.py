#!/usr/bin/env python3
"""Generates the register operations and the interpreter loop that executes them.

The handlers are described here once so that every backend executes the same operations:

- Runtime/RegisterOp.g.cs: the operations and the tables used by RegisterCompiler
- Runtime/WasmStackFrame.Register.g.cs: the interpreter loop on managed references
- Runtime/NativeInterpreter.g.cs: the interpreter loop on pointers, which is compiled by Burst

Instruction fields: A, B and C are the operands. Slots are indices from the base of the frame.
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
RUNTIME = ROOT / "WaaS.Unity" / "Packages" / "com.ruccho.waas" / "Core" / "Scripts" / "Runtime"

CMP_NAMES = ["Eq", "Ne", "LtS", "LtU", "GtS", "GtU", "LeS", "LeU", "GeS", "GeU"]
CMP_NEGATION = {"Eq": "Ne", "Ne": "Eq", "LtS": "GeS", "LtU": "GeU", "GtS": "LeS", "GtU": "LeU", "LeS": "GtS",
                "LeU": "GtU", "GeS": "LtS", "GeU": "LtU"}
CMP_OPERATOR = {"Eq": "==", "Ne": "!=", "Lt": "<", "Gt": ">", "Le": "<=", "Ge": ">="}

INT_BINARY = {
    "Add": "{l} + {r}", "Sub": "{l} - {r}", "Mul": "{l} * {r}", "And": "{l} & {r}", "Or": "{l} | {r}",
    "Xor": "{l} ^ {r}", "Shl": "{l} << (int){r}", "ShrU": "{l} >> (int){r}",
}
FLOAT_BINARY = {"Add": "+", "Sub": "-", "Mul": "*", "Div": "/"}

UNARY = {
    # name: (result field, result type, expression of the operand slot s)
    "I32WrapI64": ("valueI64", "I32", "s.valueI64 & 0xFFFFFFFF"),
    "I64ExtendI32S": ("valueI64", "I64", "unchecked((ulong)(long)(int)s.valueI32)"),
    "I64ExtendI32U": ("valueI64", "I64", "s.valueI32"),
    "I32ExtendS8": ("valueI64", "I32", "unchecked((uint)(sbyte)(byte)s.valueI32)"),
    "I64Ctz": ("valueI64", "I64", "BitUtils.TrailingZeroCount(s.valueI64)"),
    "I32Eqz": ("valueI64", "I32", "s.valueI32 == 0 ? 1UL : 0UL"),
    "I64Eqz": ("valueI64", "I32", "s.valueI64 == 0 ? 1UL : 0UL"),
    "F32ConvertI32S": ("valueF32", "F32", "(int)s.valueI32"),
    "F32ConvertI32U": ("valueF32", "F32", "s.valueI32"),
    "F32ConvertI64S": ("valueF32", "F32", "(long)s.valueI64"),
    "F32ConvertI64U": ("valueF32", "F32", "s.valueI64"),
    "F64ConvertI32S": ("valueF64", "F64", "(int)s.valueI32"),
    "F64ConvertI32U": ("valueF64", "F64", "s.valueI32"),
    "F64ConvertI64S": ("valueF64", "F64", "(long)s.valueI64"),
    "F64ConvertI64U": ("valueF64", "F64", "s.valueI64"),
    "F32DemoteF64": ("valueF32", "F32", "(float)s.valueF64"),
    "F64PromoteF32": ("valueF64", "F64", "(double)s.valueF32"),
}

LOADS = {
    # name: (bytes, C# type read from the memory, conversion to the value, result type or None for the type in Tag)
    "Load32": (4, "uint", "{v}", None),
    "Load64": (8, "ulong", "{v}", None),
    "I32Load8S": (1, "sbyte", "unchecked((uint){v})", "I32"),
    "I32Load8U": (1, "byte", "{v}", "I32"),
    "I32Load16S": (2, "short", "unchecked((uint){v})", "I32"),
    "I32Load16U": (2, "ushort", "{v}", "I32"),
    "I64Load8S": (1, "sbyte", "unchecked((ulong){v})", "I64"),
    "I64Load8U": (1, "byte", "{v}", "I64"),
    "I64Load16S": (2, "short", "unchecked((ulong){v})", "I64"),
    "I64Load16U": (2, "ushort", "{v}", "I64"),
    "I64Load32S": (4, "int", "unchecked((ulong){v})", "I64"),
    "I64Load32U": (4, "uint", "{v}", "I64"),
}

STORES = {"Store8": (1, "byte"), "Store16": (2, "ushort"), "Store32": (4, "uint"), "Store64": (8, "ulong")}


def cmp_type(width, name):
    if name.endswith("S"):
        return "int" if width == 32 else "long"
    return None


def cmp_expr(width, name, left, right):
    signed = cmp_type(width, name)
    operator = CMP_OPERATOR[name.rstrip("SU") if name not in ("Eq", "Ne") else name]
    if signed:
        return f"unchecked(({signed}){left}) {operator} unchecked(({signed}){right})"
    return f"{left} {operator} {right}"


class Handlers:
    """Writes the operations in terms of the expressions of a backend."""

    def __init__(self, slot, load, store, oob, div0, jump, ctz):
        self.slot, self.load, self.store, self.oob, self.div0, self.jump = slot, load, store, oob, div0, jump
        self.ctz = ctz
        self.names = []
        self.cases = []

    def case(self, name, body):
        self.names.append(name)
        lines = "\n".join("                        " + line for line in body.strip().split("\n"))
        self.cases.append(f"                    case RegisterOp.{name}:\n                    {{\n{lines}\n"
                          f"                        break;\n                    }}\n")

    def jump_case(self, name, condition, target, loop):
        self.names.append(name)
        body = f"if ({condition})\n{{\n"
        for line in self.jump(target, loop):
            body += f"    {line}\n"
        body += "}\n"
        lines = "\n".join("                        " + line for line in body.strip().split("\n"))
        self.cases.append(f"                    case RegisterOp.{name}:\n                    {{\n{lines}\n"
                          f"                        break;\n                    }}\n")

    def build(self):
        s = self.slot
        self.case("Copy", f"{s('A')} = {s('B')};")
        self.case("Const", f"ref var d = ref {s('A')};\nd.valueType = (ValueType)instr.Tag;\nd.valueI64 = instr.Value;")
        self.case("SelectLocal", f"if ({s('C')}.valueI32 == 0) {s('A')} = {s('B')};")

        for width, field in ((32, "valueI32"), (64, "valueI64")):
            type_name = f"I{width}"
            integer = "uint" if width == 32 else "ulong"
            for name, template in INT_BINARY.items():
                expr = template.format(l="l", r="r")
                self.case(f"{type_name}{name}",
                          f"var l = {s('B')}.{field};\nvar r = {s('C')}.{field};\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.{type_name};\nd.valueI64 = unchecked({expr});")
                self.case(f"{type_name}{name}Imm",
                          f"var l = {s('B')}.{field};\nvar r = unchecked(({integer})instr.C);\n"
                          f"ref var d = ref {s('A')};\nd.valueType = ValueType.{type_name};\n"
                          f"d.valueI64 = unchecked({expr});")
            signed = "int" if width == 32 else "long"
            bits = width
            for suffix, read in (("", f"{s('C')}.{field}"), ("Imm", f"unchecked(({integer})instr.C)")):
                self.case(f"{type_name}ShrS{suffix}",
                          f"var l = {s('B')}.{field};\nvar r = {read};\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.{type_name};\n"
                          f"d.valueI64 = unchecked(({integer})(({signed})l >> (int)r));")
                self.case(f"{type_name}Rotl{suffix}",
                          f"var l = {s('B')}.{field};\nvar k = (int)({read});\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.{type_name};\nd.valueI64 = unchecked((l << k) | (l >> ({bits} - k)));")
                self.case(f"{type_name}Rotr{suffix}",
                          f"var l = {s('B')}.{field};\nvar k = (int)({read});\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.{type_name};\nd.valueI64 = unchecked((l >> k) | (l << ({bits} - k)));")
                for name, operator in (("DivU", "/"), ("RemU", "%")):
                    if suffix:
                        continue
                    self.case(f"{type_name}{name}",
                              f"var l = {s('B')}.{field};\nvar r = {s('C')}.{field};\nif (r == 0) {self.div0};\n"
                              f"ref var d = ref {s('A')};\nd.valueType = ValueType.{type_name};\nd.valueI64 = l {operator} r;")

            for name in CMP_NAMES:
                expr = cmp_expr(width, name, "l", "r")
                self.case(f"{type_name}{name}",
                          f"var l = {s('B')}.{field};\nvar r = {s('C')}.{field};\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.I32;\nd.valueI64 = {expr} ? 1UL : 0UL;")
                self.case(f"{type_name}{name}Imm",
                          f"var l = {s('B')}.{field};\nvar r = unchecked(({integer})instr.C);\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.I32;\nd.valueI64 = {expr} ? 1UL : 0UL;")
                for loop in (False, True):
                    suffix = "Loop" if loop else ""
                    self.jump_case(f"BrIf{type_name}{name}{suffix}",
                                   cmp_expr(width, name, f"{s('A')}.{field}", f"{s('B')}.{field}"), "C", loop)
                    self.jump_case(f"BrIf{type_name}{name}Imm{suffix}",
                                   cmp_expr(width, name, f"{s('A')}.{field}", f"unchecked(({integer})instr.B)"),
                                   "C", loop)

        for width, field in ((32, "valueF32"), (64, "valueF64")):
            type_name = f"F{width}"
            for name, operator in FLOAT_BINARY.items():
                self.case(f"{type_name}{name}",
                          f"var l = {s('B')}.{field};\nvar r = {s('C')}.{field};\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.{type_name};\nd.{field} = l {operator} r;")
            for name in ("Eq", "Ne", "Lt", "Gt", "Le", "Ge"):
                self.case(f"{type_name}{name}",
                          f"var l = {s('B')}.{field};\nvar r = {s('C')}.{field};\nref var d = ref {s('A')};\n"
                          f"d.valueType = ValueType.I32;\nd.valueI64 = l {CMP_OPERATOR[name]} r ? 1UL : 0UL;")

        for name, (field, result, expr) in UNARY.items():
            expr = expr.replace("BitUtils.TrailingZeroCount", self.ctz)
            self.case(name, f"ref var s = ref {s('B')};\nvar v = {expr};\nref var d = ref {s('A')};\n"
                            f"d.valueType = ValueType.{result};\nd.{field} = v;")
        self.case("Reinterpret",
                  f"var v = {s('B')}.valueI64;\nref var d = ref {s('A')};\nd.valueType = (ValueType)instr.Tag;\nd.valueI64 = v;")

        for name, (size, type_name, convert, result) in LOADS.items():
            value = convert.format(v=self.load(type_name))
            tag = f"ValueType.{result}" if result else "(ValueType)instr.Tag"
            self.case(name,
                      f"var address = (ulong){s('B')}.valueI32 + (uint)instr.C;\nif (address + {size} > memLen) {self.oob};\n"
                      f"var v = {value};\nref var d = ref {s('A')};\nd.valueType = {tag};\nd.valueI64 = v;")

        for name, (size, type_name) in STORES.items():
            self.case(name,
                      f"var address = (ulong){s('A')}.valueI32 + (uint)instr.C;\nif (address + {size} > memLen) {self.oob};\n"
                      f"{self.store(type_name, f'unchecked(({type_name}){s(chr(66))}.valueI64)')};")

        self.jump_case("Jump", "true", "A", False)
        self.jump_case("JumpLoop", "true", "A", True)
        self.jump_case("BrIf", f"{s('A')}.valueI32 != 0", "B", False)
        self.jump_case("BrIfLoop", f"{s('A')}.valueI32 != 0", "B", True)
        self.jump_case("BrIfZero", f"{s('A')}.valueI32 == 0", "B", False)
        self.jump_case("BrIfZeroLoop", f"{s('A')}.valueI32 == 0", "B", True)


def managed_handlers():
    return Handlers(
        slot=lambda field: f"Unsafe.Add(ref v0, frameBase + instr.{field})",
        load=lambda type_name: f"Unsafe.ReadUnaligned<{type_name}>(ref Unsafe.Add(ref m0, (int)address))",
        store=lambda type_name, value: f"Unsafe.WriteUnaligned(ref Unsafe.Add(ref m0, (int)address), {value})",
        oob="ThrowOutOfBoundsAt(pc)",
        div0="ThrowDivideByZeroAt(pc)",
        jump=lambda target, loop: (["ThrowIfCancellationRequestedAt(context, pc);"] if loop else []) +
                                  [f"pc = instr.{target};", "continue;"],
        ctz="BitUtils.TrailingZeroCount")


def native_handlers():
    return Handlers(
        slot=lambda field: f"fp[instr.{field}]",
        load=lambda type_name: f"*({type_name}*)(m0 + address)",
        store=lambda type_name, value: f"*({type_name}*)(m0 + address) = {value}",
        oob="{ reason = NativeExit.OutOfBounds; goto Leave; }",
        div0="{ reason = NativeExit.DivideByZero; goto Leave; }",
        jump=lambda target, loop: [f"ip = code + instr.{target};"] +
                                  (["if (--poll == 0) { reason = NativeExit.Poll; goto Leave; }"] if loop else []) +
                                  ["continue;"],
        ctz="TrailingZeroCount")


def write_ops(handlers):
    special = ["Nop", "Return", "Call", "CallIndirect", "BrTable", "GlobalGet", "GlobalSet", "Fallback"]
    names = special + handlers.names
    if len(names) > 256:
        raise SystemExit(f"too many operations: {len(names)}")

    enum = ",\n".join(f"        {name}" for name in names)

    def table_case(op, value):
        return f"                case Op.{op}: return RegisterOp.{value};"

    binary, immediate, compare, unary = [], [], [], []
    for type_name in ("I32", "I64"):
        for name in list(INT_BINARY) + ["ShrS", "Rotl", "Rotr"]:
            binary.append(table_case(f"{type_name}{name}", f"{type_name}{name}"))
            immediate.append(f"                case RegisterOp.{type_name}{name}: return RegisterOp.{type_name}{name}Imm;")
        for name in ("DivU", "RemU"):
            binary.append(table_case(f"{type_name}{name}", f"{type_name}{name}"))
        for name in CMP_NAMES:
            binary.append(table_case(f"{type_name}{name}", f"{type_name}{name}"))
            immediate.append(f"                case RegisterOp.{type_name}{name}: return RegisterOp.{type_name}{name}Imm;")
            for suffix, jump_suffix in (("", ""), ("Imm", "Imm")):
                source = f"RegisterOp.{type_name}{name}{suffix}"
                negated = f"{type_name}{CMP_NEGATION[name]}"
                compare.append(
                    f"                case {source}:\n"
                    f"                    return negate\n"
                    f"                        ? loop ? RegisterOp.BrIf{negated}{jump_suffix}Loop : RegisterOp.BrIf{negated}{jump_suffix}\n"
                    f"                        : loop ? RegisterOp.BrIf{type_name}{name}{jump_suffix}Loop : RegisterOp.BrIf{type_name}{name}{jump_suffix};")
    for type_name in ("F32", "F64"):
        for name in list(FLOAT_BINARY) + ["Eq", "Ne", "Lt", "Gt", "Le", "Ge"]:
            binary.append(table_case(f"{type_name}{name}", f"{type_name}{name}"))
    for name in UNARY:
        unary.append(table_case(name, name))

    no_result = [f"                case RegisterOp.{name}:" for name in names if
                 name in special or name in STORES or name == "SelectLocal" or name.startswith(("BrIf", "Jump"))]
    no_result = [line for line in no_result if not line.endswith(".GlobalGet:")]

    (RUNTIME / "RegisterOp.g.cs").write_text(f'''// <auto-generated>generated by scripts/generate_register_interpreter.py</auto-generated>
namespace WaaS.Runtime
{{
    /// <summary>
    ///     An operation of the register code. See <see cref="RegisterCompiler" />.
    /// </summary>
    internal enum RegisterOp : byte
    {{
{enum}
    }}

    internal static class RegisterOpTable
    {{
        /// <summary>
        ///     Gets the operation of a numeric instruction which takes two slots and writes a slot.
        /// </summary>
        public static bool TryGetBinary(Op op, out RegisterOp result)
        {{
            result = GetBinary(op);
            return result != RegisterOp.Nop;
        }}

        private static RegisterOp GetBinary(Op op)
        {{
            switch (op)
            {{
{chr(10).join(binary)}
                default: return RegisterOp.Nop;
            }}
        }}

        /// <summary>
        ///     Gets the operation which takes the second operand as an immediate, if any.
        /// </summary>
        public static bool TryGetImmediate(RegisterOp op, out RegisterOp result)
        {{
            result = GetImmediate(op);
            return result != RegisterOp.Nop;
        }}

        private static RegisterOp GetImmediate(RegisterOp op)
        {{
            switch (op)
            {{
{chr(10).join(immediate)}
                default: return RegisterOp.Nop;
            }}
        }}

        /// <summary>
        ///     Gets the operation of an instruction which takes a slot and writes a slot.
        /// </summary>
        public static bool TryGetUnary(Op op, out RegisterOp result)
        {{
            result = GetUnary(op);
            return result != RegisterOp.Nop;
        }}

        private static RegisterOp GetUnary(Op op)
        {{
            switch (op)
            {{
{chr(10).join(unary)}
                default: return RegisterOp.Nop;
            }}
        }}

        /// <summary>
        ///     Gets the branch on the result of a comparison, if the operation is a comparison of integers.
        /// </summary>
        public static bool TryGetCompareBranch(RegisterOp op, bool negate, bool loop, out RegisterOp result)
        {{
            result = GetCompareBranch(op, negate, loop);
            return result != RegisterOp.Nop;
        }}

        private static RegisterOp GetCompareBranch(RegisterOp op, bool negate, bool loop)
        {{
            switch (op)
            {{
{chr(10).join(compare)}
                default: return RegisterOp.Nop;
            }}
        }}

        /// <summary>
        ///     Gets whether the operation only writes a result to the slot A.
        /// </summary>
        public static bool WritesResult(RegisterOp op)
        {{
            switch (op)
            {{
{chr(10).join(no_result)}
                    return false;
                default:
                    return true;
            }}
        }}
    }}
}}
''', encoding="utf-8", newline="")


def write_managed_loop(handlers):
    cases = "".join(handlers.cases)
    (RUNTIME / "WasmStackFrame.Register.g.cs").write_text(f'''// <auto-generated>generated by scripts/generate_register_interpreter.py</auto-generated>
using System;
using System.Runtime.CompilerServices;

namespace WaaS.Runtime
{{
    public partial class WasmStackFrame
    {{
        /// <summary>
        ///     The interpreter loop of the register code. It leaves a function to <see cref="Run" /> at the start of a
        ///     basic block when the fuel runs out, where the operand stack is in the same state.
        /// </summary>
        private StackFrameState RunRegister(in Waker waker)
        {{
            // the locals are kept few for the register allocation
            var context = Context;
            var frameBase = this.frameBase;
            var pc = compiled.EntryIndices[programCounter];

            // the bounds are not checked on the hot path. the compiler ensures that the slots are within the buffer
            ref var v0 = ref values[0];
            ref var c0 = ref compiled.RegisterCode[0];

            ref var m0 = ref EmptyMemory[0];
            var memLen = 0UL;
            if (memory != null && memory.Buffer.Length != 0)
            {{
                m0 = ref memory.Buffer[0];
                memLen = (ulong)memory.Length;
            }}

            while (true)
            {{
                ref var instr = ref Unsafe.Add(ref c0, pc);

                // the fuel is consumed only at the start of each basic block
                int cost = instr.Cost;
                if (cost != 0 && (context.fuel -= cost) < 0)
                {{
                    context.fuel += cost;
                    ref var block = ref compiled.GetBlock(pc);
                    programCounter = (uint)block.Index;
                    stackPointer = stackBase + block.Height;
                    return Run(waker);
                }}

                IInvocableFunction callee;
                int numParameters;
                switch (instr.Op)
                {{
                    case RegisterOp.Nop:
                        break;
                    case RegisterOp.Return:
                        goto Return;
                    case RegisterOp.Call:
                        callee = functions[instr.B];
                        numParameters = instr.C;
                        goto Call;
                    case RegisterOp.CallIndirect:
                        programCounter = (uint)compiled.Origins[pc];
                        callee = GetIndirectCallee((uint)instr.B, (uint)(instr.Tag),
                            Unsafe.Add(ref v0, frameBase + instr.C).valueI32);
                        numParameters = callee.Type.ParameterTypes.Length;
                        goto Call;
                    case RegisterOp.BrTable:
                    {{
                        var i = Unsafe.Add(ref v0, frameBase + instr.A).valueI32;
                        var index = instr.B + (i < (uint)instr.C ? (int)i : instr.C);
                        pc = compiled.RegisterBranchTargets[index];
                        continue;
                    }}
                    case RegisterOp.GlobalGet:
                        Unsafe.Add(ref v0, frameBase + instr.A) = globals[instr.B].stackValue;
                        break;
                    case RegisterOp.GlobalSet:
                        globals[instr.A].stackValue = Unsafe.Add(ref v0, frameBase + instr.B);
                        break;
                    case RegisterOp.Fallback:
                    {{
                        // instructions without a dedicated operation, which work on the operand stack
                        var index = compiled.Origins[pc];
                        programCounter = (uint)index;
                        stackPointer = stackBase + instr.A;
                        Function.function.Body.Instructions.Span[index].Execute(this);

                        if (isFramePushed)
                        {{
                            programCounter = (uint)(index + 1);
                            return StackFrameState.Ready;
                        }}

                        if (isEnd) return StackFrameState.Completed;
                        if (memory != null && memory.Buffer.Length != 0)
                        {{
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }}

                        break;
                    }}
{cases}
                    default:
                        throw new InvalidOperationException();
                }}

                pc++;
                continue;

                Call:
                {{
                    var origin = compiled.Origins[pc];
                    programCounter = (uint)origin;
                    // the calls in this frame count as frames of the context
                    if (context.FrameCount + activationCount >= context.MaxStackFrameCount) ThrowStackOverflow();

                    var sp = stackBase + instr.A;
                    if (callee is InstanceFunction wasmCallee)
                    {{
                        // the cancellation is observed on calls as well as loops, to abort a deep recursion
                        ThrowIfCancellationRequested(context, origin);
                        EnterFunction(wasmCallee, origin + 1, sp - numParameters);
                        c0 = ref compiled.RegisterCode[0];
                        v0 = ref values[0];
                        frameBase = this.frameBase;
                        pc = 0;
                        if (memory != null && memory.Buffer.Length != 0)
                        {{
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }}

                        continue;
                    }}

                    // functions of the host run in their own frames, which are moved here without going back to the
                    // execution context. this frame continues if the callee completes synchronously.
                    sp -= numParameters;
                    stackPointer = sp;
                    if (TryInvokeDirectly(callee, sp, numParameters, out var pendingCore))
                    {{
                        pc++;
                        if (memory != null && memory.Buffer.Length != 0)
                        {{
                            m0 = ref memory.Buffer[0];
                            memLen = (ulong)memory.Length;
                        }}

                        continue;
                    }}

                    isFramePushed = true;
                    var frame = pendingCore != null
                        ? context.PushFrame(new StackFrame(pendingCore))
                        : context.PushFrameFromFrame(callee,
                            new ReadOnlySpan<StackValueItem>(values, sp, numParameters));
                    programCounter = (uint)(origin + 1);

                    var state = MovePushedFrame(frame, waker);
                    if (state != StackFrameState.Completed) return state;

                    // the results are pushed by the completed frame
                    isFramePushed = false;
                    pc++;
                    if (memory != null && memory.Buffer.Length != 0)
                    {{
                        m0 = ref memory.Buffer[0];
                        memLen = (ulong)memory.Length;
                    }}

                    continue;
                }}

                Return:
                {{
                    var sp = stackBase + instr.A;
                    if (activationCount == 0)
                    {{
                        isEnd = true;
                        stackPointer = sp;
                        return StackFrameState.Completed;
                    }}

                    // the locals are not passed by reference to keep them in registers
                    var returnIndex = ReturnToCaller(sp);
                    c0 = ref compiled.RegisterCode[0];
                    pc = compiled.EntryIndices[returnIndex];
                    frameBase = this.frameBase;
                    if (memory != null && memory.Buffer.Length != 0)
                    {{
                        m0 = ref memory.Buffer[0];
                        memLen = (ulong)memory.Length;
                    }}
                }}
            }}
        }}

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowOutOfBoundsAt(int pc)
        {{
            ThrowOutOfBounds(compiled.Origins[pc]);
        }}

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowDivideByZeroAt(int pc)
        {{
            ThrowDivideByZero(compiled.Origins[pc]);
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfCancellationRequestedAt(ExecutionContext context, int pc)
        {{
            if (context.CancellationToken.IsCancellationRequested)
                ThrowIfCancellationRequested(context, compiled.Origins[pc]);
        }}
    }}
}}
''', encoding="utf-8", newline="")


def write_native_loop(handlers):
    cases = "".join(handlers.cases)
    (RUNTIME / "NativeInterpreter.g.cs").write_text(f'''// <auto-generated>generated by scripts/generate_register_interpreter.py</auto-generated>
#if WAAS_NATIVE_INTERPRETER
#if WAAS_BURST
using Unity.Burst;
#endif

namespace WaaS.Runtime
{{
#if WAAS_BURST
    [BurstCompile]
#endif
    internal static unsafe partial class NativeInterpreter
    {{
        private static ulong TrailingZeroCount(ulong value)
        {{
            if (value == 0) return 64;
            value = (value & (0 - value)) - 1;
            value -= (value >> 1) & 0x5555555555555555UL;
            value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
            value = (value + (value >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return unchecked(value * 0x0101010101010101UL) >> 56;
        }}

        /// <summary>
        ///     The interpreter loop of the register code. It runs until something needs the managed side, and returns the
        ///     reason as <see cref="NativeExit" />.
        /// </summary>
#if WAAS_BURST
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
#endif
        public static int Run(NativeFrameState* state)
        {{
            var function = state->Function;
            var functions = function->Instance->Functions;
            var globals = function->Instance->Globals;
            var code = function->Code;
            var ip = state->Ip;
            var v0 = state->Values;
            var fp = v0 + state->FrameBase;
            var m0 = state->Memory;
            var memLen = state->MemoryLength;
            var fuel = state->Fuel;
            var poll = state->Poll;
            var activations = state->Activations;
            var activationCount = state->ActivationCount;
            int reason;

            while (true)
            {{
                ref var instr = ref *ip;

                // the fuel is consumed only at the start of each basic block
                int cost = instr.Cost;
                if (cost != 0 && (fuel -= cost) < 0)
                {{
                    fuel += cost;
                    reason = NativeExit.OutOfFuel;
                    goto Leave;
                }}

                switch (instr.Op)
                {{
                    case RegisterOp.Nop:
                        break;
                    case RegisterOp.Return:
                    {{
                        if (activationCount == 0)
                        {{
                            reason = NativeExit.Completed;
                            goto Leave;
                        }}

                        ref var caller = ref activations[activationCount - 1];
                        if (caller.Function->Instance != function->Instance)
                        {{
                            reason = NativeExit.ReturnToAnotherInstance;
                            goto Leave;
                        }}

                        var numResults = function->NumResults;
                        var source = fp + function->NumLocals + instr.A - numResults;
                        if (source != fp)
                            for (var i = 0; i < numResults; i++)
                                fp[i] = source[i];

                        activationCount--;
                        function = caller.Function;
                        code = function->Code;
                        ip = code + caller.ReturnPc + 1;
                        fp = v0 + caller.FrameBase;
                        continue;
                    }}
                    case RegisterOp.Call:
                    {{
                        var callee = functions + instr.B;
                        if (callee->Code == null)
                        {{
                            state->ExitArgument = instr.B;
                            reason = NativeExit.Call;
                            goto Leave;
                        }}

                        var calleeFrame = fp + function->NumLocals + instr.A - instr.C;
                        if (activationCount >= state->ActivationLimit ||
                            (calleeFrame - v0) + callee->NumLocals + callee->MaxStackDepth > state->Capacity)
                        {{
                            reason = NativeExit.Grow;
                            goto Leave;
                        }}

                        // the cancellation is observed on calls as well as loops, to abort a deep recursion
                        if (--poll == 0)
                        {{
                            reason = NativeExit.Poll;
                            goto Leave;
                        }}

                        var initial = callee->InitialLocals;
                        for (var i = callee->NumParameters; i < callee->NumLocals; i++) calleeFrame[i] = initial[i];

                        ref var activation = ref activations[activationCount++];
                        activation.Function = function;
                        activation.ReturnPc = (int)(ip - code);
                        activation.FrameBase = (int)(fp - v0);

                        function = callee;
                        code = callee->Code;
                        ip = code;
                        fp = calleeFrame;
                        continue;
                    }}
                    case RegisterOp.CallIndirect:
                        reason = NativeExit.CallIndirect;
                        goto Leave;
                    case RegisterOp.BrTable:
                    {{
                        var i = fp[instr.A].valueI32;
                        var index = instr.B + (i < (uint)instr.C ? (int)i : instr.C);
                        ip = code + function->BranchTargets[index];
                        continue;
                    }}
                    case RegisterOp.GlobalGet:
                        fp[instr.A] = *globals[instr.B];
                        break;
                    case RegisterOp.GlobalSet:
                        *globals[instr.A] = fp[instr.B];
                        break;
                    case RegisterOp.Fallback:
                        reason = NativeExit.Fallback;
                        goto Leave;
{cases}
                    default:
                        reason = NativeExit.InvalidOperation;
                        goto Leave;
                }}

                ip++;
                continue;

                Leave:
                state->Function = function;
                state->Ip = ip;
                state->FrameBase = (int)(fp - v0);
                state->Fuel = fuel;
                state->Poll = poll;
                state->ActivationCount = activationCount;
                return reason;
            }}
        }}
    }}
}}
#endif
''', encoding="utf-8", newline="")


def main():
    handlers = managed_handlers()
    handlers.build()
    write_ops(handlers)
    write_managed_loop(handlers)

    native = native_handlers()
    native.build()
    write_native_loop(native)
    print(f"{len(handlers.names) + 8} operations")


if __name__ == "__main__":
    main()
