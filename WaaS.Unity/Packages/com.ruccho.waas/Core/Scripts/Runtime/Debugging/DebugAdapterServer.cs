using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using WaaS.Models;

namespace WaaS.Runtime.Debugging
{
    /// <summary>
    ///     A server of the Debug Adapter Protocol (DAP), to which IDEs such as VS Code attach over TCP. While a client is
    ///     connected, the server is set to <see cref="ExecutionContext.DefaultDebugger" />, so that the contexts created
    ///     after that can be debugged.
    /// </summary>
    /// <remarks>
    ///     The breakpoints are resolved to the instructions with the line tables of DWARF (<see cref="Module.LineTable" />).
    ///     The source paths in the line tables relative to the directory of the compilation are resolved from
    ///     <see cref="SourceRoot" />. The steps stop only at the lines of the files in <see cref="SourceRoot" />, and skip
    ///     the libraries such as the standard library.
    /// </remarks>
    public sealed class DebugAdapterServer : IWasmDebugger, IDisposable
    {
        private readonly object locker = new();
        private readonly TcpListener listener;
        private readonly Thread acceptThread;

        // the state of the debugging session, guarded by the locker
        private Connection connection;
        // the breakpoints of each source file, by the normalized path
        private readonly Dictionary<string, List<Breakpoint>> breakpoints = new();
        private int nextBreakpointId = 1;
        // the breakpoints moved to other lines, which are reported to the client
        private readonly List<Breakpoint> changedBreakpoints = new();
        private int breakpointsVersion;
        private readonly ConditionalWeakTable<Module, ModuleBreakpoints> moduleBreakpoints = new();
        private readonly ConditionalWeakTable<Module, ModuleSources> moduleSources = new();
        private readonly ConditionalWeakTable<ExecutionContext, ThreadId> threadIds = new();
        private readonly Dictionary<int, WeakReference<ExecutionContext>> threads = new();
        private readonly Dictionary<int, WasmDebugPause> pauses = new();
        private readonly Dictionary<int, Step> steps = new();
        private readonly Dictionary<int, Func<List<object>>> variables = new();
        private int nextThreadId = 1;
        private int nextVariablesReference = 1;
        private bool isPauseRequested;
        private string pauseReason;

        public DebugAdapterServer(int port, string sourceRoot)
        {
            SourceRoot = Path.GetFullPath(sourceRoot);
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            acceptThread = new Thread(Accept) { IsBackground = true, Name = "WaaS Debug Adapter" };
            acceptThread.Start();
        }

        public int Port { get; }

        /// <summary>
        ///     The directory that the relative source paths in the line tables are resolved from, such as the root of the
        ///     Unity project where cargo runs.
        /// </summary>
        public string SourceRoot { get; }

