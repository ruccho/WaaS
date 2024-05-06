
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    [ScriptedImporter(1, "wasm")]
    public class WasmImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var module = ScriptableObject.CreateInstance<ModuleAsset>();
            
            module.SetData(File.ReadAllBytes(ctx.assetPath));
            
            ctx.AddObjectToAsset("module",  module);
            
            ctx.SetMainObject(module);
        }
    }
}
