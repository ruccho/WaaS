using System;
using System.Collections.Generic;
using System.Text;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     A snapshot of the stack frames of an <see cref="ExecutionContext" /> at the time an exception occurred.
    ///     The first frame is the innermost one.
    /// </summary>
    [Serializable]
    public sealed class WasmStackTrace
    {
        private const string DataKey = "WaaS.WasmStackTrace";

        private string text;

        private WasmStackTrace(WasmStackTraceFrame[] frames)
        {
            Frames = frames;
        }

        public IReadOnlyList<WasmStackTraceFrame> Frames { get; }

        /// <summary>
        ///     Finds the stack trace attached to the exception or its inner exceptions.
        /// </summary>
        /// <returns>The stack trace, or null if no stack trace is attached.</returns>
        public static WasmStackTrace FromException(Exception exception)
        {
            for (var ex = exception; ex != null; ex = ex.InnerException)
            {
                var trace = GetAttached(ex);
                if (trace != null) return trace;
            }

            return null;
        }

        private static WasmStackTrace GetAttached(Exception exception)
        {
            switch (exception)
            {
                case TrapException trap:
                    return trap.WasmStackTrace;
                case WasmException wasm:
                    return wasm.WasmStackTrace;
                default:
                    try
                    {
                        return exception.Data[DataKey] as WasmStackTrace;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
            }
        }

        /// <summary>
        ///     Captures the frames and attaches them to the exception unless a stack trace is already attached.
        ///     This is called only when an exception is thrown, so it does not affect the normal execution.
        /// </summary>
        internal static void Attach(Exception exception, Stack<StackFrame> frames)
        {
            if (exception == null || frames.Count == 0) return;
            if (GetAttached(exception) != null) return;

            var trace = Capture(frames);

            switch (exception)
            {
                case TrapException trap:
                    trap.WasmStackTrace = trace;
                    break;
                case WasmException wasm:
                    wasm.WasmStackTrace = trace;
                    break;
                default:
                    try
                    {
                        exception.Data[DataKey] = trace;
                    }
                    catch (Exception)
                    {
                        // the stack trace is not essential
                    }

                    break;
            }
        }

        private static WasmStackTrace Capture(Stack<StackFrame> frames)
        {
            var result = new List<WasmStackTraceFrame>(frames.Count);
            var isInnermost = true;
            // Stack<T> enumerates from the top
            foreach (var frame in frames)
            {
                if (frame.Core is WasmStackFrame { Function: not null } wasmFrame)
                    // a frame of WebAssembly contains the functions called in it
                    wasmFrame.CaptureStackTrace(result, isInnermost);
                else
                    result.Add(WasmStackTraceFrame.CaptureHost(frame.Core));
                isInnermost = false;
            }

            return new WasmStackTrace(result.ToArray());
        }

        public override string ToString()
        {
            if (text != null) return text;

            var builder = new StringBuilder();
            builder.Append("wasm backtrace:");
            for (var i = 0; i < Frames.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  ");
                builder.Append(i.ToString().PadLeft(3));
                builder.Append(": ");
                Frames[i].AppendTo(builder);
            }

            return text = builder.ToString();
        }
    }

    /// <summary>
    ///     A frame in <see cref="WasmStackTrace" />.
    /// </summary>
    [Serializable]
    public sealed class WasmStackTraceFrame
    {
        private WasmStackTraceFrame()
        {
        }

        /// <summary>
        ///     Whether the frame is a function implemented by the host, not a WebAssembly function.
        /// </summary>
        public bool IsHost { get; private set; }

        /// <summary>
        ///     Description of the host frame. Null for a WebAssembly frame.
        /// </summary>
        public string HostDescription { get; private set; }

        /// <summary>
        ///     <see cref="Module.SourceDescription" /> of the module.
        /// </summary>
        public string ModuleSourceDescription { get; private set; }

        /// <summary>
        ///     Name of the module in the name section, or null if it is not specified.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        ///     Index of the function in the function index space of the module, which includes the imported functions.
        /// </summary>
        public uint? FunctionIndex { get; private set; }

        /// <summary>
        ///     Name of the function in the name section, or null if it is not specified.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        ///     Index of the instruction being executed in the function body.
        ///     For the frames other than the innermost one, this is the instruction which called the next frame.
        /// </summary>
        public uint? InstructionIndex { get; private set; }

        /// <summary>
        ///     Byte offset of the instruction from the start of the module binary.
        /// </summary>
        public uint? Offset { get; private set; }

        internal static WasmStackTraceFrame CaptureWasm(InstanceFunction function, Instruction instruction)
        {
            var module = function.instance.Module;
            var functionIndex = checked((uint)(module.ImportedFunctionCount + function.function.FunctionIndex));

            string moduleName = null;
            string functionName = null;
            try
            {
                var names = module.Names;
                moduleName = names.ModuleName;
                names.TryGetFunctionName(functionIndex, out functionName);
            }
            catch (Exception)
            {
                // the names are not essential
            }

            return new WasmStackTraceFrame
            {
                ModuleSourceDescription = module.SourceDescription,
                ModuleName = moduleName,
                FunctionIndex = functionIndex,
                FunctionName = functionName,
                InstructionIndex = instruction?.Index,
                Offset = instruction?.Offset
            };
        }

        internal static WasmStackTraceFrame CaptureHost(IStackFrameCore core)
        {
            string description;
            try
            {
                description = core?.ToString();
            }
            catch (Exception)
            {
                description = core?.GetType().FullName;
            }

            return new WasmStackTraceFrame
            {
                IsHost = true,
                HostDescription = description
            };
        }

        internal void AppendTo(StringBuilder builder)
        {
            if (IsHost)
            {
                builder.Append("<host> ");
                builder.Append(HostDescription);
                return;
            }

            builder.Append(Offset.HasValue ? $"0x{Offset.Value:x}" : "<unknown offset>");
            builder.Append(" - ");

            var moduleName = ModuleName ?? ModuleSourceDescription;
            if (!string.IsNullOrEmpty(moduleName))
            {
                builder.Append(moduleName);
                builder.Append('!');
            }

            builder.Append(FunctionName ?? $"<wasm function {FunctionIndex}>");

            if (InstructionIndex.HasValue)
            {
                builder.Append(" (function ");
                builder.Append(FunctionIndex);
                builder.Append(", instruction ");
                builder.Append(InstructionIndex.Value);
                builder.Append(')');
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            AppendTo(builder);
            return builder.ToString();
        }
    }
}
