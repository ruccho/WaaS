using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor.AssetImporters;
using UnityEngine;
using WaaS.Models;

namespace WaaS.Unity.Editor
{
    [ScriptedImporter(1, "wasm")]
    public class WasmImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var data = File.ReadAllBytes(ctx.assetPath);
            var preamble = Unsafe.ReadUnaligned<Preamble>(ref data[0]);
            if (preamble.IsValid())
            {
                var module = ScriptableObject.CreateInstance<ModuleAsset>();

                module.SetData(data);

                ctx.AddObjectToAsset("module", module);

                ctx.SetMainObject(module);
            }
            else if (preamble is { magic: 0x6D736100, version: 0x0001000d })
            {
                var component = ScriptableObject.CreateInstance<ComponentAsset>();

                component.SetData(data);

                ctx.AddObjectToAsset("component", component);

                ctx.SetMainObject(component);
            }
        }
    }
}