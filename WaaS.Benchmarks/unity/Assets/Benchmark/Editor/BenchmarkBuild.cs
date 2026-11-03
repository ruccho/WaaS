using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using XLua;

namespace WaaS.Benchmarks.Editor
{
    /// <summary>
    ///     Entry points called in batch mode from run.py.
    /// </summary>
    public static class BenchmarkBuild
    {
        private const string ScenePath = "Assets/Benchmark/Empty.unity";

        public static void GenerateLuaBindings()
        {
            // Generator.ClearAll passes an absolute path to AssetDatabase.DeleteAsset, which Unity 6 rejects
            AssetDatabase.DeleteAsset("Assets/XLua/Gen");
            CSObjectWrapEditor.Generator.GenAll();
            AssetDatabase.Refresh();
        }

        public static void BuildMono()
        {
            Build(ScriptingImplementation.Mono2x, "Mono");
        }

        public static void BuildIl2Cpp()
        {
            Build(ScriptingImplementation.IL2CPP, "IL2CPP");
        }

        private static void Build(ScriptingImplementation backend, string name)
        {
            CompileLuaBytecode();
            EnsureScene();

            var target = NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(target, backend);
            PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = $"Builds/{name}/Benchmark.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"The build failed: {report.summary.result}");
        }

        private static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>
        ///     Precompiles benchmark.lua, as a product can do at build time to skip compilation at runtime.
        /// </summary>
        private static void CompileLuaBytecode()
        {
            var directory = Application.streamingAssetsPath;
            var source = File.ReadAllText(Path.Combine(directory, "benchmark.lua"), Encoding.UTF8);

            using var env = new LuaEnv();
            using var dump = env.LoadString("return string.dump(load(...))");
            var bytecode = (byte[])dump.Call(new object[] { source }, new[] { typeof(byte[]) })[0];
            File.WriteAllBytes(Path.Combine(directory, "benchmark.luac"), bytecode);
        }
    }
}
