using System;
using UnityEngine;
using WaaS.Unity;

namespace Wukuro
{
    /// <summary>
    ///     A reference to an entry point of a script, with the arguments passed to it.
    /// </summary>
    [Serializable]
    public class WukuroEntryReference
    {
        [SerializeField] private ComponentAsset _script;

        // GUID of the script asset, set in the editor. Used to identify the script at runtime (e.g. for saved variables).
        [SerializeField] [HideInInspector] private string _scriptId;

        [SerializeField] private string _entry;
        [SerializeField] private Arguments _arguments = new();

        public WukuroEntryReference()
        {
        }

        public WukuroEntryReference(ComponentAsset script, string scriptId, string entry)
        {
            _script = script;
            _scriptId = scriptId;
            _entry = entry;
        }

        public ComponentAsset Script => _script;

        /// <summary>
        ///     A stable identifier of the script (GUID of the asset), which does not change when the asset is renamed or moved.
        /// </summary>
        public string ScriptId => _scriptId;

        /// <summary>
        ///     Name of the entry point in kebab-case, such as "talk-to-villager" for a Rust function `talk_to_villager`.
        /// </summary>
        public string Entry => _entry;

        public IArguments Arguments => _arguments;

        public bool IsValid => _script != null && !string.IsNullOrEmpty(_entry);
    }
}
