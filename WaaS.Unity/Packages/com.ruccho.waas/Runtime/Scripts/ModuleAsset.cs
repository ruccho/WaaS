using System;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.Models;

namespace WaaS.Unity
{
    public class ModuleAsset : ScriptableObject
    {
        [SerializeField] private bool deserializeOnLoad;
        [SerializeField, HideInInspector] private byte[] data;
        [NonSerialized] private Lazy<Module> module;

        internal ulong Size => (ulong)(data?.Length ?? 0);
        internal byte[] Data => data;

        private void OnEnable()
        {
            module = new(() =>
            {
                var module = Module.Create(data);
                module.SourceDescription = name;
                return module;
            }, true);
            
            if (deserializeOnLoad && data != null)
            {
                LoadModule();
            }
        }

        private void OnDisable()
        {
            module = null;
        }

        public async ValueTask<Module> LoadModuleAsync()
        {
            if (module.IsValueCreated) return module.Value;
            return await Task.Run(() => module.Value);
        }

        public Module LoadModule()
        {
            return module.Value;
        }

        internal void SetData(byte[] data)
        {
            this.data = data;
        }
    }
}
