using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using WaaS.Unity;
using Wukuro.Dialog;
using CoreTypes = Wukuro.Core.ITypes;
using DialogTypes = Wukuro.Dialog.ITypes;
using Object = UnityEngine.Object;

namespace Wukuro.Tests
{
    /// <summary>
    ///     Runs the entry points of wukuro-tests.rs, wukuro-tests.mbt and MoonBitPackage, which are the same scripts written
    ///     in Rust, in a MoonBit source file and in a MoonBit package.
    /// </summary>
    [TestFixture("Assets/Tests/Wukuro/wukuro-tests.rs")]
    [TestFixture("Assets/Tests/Wukuro/wukuro-tests.mbt")]
    [TestFixture("Assets/Tests/Wukuro/MoonBitPackage/moon.pkg")]
    public class WukuroTests
    {
        private string ScriptPath { get; }

        private ComponentAsset script;

        public WukuroTests(string scriptPath)
        {
            ScriptPath = scriptPath;
        }

        private bool IsRust => ScriptPath.EndsWith(".rs");

        [OneTimeSetUp]
        public void SetUp()
        {
            script = AssetDatabase.LoadAssetAtPath<ComponentAsset>(ScriptPath);
            Assert.That(script, Is.Not.Null,
                $"{ScriptPath} is not imported as a component: {AssetDatabase.GetMainAssetTypeAtPath(ScriptPath)}, " +
                $"{string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(ScriptPath).Select(a => a.GetType().Name))}");
            DialogImports.Register();
            WukuroTestRecords.Register();
        }

        private WukuroEntryReference Entry(string name)
        {
            return new WukuroEntryReference(script, AssetDatabase.AssetPathToGUID(ScriptPath), name);
        }

        private static Task RunAsync(WukuroRunner runner, WukuroEntryReference entry, IVariables variables,
            IArguments arguments = null)
        {
            return runner.RunAsync(entry, variables, arguments, false).AsTask();
        }

        [Test]
        public void EntryPointsAreReadFromWit()
        {
            var entries = WukuroEntryPoints.Parse(script.Wit);
            Assert.That(entries.Select(e => e.Name),
                Is.SupersetOf(new[] { "store-arguments", "increment", "run-action", "typed-object", "talk" }));

            var parameters = entries.Single(e => e.Name == "store-arguments").Parameters
                .Where(p => p.IsArgument)
                .Select(p => $"{p.Name}: {p.WitType}");
            Assert.That(parameters, Is.EqualTo(new[] { "count: s32", "label: string", "flag: bool" }));
        }

        [Test]
        public void SmallIntegersAreDeclaredWithTheirTypes()
        {
            var parameters = WukuroEntryPoints.Parse(script.Wit).Single(e => e.Name == "small-integers").Parameters
                .Where(p => p.IsArgument)
                .Select(p => $"{p.WitType}");
            Assert.That(parameters, Is.EqualTo(new[] { "s16", "u16" }));
        }

        [Test]
        public async Task ArgumentsArePassedByName()
        {
            var variables = new MemoryVariables();
            var arguments = new WukuroRuntimeArguments()
                .Add("count", 21)
                .Add("label", "wukuro")
                .Add("flag", true);

            await RunAsync(new WukuroRunner(), Entry("store-arguments"), variables, arguments);

            Assert.That(variables.Values["count"].TyS32, Is.EqualTo(42));
            Assert.That(variables.Values["label"].TyString, Is.EqualTo("wukuro!"));
            Assert.That(variables.Values["flag"].TyBool, Is.False);
        }

        [Test]
        public async Task VariablesPersistAcrossRuns()
        {
            var variables = new MemoryVariables();
            var runner = new WukuroRunner();

            await RunAsync(runner, Entry("increment"), variables);
            await RunAsync(runner, Entry("increment"), variables);
            await RunAsync(runner, Entry("increment"), variables);

            Assert.That(variables.Values["counter"].TyS32, Is.EqualTo(3));
        }

