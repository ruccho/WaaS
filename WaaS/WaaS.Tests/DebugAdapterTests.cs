using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Debugging;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

/// <summary>
///     Debugs the guest Guests/debug-test, built with the line tables of DWARF, through the Debug Adapter Protocol.
/// </summary>
[NonParallelizable]
public class DebugAdapterTests
{
    // the lines in Guests/debug-test/src/lib.rs
    private const int SquareResultLine = 7;
    private const int LoopBodyLine = 13;
    private const int SumLine = 16;

    private static string SourcePath => Path.Combine(GuestBuilder.GetCrateDirectory("debug-test"), "src", "lib.rs");

    [Test]
    public void LineTableIsRead()
    {
        var module = Module.Create(File.ReadAllBytes(GuestBuilder.BuildModule("debug-test")));
        var table = module.LineTable;
        Assert.That(table, Is.Not.Null);
        Assert.That(table!.Files, Does.Contain("src/lib.rs"));

        var compute = FindFunction(module, "compute");
        var lines = compute.Body.Instructions.ToArray()
            .Select(i => table.TryFind(i.Offset, out var row) && table.GetFile(row) == "src/lib.rs" ? row.Line : 0)
            .Where(l => l > 0).Distinct().ToArray();
        Assert.That(lines, Is.SupersetOf(new[] { LoopBodyLine, SumLine }));
    }

    [Test]
    public async Task BreakpointsAndStepsAreHandled()
    {
        var module = Module.Create(File.ReadAllBytes(GuestBuilder.BuildModule("debug-test")));
        var instance = new Instance(module, new Imports());
        Assert.That(instance.TryGetExport("compute", out IInvocableFunction? compute), Is.True);

        using var server = new DebugAdapterServer(0, GuestBuilder.GetCrateDirectory("debug-test"));
        using var client = new Client(server.Port);

        await client.Request("initialize", new JsonObject { ["adapterID"] = "waas" });
        await client.WaitEvent("initialized");
        await client.Request("attach", new JsonObject());
        Assert.That(ExecutionContext.DefaultDebugger, Is.SameAs(server));
        await client.Request("setBreakpoints", new JsonObject
        {
            ["source"] = new JsonObject { ["path"] = SourcePath },
            ["breakpoints"] = new JsonArray(new JsonObject { ["line"] = LoopBodyLine })
        });
        await client.Request("configurationDone", new JsonObject());

        // the contexts created after the attach are debugged
        var run = Task.Run(async () =>
        {
            using var context = new ExecutionContext();
            await context.InvokeAsync(compute!, [new StackValueItem(4u)]);
            Span<StackValueItem> results = stackalloc StackValueItem[1];
            context.TakeResults(results);
            return results[0].ExpectValueI32();
        });

        var stopped = await client.WaitEvent("stopped");
        Assert.That((string?)stopped["reason"], Is.EqualTo("breakpoint"));
        var threadId = (int)stopped["threadId"]!;

        var frame = await TopFrame(client, threadId);
        Assert.That((string?)frame["name"], Is.EqualTo("compute"));
        Assert.That((int)frame["line"]!, Is.EqualTo(LoopBodyLine));
        Assert.That(Path.GetFullPath((string)frame["source"]!["path"]!), Is.EqualTo(Path.GetFullPath(SourcePath)));

        // the parameter n is the first local
        var scopes = await client.Request("scopes", new JsonObject { ["frameId"] = (int)frame["id"]! });
        var locals = scopes["scopes"]!.AsArray().Single(s => (string?)s!["name"] == "Locals")!;
        var variables = await client.Request("variables",
            new JsonObject { ["variablesReference"] = (int)locals["variablesReference"]! });
        Assert.That((string?)variables["variables"]![0]!["value"], Does.StartWith("4 "));

        // steps into square, skipping the lines of the standard library
        await client.Request("stepIn", new JsonObject { ["threadId"] = threadId });
        Assert.That((string?)(await client.WaitEvent("stopped"))["reason"], Is.EqualTo("step"));
        frame = await TopFrame(client, threadId);
        Assert.That((string?)frame["name"], Does.Contain("square"));
        Assert.That((int)frame["line"]!, Is.EqualTo(SquareResultLine), (string?)frame["source"]?["path"]);
        Assert.That((int)(await client.Request("stackTrace", new JsonObject { ["threadId"] = threadId }))
            ["totalFrames"]!, Is.EqualTo(2));

        // steps out to compute
        await client.Request("stepOut", new JsonObject { ["threadId"] = threadId });
        await client.WaitEvent("stopped");
        frame = await TopFrame(client, threadId);
        Assert.That((string?)frame["name"], Is.EqualTo("compute"));

        // the breakpoint is hit again in the next iteration, and removed
        await client.Request("continue", new JsonObject { ["threadId"] = threadId });
        Assert.That((string?)(await client.WaitEvent("stopped"))["reason"], Is.EqualTo("breakpoint"));
        await client.Request("setBreakpoints", new JsonObject
        {
            ["source"] = new JsonObject { ["path"] = SourcePath }, ["breakpoints"] = new JsonArray()
        });
        await client.Request("continue", new JsonObject { ["threadId"] = threadId });

        Assert.That(await run.WaitAsync(TimeSpan.FromSeconds(30)), Is.EqualTo(0u + 1 + 4 + 9));

        await client.Request("disconnect", new JsonObject());
        await WaitUntil(() => !server.IsConnected);
        Assert.That(ExecutionContext.DefaultDebugger, Is.Null);
    }

