using System;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.Models;

namespace WaaS.Unity
{
    public class ComponentAsset : ScriptableObject
    {
        [SerializeField] private bool deserializeOnLoad;
        [SerializeField, HideInInspector] private byte[] data;
        [NonSerialized] private Lazy<ComponentModel.Models.Component> component;

        private void OnEnable()
        {
            component = new(() => ComponentModel.Models.Component.Create(data), true);
            
            if (deserializeOnLoad && data != null)
            {
                LoadComponent();
            }
        }

        private void OnDisable()
        {
            component = null;
        }

        public async ValueTask<ComponentModel.Models.Component> LoadComponentAsync()
        {
            if (component.IsValueCreated) return component.Value;
            return await Task.Run(() => component.Value);
        }

        public ComponentModel.Models.Component LoadComponent()
        {
            return component.Value;
        }

        internal void SetData(byte[] data)
        {
            this.data = data;
        }
    }
}