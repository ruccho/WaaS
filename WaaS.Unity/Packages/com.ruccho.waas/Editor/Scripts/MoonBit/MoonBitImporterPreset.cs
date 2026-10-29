using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    public class MoonBitImporterPreset : ScriptableObject
    {
        [SerializeField] internal MoonBitImporterSettings settings;
        public MoonBitImporterSettings Settings => settings;
    }
}
