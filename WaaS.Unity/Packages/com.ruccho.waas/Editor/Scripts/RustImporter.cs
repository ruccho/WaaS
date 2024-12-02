#if !WAAS_DISABLE_RUST_IMPORTER
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WaaS.Unity.Editor
{
    [ScriptedImporter(1, "rs")]
    public class RustImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var module = ScriptableObject.CreateInstance<ModuleAsset>();

            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wasm");

            var process = Process.Start(new ProcessStartInfo(@$"rustc",
                    $@"""{ctx.assetPath}"" --target wasm32-unknown-unknown --crate-type cdylib -o ""{path}"" -C opt-level=z -C lto")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            );
            process!.WaitForExit();

            var error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            module.SetData(File.ReadAllBytes(path));

            ctx.AddObjectToAsset("module", module);

            ctx.SetMainObject(module);
            File.Delete(path);
        }
    }
}
#endif