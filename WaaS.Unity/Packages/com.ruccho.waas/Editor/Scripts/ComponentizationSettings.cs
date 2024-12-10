using System;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    [Serializable]
    public class ComponentizationSettings
    {
        [SerializeField] private string witDirectory = "wit";
        [SerializeField] private string world;

        public string WitDirectory => witDirectory;
        public string World => world;
    }
}