        /// <summary>
        ///     Whether a client is connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (locker)
                {
                    return connection != null;
                }
            }
        }

        public void Dispose()
        {
            listener.Stop();
            Connection current;
            lock (locker)
            {
                current = connection;
            }

            current?.Close();
        }

        #region IWasmDebugger

        public bool ShouldPause(in WasmDebugLocation location)
        {
            bool shouldPause;
            lock (locker)
            {
                shouldPause = ShouldPauseCore(location);
            }

            ReportChangedBreakpoints();
            return shouldPause;
        }

        private bool ShouldPauseCore(in WasmDebugLocation location)
        {
            if (connection == null) return false;
            var threadId = GetThreadId(location.Context);

            if (isPauseRequested)
            {
                isPauseRequested = false;
                pauseReason = "pause";
                return true;
            }

            if (steps.TryGetValue(threadId, out var step) && step.ShouldPause(this, location))
            {
                pauseReason = "step";
                return true;
            }

            if (breakpoints.Count == 0) return false;
            var module = location.Function.instance.Module;
            var resolved = moduleBreakpoints.GetValue(module, _ => new ModuleBreakpoints());
            if (resolved.Version != breakpointsVersion) ResolveBreakpoints(module, resolved);
            if (!resolved.Offsets.Contains(location.Instruction.Offset)) return false;
            pauseReason = "breakpoint";
            return true;
        }

        public void OnPaused(WasmDebugPause pause)
        {
            Connection current;
            int threadId;
            string reason;
            lock (locker)
            {
                threadId = GetThreadId(pause.Context);
                pauses[threadId] = pause;
                steps.Remove(threadId);
                current = connection;
                reason = pauseReason ?? "pause";
                pauseReason = null;
            }

            if (current == null)
            {
                pause.Resume();
                return;
            }

            current.SendEvent("stopped", new Dictionary<string, object>
            {
                ["reason"] = reason, ["threadId"] = threadId, ["allThreadsStopped"] = false
            });
        }

        #endregion

        #region Sources

        private void ResolveBreakpoints(Module module, ModuleBreakpoints resolved)
        {
            resolved.Version = breakpointsVersion;
            resolved.Offsets.Clear();
            var table = module.LineTable;
            if (table == null) return;

            var sources = GetSources(module);

            // the instructions of the statements of each line in the files with breakpoints
            var lines = new Dictionary<int, SortedDictionary<int, List<uint>>>();
            foreach (var row in table.Rows)
            {
                if (!row.IsStatement || row.FileIndex < 0 || row.Line <= 0) continue;
                if (!breakpoints.ContainsKey(sources.NormalizedPaths[row.FileIndex])) continue;

                // the address of a row may not be at the start of an instruction, such as the start of a function
                var index = Array.BinarySearch(sources.InstructionOffsets, row.Offset);
                if (index < 0) index = ~index;
                if (index >= sources.InstructionOffsets.Length ||
                    sources.InstructionOffsets[index] >= Math.Max(row.EndOffset, row.Offset + 1)) continue;

                if (!lines.TryGetValue(row.FileIndex, out var fileLines))
                    lines[row.FileIndex] = fileLines = new SortedDictionary<int, List<uint>>();
                if (!fileLines.TryGetValue(row.Line, out var offsets)) fileLines[row.Line] = offsets = new List<uint>();
                offsets.Add(sources.InstructionOffsets[index]);
            }

            foreach (var (fileIndex, fileLines) in lines)
            foreach (var breakpoint in breakpoints[sources.NormalizedPaths[fileIndex]])
            {
                // a line without instructions, such as a line optimized out, breaks at the next line with them
                var found = fileLines.Keys.Where(l => l >= breakpoint.Line).DefaultIfEmpty(0).First();
                if (found == 0) continue;
                resolved.Offsets.UnionWith(fileLines[found]);
                if (breakpoint.ResolvedLine == found) continue;
                breakpoint.ResolvedLine = found;
                changedBreakpoints.Add(breakpoint);
            }
        }

        private void ReportChangedBreakpoints()
        {
            // checked without the lock, since this is called for every instruction
            if (changedBreakpoints.Count == 0) return;

            Connection current;
            Breakpoint[] changed;
            lock (locker)
            {
                if (changedBreakpoints.Count == 0) return;
                changed = changedBreakpoints.ToArray();
                changedBreakpoints.Clear();
                current = connection;
            }

            foreach (var breakpoint in changed)
                current?.SendEvent("breakpoint", new Dictionary<string, object>
                {
                    ["reason"] = "changed", ["breakpoint"] = breakpoint.ToJson()
                });
        }

        private ModuleSources GetSources(Module module)
        {
            return moduleSources.GetValue(module, m =>
            {
                var table = m.LineTable;
                var paths = table.Files.Select(ResolvePath).ToArray();
                return new ModuleSources
                {
                    Paths = paths,
                    NormalizedPaths = paths.Select(NormalizePath).ToArray(),
                    // the libraries such as the standard library are skipped by the steps
                    IsUserCode = paths.Select(path => File.Exists(path) && IsInSourceRoot(path)).ToArray(),
                    InstructionOffsets = m.InternalFunctions.ToArray()
                        .SelectMany(f => f.Body.Instructions.ToArray().Select(i => i.Offset)).OrderBy(o => o)
                        .ToArray()
                };
            });
        }

        private bool IsInSourceRoot(string path)
        {
            var root = NormalizePath(SourceRoot).TrimEnd('/') + "/";
            return NormalizePath(path).StartsWith(root, StringComparison.Ordinal);
        }

        private string ResolvePath(string path)
        {
            return Path.GetFullPath(Path.Combine(SourceRoot, path.Replace('\\', '/')));
        }

        private static string NormalizePath(string path)
        {
            var normalized = Path.GetFullPath(path).Replace('\\', '/');
            // paths are case-insensitive on Windows, and the drive letters of VS Code are lower case
            return Path.DirectorySeparatorChar == '\\' ? normalized.ToLowerInvariant() : normalized;
        }

        private bool TryGetLine(WasmDebugLocation location, out int file, out int line)
        {
            return TryGetLine(location.Function, location.Instruction, out file, out line);
        }

        // finds the line in a source file in the source root, such as a script, and not the libraries
        private bool TryGetLine(InstanceFunction function, Instruction instruction, out int file, out int line)
        {
            var module = function.instance.Module;
            var table = module.LineTable;
            if (table != null && instruction != null && table.TryFind(instruction.Offset, out var row) && row.Line > 0 &&
                row.FileIndex >= 0 && GetSources(module).IsUserCode[row.FileIndex])
            {
                file = row.FileIndex;
                line = row.Line;
                return true;
            }

            file = -1;
            line = 0;
            return false;
        }

        #endregion

        #region Threads

        private int GetThreadId(ExecutionContext context)
        {
            if (threadIds.TryGetValue(context, out var id)) return id.Value;
            id = new ThreadId(nextThreadId++);
            threadIds.Add(context, id);
            threads[id.Value] = new WeakReference<ExecutionContext>(context);
            return id.Value;
        }

        #endregion

        #region Protocol

        private void Accept()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    // stopped
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                var newConnection = new Connection(this, client);
                lock (locker)
                {
                    if (connection != null)
                    {
                        // only one client is debugging at a time
                        newConnection.Close();
                        continue;
                    }

                    connection = newConnection;
                }

                newConnection.Run();
            }
        }

        private void OnDisconnected(Connection closed)
        {
            List<WasmDebugPause> resumed;
            lock (locker)
            {
                if (connection != closed) return;
                connection = null;
                breakpoints.Clear();
                breakpointsVersion++;
                steps.Clear();
                variables.Clear();
                isPauseRequested = false;
                resumed = pauses.Values.ToList();
                pauses.Clear();
                if (ExecutionContext.DefaultDebugger == this) ExecutionContext.DefaultDebugger = null;
            }

            foreach (var pause in resumed) pause.Resume();
        }

        private object Handle(Connection client, string command, Dictionary<string, object> arguments)
        {
            switch (command)
            {
                case "initialize":
                    client.SendEventAfterResponse("initialized", null);
                    return new Dictionary<string, object>
                    {
                        ["supportsConfigurationDoneRequest"] = true,
                        ["supportsSingleThreadExecutionRequests"] = true
                    };
                case "attach":
                case "launch":
                    ExecutionContext.DefaultDebugger = this;
                    return null;
                case "configurationDone":
                    return null;
                case "setBreakpoints":
                    return SetBreakpoints(arguments);
                case "setExceptionBreakpoints":
                    return new Dictionary<string, object> { ["breakpoints"] = new List<object>() };
                case "threads":
                    return Threads();
                case "stackTrace":
                    return StackTrace(arguments);
                case "scopes":
                    return Scopes(arguments);
                case "variables":
                    return Variables(arguments);
                case "continue":
                    Resume(GetInt(arguments, "threadId"), null);
                    return new Dictionary<string, object> { ["allThreadsContinued"] = false };
                case "next":
                    Resume(GetInt(arguments, "threadId"), StepKind.Over);
                    return null;
                case "stepIn":
                    Resume(GetInt(arguments, "threadId"), StepKind.In);
                    return null;
                case "stepOut":
                    Resume(GetInt(arguments, "threadId"), StepKind.Out);
                    return null;
                case "pause":
                    lock (locker)
                    {
                        isPauseRequested = true;
                    }

                    return null;
                case "disconnect":
                case "terminate":
                    client.CloseAfterResponse();
                    return null;
                default:
                    throw new NotSupportedException($"The request \"{command}\" is not supported.");
            }
        }

        private object SetBreakpoints(Dictionary<string, object> arguments)
        {
            var source = (Dictionary<string, object>)arguments["source"];
            var path = NormalizePath((string)source["path"]);
            var requested = arguments.TryGetValue("breakpoints", out var list) && list is List<object> items
                ? items.Cast<Dictionary<string, object>>().Select(b => GetInt(b, "line")).ToList()
                : new List<int>();

            List<Breakpoint> created;
            lock (locker)
            {
                created = requested.Select(line => new Breakpoint(nextBreakpointId++, line)).ToList();
                if (created.Count == 0) breakpoints.Remove(path);
                else breakpoints[path] = created;
                breakpointsVersion++;
            }

            // the scripts are loaded later, so the breakpoints are reported as verified, and the lines are corrected
            // when they are resolved
            return new Dictionary<string, object>
            {
                ["breakpoints"] = created.Select(b => (object)b.ToJson()).ToList()
            };
        }

        private object Threads()
        {
            var result = new List<object>();
            lock (locker)
            {
                foreach (var (id, reference) in threads.ToArray())
                    if (!reference.TryGetTarget(out _))
                        threads.Remove(id);
                    else
                        result.Add(new Dictionary<string, object>
                        {
                            ["id"] = id, ["name"] = pauses.ContainsKey(id) ? $"WebAssembly {id} (paused)" : $"WebAssembly {id}"
                        });
            }

            // the clients require at least one thread
            if (result.Count == 0)
                result.Add(new Dictionary<string, object> { ["id"] = 0, ["name"] = "WebAssembly" });

            return new Dictionary<string, object> { ["threads"] = result };
        }

        private object StackTrace(Dictionary<string, object> arguments)
        {
            var threadId = GetInt(arguments, "threadId");
            var frames = new List<object>();
            lock (locker)
            {
                if (pauses.TryGetValue(threadId, out var pause))
                    for (var i = 0; i < pause.Frames.Count; i++)
                    {
                        var frame = pause.Frames[i];
                        var result = new Dictionary<string, object>
                        {
                            ["id"] = threadId * 10000 + i,
                            ["name"] = GetFunctionName(frame.Function),
                            ["line"] = 0,
                            ["column"] = 0
                        };
                        if (TryGetLine(frame.Function, frame.Instruction, out var file, out var line))
                        {
                            var path = GetSources(frame.Instance.Module).Paths[file];
                            result["source"] = new Dictionary<string, object>
                            {
                                ["name"] = Path.GetFileName(path), ["path"] = path
                            };
                            result["line"] = line;
                            result["column"] = 1;
                        }
                        else
                        {
                            result["presentationHint"] = "subtle";
                        }

                        frames.Add(result);
                    }
            }

            return new Dictionary<string, object> { ["stackFrames"] = frames, ["totalFrames"] = frames.Count };
        }

        private static string GetFunctionName(InstanceFunction function)
        {
            var module = function.instance.Module;
            var index = (uint)(module.ImportedFunctionCount + function.function.FunctionIndex);
            return module.Names.TryGetFunctionName(index, out var name) ? name : $"func[{index}]";
        }

        private object Scopes(Dictionary<string, object> arguments)
        {
            var frameId = GetInt(arguments, "frameId");
            var scopes = new List<object>();
            lock (locker)
            {
                if (pauses.TryGetValue(frameId / 10000, out var pause) && frameId % 10000 < pause.Frames.Count)
                {
                    var frame = pause.Frames[frameId % 10000];
                    scopes.Add(Scope("Locals", () => Values(frame.Locals.Span, "local")));
                    scopes.Add(Scope("Operand Stack", () => Values(frame.OperandStack.Span, "stack")));
                    scopes.Add(Scope("Globals", () => Globals(frame.Instance)));
                }
            }

            return new Dictionary<string, object> { ["scopes"] = scopes };

            Dictionary<string, object> Scope(string name, Func<List<object>> getVariables)
            {
                var reference = nextVariablesReference++;
                variables[reference] = getVariables;
                return new Dictionary<string, object>
                {
                    ["name"] = name, ["variablesReference"] = reference, ["expensive"] = false
                };
            }
        }

        private static List<object> Values(ReadOnlySpan<StackValueItem> values, string prefix)
        {
            var result = new List<object>(values.Length);
            for (var i = 0; i < values.Length; i++) result.Add(Variable($"{prefix}{i}", values[i]));
            return result;
        }

        private static List<object> Globals(Instance instance)
        {
            var globals = instance.GlobalInstance.Globals.Span;
            var result = new List<object>(globals.Length);
            for (var i = 0; i < globals.Length; i++) result.Add(Variable($"global{i}", globals[i].GetStackValue()));
            return result;
        }

        private static Dictionary<string, object> Variable(string name, StackValueItem value)
        {
            string text;
            value.ExpectValue(out var type);
            switch (type)
            {
                case ValueType.I32:
                {
                    var v = value.ExpectValueI32();
                    text = $"{(int)v} (0x{v:X8})";
                    break;
                }
                case ValueType.I64:
                {
                    var v = value.ExpectValueI64();
                    text = $"{(long)v} (0x{v:X16})";
                    break;
                }
                default:
                    text = value.ToString();
                    break;
            }

            return new Dictionary<string, object>
            {
                ["name"] = name, ["value"] = text, ["type"] = type.ToString().ToLowerInvariant(),
                ["variablesReference"] = 0
            };
        }

        private object Variables(Dictionary<string, object> arguments)
        {
            Func<List<object>> getVariables;
            lock (locker)
            {
                variables.TryGetValue(GetInt(arguments, "variablesReference"), out getVariables);
            }

            return new Dictionary<string, object> { ["variables"] = getVariables?.Invoke() ?? new List<object>() };
        }

        private void Resume(int threadId, StepKind? stepKind)
        {
            WasmDebugPause pause;
            lock (locker)
            {
                if (!pauses.Remove(threadId, out pause)) return;
                variables.Clear();
                if (stepKind is { } kind)
                {
                    var frame = pause.Frames[0];
                    TryGetLine(frame.Function, frame.Instruction, out var file, out var line);
                    steps[threadId] = new Step(kind, pause.Depth, file, line);
                }
            }

            pause.Resume();
        }

        private static int GetInt(Dictionary<string, object> arguments, string name)
        {
            return arguments != null && arguments.TryGetValue(name, out var value) && value is double d ? (int)d : 0;
        }

        #endregion

        private enum StepKind
        {
            In,
            Over,
            Out
        }

        private sealed class Step
        {
            private readonly StepKind kind;
            private readonly int depth;
            private readonly int file;
            private readonly int line;

            public Step(StepKind kind, int depth, int file, int line)
            {
                this.kind = kind;
                this.depth = depth;
                this.file = file;
                this.line = line;
            }

            public bool ShouldPause(DebugAdapterServer server, in WasmDebugLocation location)
            {
                // a step stops at the start of another line. without the line table, a step is an instruction.
                var hasLine = server.TryGetLine(location, out var currentFile, out var currentLine);
                var isAnotherLine = hasLine
                    ? currentFile != file || currentLine != line || location.Depth != depth
                    : line == 0;
                if (!isAnotherLine) return false;

                return kind switch
                {
                    StepKind.In => true,
                    StepKind.Over => location.Depth <= depth,
                    StepKind.Out => location.Depth < depth,
                    _ => false
                };
            }
        }

        private sealed class Breakpoint
        {
            public Breakpoint(int id, int line)
            {
                Id = id;
                Line = line;
                ResolvedLine = line;
            }

            public int Id { get; }
            public int Line { get; }
            public int ResolvedLine { get; set; }

            public Dictionary<string, object> ToJson()
            {
                return new Dictionary<string, object> { ["id"] = Id, ["verified"] = true, ["line"] = ResolvedLine };
            }
        }

        private sealed class ModuleSources
        {
            public string[] Paths;
            public string[] NormalizedPaths;
            public bool[] IsUserCode;
            public uint[] InstructionOffsets;
        }

        private sealed class ModuleBreakpoints
        {
            public readonly HashSet<uint> Offsets = new();
            public int Version = -1;
        }

        private sealed class ThreadId
        {
            public ThreadId(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        /// <summary>
        ///     A connection with a client, which reads the requests on its own thread.
        /// </summary>
        private sealed class Connection
        {
            private readonly DebugAdapterServer server;
            private readonly TcpClient client;
            private readonly Stream stream;
            private readonly object writeLocker = new();
            private int sequence = 1;
            private (string name, object body)? eventAfterResponse;
            private bool closeAfterResponse;

            public Connection(DebugAdapterServer server, TcpClient client)
            {
                this.server = server;
                this.client = client;
                stream = client.GetStream();
            }

            public void Run()
            {
                var thread = new Thread(Read) { IsBackground = true, Name = "WaaS Debug Adapter Connection" };
                thread.Start();
            }

            public void Close()
            {
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                    // already closed
                }
            }

            public void SendEventAfterResponse(string name, object body)
            {
                eventAfterResponse = (name, body);
            }

            public void CloseAfterResponse()
            {
                closeAfterResponse = true;
            }

            private void Read()
            {
                try
                {
                    while (true)
                    {
                        var message = ReadMessage();
                        if (message == null) break;
                        if (Json.Parse(message) is not Dictionary<string, object> request ||
                            !Equals(request.GetValueOrDefault("type"), "request")) continue;

                        var command = (string)request["command"];
                        var arguments = request.GetValueOrDefault("arguments") as Dictionary<string, object>;
                        var response = new Dictionary<string, object>
                        {
                            ["type"] = "response", ["request_seq"] = request["seq"], ["command"] = command
                        };
                        try
                        {
                            response["body"] = server.Handle(this, command, arguments);
                            response["success"] = true;
                        }
                        catch (Exception ex)
                        {
                            response["success"] = false;
                            response["message"] = ex.Message;
                        }

                        Send(response);

                        if (eventAfterResponse is { } e)
                        {
                            eventAfterResponse = null;
                            SendEvent(e.name, e.body);
                        }

                        if (closeAfterResponse) break;
                    }
                }
                catch (IOException)
                {
                    // disconnected
                }
                catch (ObjectDisposedException)
                {
                    // closed
                }
                finally
                {
                    Close();
                    server.OnDisconnected(this);
                }
            }

            public void SendEvent(string name, object body)
            {
                var message = new Dictionary<string, object> { ["type"] = "event", ["event"] = name };
                if (body != null) message["body"] = body;
                try
                {
                    Send(message);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                    // disconnected
                }
            }

            private void Send(Dictionary<string, object> message)
            {
                lock (writeLocker)
                {
                    message["seq"] = sequence++;
                    var content = Encoding.UTF8.GetBytes(Json.Serialize(message));
                    var header = Encoding.ASCII.GetBytes($"Content-Length: {content.Length}\r\n\r\n");
                    stream.Write(header, 0, header.Length);
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }

            // reads a message framed with the Content-Length header, or returns null at the end of the stream
            private string ReadMessage()
            {
                var contentLength = -1;
                while (true)
                {
                    var line = ReadHeaderLine();
                    if (line == null) return null;
                    if (line.Length == 0) break;
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        contentLength = int.Parse(line.Substring("Content-Length:".Length).Trim());
                }

                if (contentLength < 0) throw new IOException("Content-Length is missing.");
                var buffer = new byte[contentLength];
                var read = 0;
                while (read < contentLength)
                {
                    var n = stream.Read(buffer, read, contentLength - read);
                    if (n <= 0) return null;
                    read += n;
                }

                return Encoding.UTF8.GetString(buffer);
            }

            private string ReadHeaderLine()
            {
                var builder = new StringBuilder();
                while (true)
                {
                    var b = stream.ReadByte();
                    if (b < 0) return null;
                    if (b == '\n') return builder.ToString().TrimEnd('\r');
                    builder.Append((char)b);
                }
            }
        }
    }
}
