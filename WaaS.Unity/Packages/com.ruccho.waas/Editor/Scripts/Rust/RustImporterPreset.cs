using UnityEngine;

namespace WaaS.Unity.Editor.Rust
{
    public class RustImporterPreset : ScriptableObject
    {
        [SerializeField] internal RustImporterSettings settings;
        public IRustImporterSettings Settings => settings;
    }
}