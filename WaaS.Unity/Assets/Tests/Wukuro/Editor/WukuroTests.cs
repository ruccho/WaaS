using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using WaaS.ComponentModel.Binding;
using WaaS.Unity;
using Wukuro.Dialog;
using CoreTypes = Wukuro.Core.ITypes;
using DialogTypes = Wukuro.Dialog.ITypes;

namespace Wukuro.Tests
{
    /// <summary>
    ///     Runs the entry points of wukuro-tests.rs.
    /// </summary>
    public class WukuroTests
    {
        private const string ScriptPath = "Assets/Tests/Wukuro/wukuro-tests.rs";

        private ComponentAsset script;

        [OneTimeSetUp]
        public void SetUp()
        {
            script = AssetDatabase.LoadAssetAtPath<ComponentAsset>(ScriptPath);
            Assert.That(script, Is.Not.Null,
                $"{ScriptPath} is not imported as a component: {AssetDatabase.GetMainAssetTypeAtPath(ScriptPath)}, " +
                $"{string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(ScriptPath).Select(a => a.GetType().Name))}");
            DialogImports.Register();
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
            // e.g. "panicked at src/lib.rs:10:5:\nscript failure"
            Assert.That(exception.PanicMessage, Does.StartWith("panicked at "));
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
