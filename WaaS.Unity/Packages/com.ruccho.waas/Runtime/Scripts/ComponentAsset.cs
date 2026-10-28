using System;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Runtime;
using Component = WaaS.ComponentModel.Models.Component;

namespace WaaS.Unity
{
    public class ComponentAsset : ScriptableObject
    {
        [SerializeField] private bool deserializeOnLoad;
        [SerializeField] [HideInInspector] private byte[] data;

        // the type of the component in WIT, recorded when the asset is imported in the editor
        [SerializeField] [HideInInspector] private string wit;
        [NonSerialized] private Lazy<IComponent> component;

        internal ulong Size => (ulong)(data?.Length ?? 0);

        /// <summary>
        ///     The type of the component (imports and exports) in WIT, or an empty string if it could not be decoded.
        /// </summary>
        public string Wit => wit ?? "";
        internal byte[] Data => data;

        private void OnEnable()
        {
            component = new Lazy<IComponent>(() => Component.Create(data), true);

            if (deserializeOnLoad && data != null) LoadComponent();
        }

        private void OnDisable()
        {
            component = null;
        }

        public async ValueTask<IComponent> LoadComponentAsync()
        {
            if (component.IsValueCreated) return component.Value;
            return await Task.Run(() => component.Value);
        }

        public IComponent LoadComponent()
        {
            return component.Value;
        }

        internal void SetData(byte[] data)
        {
            this.data = data;
        }

        internal void SetWit(string wit)
        {
            this.wit = wit;
        }
    }
}