        [Test]
        public async Task ActionsAreCalledFromScripts()
        {
            var called = 0;
            var arguments = new WukuroRuntimeArguments().Add("callback", () => called++);

            await RunAsync(new WukuroRunner(), Entry("run-action"), new MemoryVariables(), arguments);

            Assert.That(called, Is.EqualTo(1));
        }

        [Test]
        public void ObjectTypesAreRecorded()
        {
            var types = WukuroEntryPoints.GetObjectTypes(script);

            Assert.That(types[("typed-object", "target")],
                Is.EqualTo("UnityEngine.Transform, UnityEngine.CoreModule"));
            Assert.That(types.ContainsKey(("typed-object", "untyped")), Is.False);
        }

        [Test]
        public void MissingEntryPointThrows()
        {
            Assert.That(async () => await RunAsync(new WukuroRunner(), Entry("missing"), new MemoryVariables()),
                Throws.ArgumentException);
        }

        [Test]
        public async Task PanicMessageIsReported()
        {
            var arguments = new WukuroRuntimeArguments().Add("message", "script failure");

            WukuroPanicException exception = null;
            try
            {
                await RunAsync(new WukuroRunner(), Entry("panics"), new MemoryVariables(), arguments);
            }
            catch (WukuroPanicException ex)
            {
                exception = ex;
            }

            Assert.That(exception, Is.Not.Null);
            // e.g. "panicked at src/lib.rs:10:5:\nscript failure" in Rust, the message passed to fail in MoonBit
            if (IsRust) Assert.That(exception.PanicMessage, Does.StartWith("panicked at "));
            Assert.That(exception.PanicMessage, Does.Contain("script failure"));
            Assert.That(exception.WasmStackTrace, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("wasm backtrace:"));
        }

        [Test]
        public async Task DialogsAreShownThroughTheModule()
        {
            var dialog = new RecordingDialogModule { ChosenOption = 1 };
            var variables = new MemoryVariables();
            var runner = new WukuroRunner();
            runner.AddModule(dialog);

            await RunAsync(runner, Entry("talk"), variables);

            Assert.That(dialog.Log, Is.EqualTo(new[]
            {
                "open test",
                "message [] hello auto=False skip=False styles=",
                "message [] {table/key} auto=True skip=True styles=whisper",
                "options first, second",
                "close"
            }));
            Assert.That(variables.Values["chosen"].TyS32, Is.EqualTo(1));
        }

        private static ITestRecordTypes.DoorSettings CreateSettings(Transform target)
        {
            return new ITestRecordTypes.DoorSettings
            {
                Count = 3,
                Label = "gate",
                Kind = ITestRecordTypes.DoorKind.Iron,
                Locked = true,
                Target = target,
                Limits = new ITestRecordTypes.Limits { Min = 0.5f, Max = 2f }
            };
        }

        private static void AssertReceived(Transform target)
        {
            Assert.That(WukuroTestRecords.Received, Is.Not.Null);
            var (settings, kind) = WukuroTestRecords.Received.Value;
            Assert.That(settings.Count, Is.EqualTo(3));
            Assert.That(settings.Label, Is.EqualTo("gate"));
            Assert.That(settings.Kind, Is.EqualTo(ITestRecordTypes.DoorKind.Iron));
            Assert.That(settings.Locked, Is.True);
            Assert.That(settings.Target.Value, Is.SameAs(target));
            Assert.That(settings.Limits.Min, Is.EqualTo(0.5f));
            Assert.That(settings.Limits.Max, Is.EqualTo(2f));
            Assert.That(kind, Is.EqualTo(ITestRecordTypes.DoorKind.Wooden));
        }