    [Test]
    public async Task BreakpointOnLineWithoutCodeMovesToNextLine()
    {
        var module = Module.Create(File.ReadAllBytes(GuestBuilder.BuildModule("debug-test")));
        var instance = new Instance(module, new Imports());
        Assert.That(instance.TryGetExport("compute", out IInvocableFunction? compute), Is.True);

        using var server = new DebugAdapterServer(0, GuestBuilder.GetCrateDirectory("debug-test"));
        using var client = new Client(server.Port);
        await client.Request("initialize", new JsonObject());
        await client.Request("attach", new JsonObject());
        // the multiplication of the line is attributed to the standard library, which is inlined
        await client.Request("setBreakpoints", new JsonObject
        {
            ["source"] = new JsonObject { ["path"] = SourcePath },
            ["breakpoints"] = new JsonArray(new JsonObject { ["line"] = SquareResultLine - 2 })
        });

        var run = Task.Run(async () =>
        {
            using var context = new ExecutionContext();
            await context.InvokeAsync(compute!, [new StackValueItem(1u)]);
        });

        var changed = await client.WaitEvent("breakpoint");
        Assert.That((int)changed["breakpoint"]!["line"]!, Is.EqualTo(SquareResultLine));
        var stopped = await client.WaitEvent("stopped");
        var frame = await TopFrame(client, (int)stopped["threadId"]!);
        Assert.That((int)frame["line"]!, Is.EqualTo(SquareResultLine));

        await client.Request("disconnect", new JsonObject());
        await run.WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Test]
    public async Task PausedExecutionResumesOnDisconnect()
    {
        var module = Module.Create(File.ReadAllBytes(GuestBuilder.BuildModule("debug-test")));
        var instance = new Instance(module, new Imports());
        Assert.That(instance.TryGetExport("compute", out IInvocableFunction? compute), Is.True);

        using var server = new DebugAdapterServer(0, GuestBuilder.GetCrateDirectory("debug-test"));
        using (var client = new Client(server.Port))
        {
            await client.Request("initialize", new JsonObject());
            await client.Request("attach", new JsonObject());
            await client.Request("pause", new JsonObject { ["threadId"] = 0 });

            var run = Task.Run(async () =>
            {
                using var context = new ExecutionContext();
                await context.InvokeAsync(compute!, [new StackValueItem(3u)]);
            });
            Assert.That((string?)(await client.WaitEvent("stopped"))["reason"], Is.EqualTo("pause"));

            await client.Request("disconnect", new JsonObject());
            await run.WaitAsync(TimeSpan.FromSeconds(30));
        }

        await WaitUntil(() => !server.IsConnected);
    }

    private static async Task<JsonNode> TopFrame(Client client, int threadId)
    {
        var trace = await client.Request("stackTrace", new JsonObject { ["threadId"] = threadId });
        return trace["stackFrames"]![0]!;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(50);
        Assert.That(condition(), Is.True);
    }

    private static Function FindFunction(Module module, string name)
    {
        var functions = module.InternalFunctions.Span;
        for (var i = 0; i < functions.Length; i++)
            if (module.Names.TryGetFunctionName((uint)(module.ImportedFunctionCount + i), out var n) && n == name)
                return functions[i];
        throw new InvalidOperationException($"{name} is not found");
    }

    /// <summary>
    ///     A client of the Debug Adapter Protocol.
    /// </summary>
    private sealed class Client : IDisposable
    {
        private readonly TcpClient tcp;
        private readonly NetworkStream stream;
        private readonly Queue<JsonNode> events = new();
        private int sequence = 1;

        public Client(int port)
        {
            tcp = new TcpClient("127.0.0.1", port);
            stream = tcp.GetStream();
            stream.ReadTimeout = 30000;
        }

        public void Dispose()
        {
            tcp.Dispose();
        }

        public async Task<JsonNode> Request(string command, JsonObject arguments)
        {
            var seq = sequence++;
            var message = new JsonObject
            {
                ["seq"] = seq, ["type"] = "request", ["command"] = command, ["arguments"] = arguments
            };
            var content = Encoding.UTF8.GetBytes(message.ToJsonString());
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"Content-Length: {content.Length}\r\n\r\n"));
            await stream.WriteAsync(content);

            while (true)
            {
                var received = await Read();
                if ((string?)received["type"] == "event")
                {
                    events.Enqueue(received);
                    continue;
                }

                if ((int)received["request_seq"]! != seq) continue;
                Assert.That((bool)received["success"]!, Is.True, $"{command}: {received["message"]}");
                return received["body"] ?? new JsonObject();
            }
        }

        public async Task<JsonNode> WaitEvent(string name)
        {
            while (true)
            {
                var received = events.Count > 0 ? events.Dequeue() : await Read();
                if ((string?)received["type"] == "event" && (string?)received["event"] == name)
                    return received["body"] ?? new JsonObject();
            }
        }

        private async Task<JsonNode> Read()
        {
            var length = -1;
            while (true)
            {
                var line = await ReadLine();
                if (line.Length == 0) break;
                if (line.StartsWith("Content-Length:")) length = int.Parse(line["Content-Length:".Length..].Trim());
            }

            var buffer = new byte[length];
            await stream.ReadExactlyAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
            return JsonNode.Parse(buffer)!;
        }

        private async Task<string> ReadLine()
        {
            var builder = new StringBuilder();
            var buffer = new byte[1];
            while (true)
            {
                var n = await stream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(30));
                if (n == 0) throw new IOException("disconnected");
                if (buffer[0] == '\n') return builder.ToString().TrimEnd('\r');
                builder.Append((char)buffer[0]);
            }
        }
    }
}
