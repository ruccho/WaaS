﻿using System;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;

namespace WaaS.Unity
{
    public class ComponentAsset : ScriptableObject
    {
        [SerializeField] private bool deserializeOnLoad;
        [SerializeField, HideInInspector] private byte[] data;
        [NonSerialized] private Lazy<IComponent> component;

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
    }
}