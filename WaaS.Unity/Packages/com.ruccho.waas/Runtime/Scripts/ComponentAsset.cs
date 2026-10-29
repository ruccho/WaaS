using System;
using System.Collections.Generic;
using System.Text;
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

        /// <summary>
        ///     Returns the contents of the custom sections with the name, in the component and the core modules in it.
        /// </summary>
        public IReadOnlyList<ReadOnlyMemory<byte>> GetCustomSections(string name)
        {
            var result = new List<ReadOnlyMemory<byte>>();
            if (data != null) CollectCustomSections(data, name, result);
            return result;
        }

        private static void CollectCustomSections(ReadOnlyMemory<byte> binary, string name,
            List<ReadOnlyMemory<byte>> result)
        {
            // both components and core modules start with the magic and a 4-byte version
            var span = binary.Span;
            if (span.Length < 8 || span[0] != 0 || span[1] != 'a' || span[2] != 's' || span[3] != 'm') return;
            var isComponent = span[6] != 0 || span[7] != 0;

            var position = 8;
            while (position < span.Length)
            {
                var id = span[position++];
                var size = (int)ReadLeb128(span, ref position);
                var contents = binary.Slice(position, size);
                position += size;

                switch (id)
                {
                    case 0:
                    {
                        var namePosition = 0;
                        var nameLength = (int)ReadLeb128(contents.Span, ref namePosition);
                        if (Encoding.UTF8.GetString(contents.Span.Slice(namePosition, nameLength)) == name)
                            result.Add(contents.Slice(namePosition + nameLength));
                        break;
                    }
                    // core module section and component section of a component
                    case 1 when isComponent:
                    case 4 when isComponent:
                        CollectCustomSections(contents, name, result);
                        break;
                }
            }
        }

        private static ulong ReadLeb128(ReadOnlySpan<byte> span, ref int position)
        {
            ulong result = 0;
            var shift = 0;
            while (true)
            {
                var b = span[position++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
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