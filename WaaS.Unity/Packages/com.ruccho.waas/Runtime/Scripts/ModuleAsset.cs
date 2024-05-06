using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
        [NonSerialized] private SynchronizationContext unityMain;

        private void OnEnable()
        {
            unityMain = SynchronizationContext.Current;
            
            module = new(() => Module.Create(data), true);
            
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