        [Test]
        public void RecordParametersAreReadFromWit()
        {
            var parameters = WukuroEntryPoints.Parse(script.Wit).Single(e => e.Name == "forward-settings").Parameters
                .Where(p => p.IsArgument)
                .ToArray();
            Assert.That(parameters.Select(p => $"{p.Name}: {p.WitType} ({p.TypeInterface})"), Is.EqualTo(new[]
            {
                "settings: door-settings (wukuro-tests:records/types)",
                "kind: door-kind (wukuro-tests:records/types)"
            }));

            // the inspector creates the containers of the C# types
            var settings = ArgumentContainerBase.Create(parameters[0]);
            Assert.That(settings, Is.InstanceOf<ArgumentContainer<ITestRecordTypes.DoorSettings>>());
            Assert.That(settings.WitType, Is.EqualTo("door-settings"));
            Assert.That(ArgumentContainerBase.Create(parameters[1]),
                Is.InstanceOf<ArgumentContainer<ITestRecordTypes.DoorKind>>());
        }

        [Test]
        public async Task RecordsArePassedToHostFunctions()
        {
            var target = new GameObject("target").transform;
            try
            {
                WukuroTestRecords.Received = null;
                var arguments = new WukuroRuntimeArguments()
                    .AddValue("settings", CreateSettings(target))
                    .AddValue("kind", ITestRecordTypes.DoorKind.Wooden);

                await RunAsync(new WukuroRunner(), Entry("forward-settings"), new MemoryVariables(), arguments);

                AssertReceived(target);
            }
            finally
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public async Task SerializedRecordsArePassedToHostFunctions()
        {
            var target = new GameObject("target").transform;
            try
            {
                WukuroTestRecords.Received = null;
                // the arguments as set in the inspector
                var entry = Entry("forward-settings");
                var arguments = typeof(WukuroEntryReference)
                    .GetField("_arguments", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(entry);
                arguments.GetType().GetField("_arguments", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .SetValue(arguments, new ArgumentContainerBase[]
                    {
                        new ArgumentContainer<ITestRecordTypes.DoorSettings>("settings", CreateSettings(target)),
                        new ArgumentContainer<ITestRecordTypes.DoorKind>("kind", ITestRecordTypes.DoorKind.Wooden)
                    });

                await RunAsync(new WukuroRunner(), entry, new MemoryVariables());

                AssertReceived(target);
            }
            finally
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void RustMacrosOfRecordsAreUpToDate()
        {
            Assert.That(File.ReadAllText(WukuroTestRecords.RustPath).Replace("\r\n", "\n"),
                Is.EqualTo(WukuroTestRecords.GenerateRust()),
                "Run Wukuro.Tests.WukuroTestRecords.Export to regenerate the files.");
        }

        private class RecordingDialogModule : IDialogModule
        {
            public readonly List<string> Log = new();
            public int ChosenOption;

            public ValueTask OpenDialog(Option<string> provider)
            {
                Log.Add($"open {provider.Some}");
                return default;
            }

            public ValueTask CloseDialog()
            {
                Log.Add("close");
                return default;
            }

            public ValueTask ShowMessage(DialogTypes.MessageInfo message)
            {
                Log.Add($"message [{message.Speaker}] {Describe(message.Text)} auto={message.Auto} " +
                        $"skip={message.ForbidSkip} styles={string.Join(",", message.Styles.ToArray())}");
                return default;
            }

            public ValueTask<int> ShowOptions(ReadOnlyMemory<CoreTypes.Text> options)
            {
                Log.Add($"options {string.Join(", ", options.ToArray().Select(Describe))}");
                return new ValueTask<int>(ChosenOption);
            }

            public ValueTask FadeIn(float duration)
            {
                Log.Add("fade-in");
                return default;
            }

            public ValueTask FadeOut(float duration)
            {
                Log.Add("fade-out");
                return default;
            }

            private static string Describe(CoreTypes.Text text)
            {
                return text.Localized is { } id ? $"{{{id.Table}/{id.Key}}}" : text.Plain;
            }
        }
    }
}
