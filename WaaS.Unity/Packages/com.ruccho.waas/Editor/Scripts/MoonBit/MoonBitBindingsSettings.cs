using System;
using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Settings to generate MoonBit bindings of WIT interfaces into a MoonBit module.
    ///     The bindings are generated after the WIT export of every script compilation, and only changed files are written.
    /// </summary>
    [CreateAssetMenu(menuName = "WaaS/MoonBit Bindings Settings", fileName = "MoonBitBindingsSettings")]
    public class MoonBitBindingsSettings : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Directory that contains the WIT packages, each in its own subdirectory. Relative to the project root.")]
        private string witDirectory = "wit";

        [SerializeField] [Tooltip("Interfaces to generate the bindings of, such as \"my-namespace:my-package/my-interface\".")]
        private string[] interfaces = { };

        [SerializeField]
        [Tooltip("Directory to write the bindings to, which is used only by the bindings. Relative to the project root.")]
        private string outputDirectory = "moonbit/bindings";

        [SerializeField]
        [Tooltip("Package path of the output directory, such as \"my-name/my-module/bindings\".")]
        private string packagePath = "";

        [SerializeField] [Tooltip("Interfaces whose bindings are provided by other packages.")]
        private ExternalInterface[] externalInterfaces = { };

        public string WitDirectory => witDirectory;
        public string[] Interfaces => interfaces;
        public string OutputDirectory => outputDirectory;
        public string PackagePath => packagePath;
        public ExternalInterface[] ExternalInterfaces => externalInterfaces;

        [Serializable]
        public class ExternalInterface
        {
            [SerializeField] [Tooltip("Interface, such as \"my-namespace:my-package/my-interface\".")]
            private string @interface = "";

            [SerializeField] [Tooltip("Package path of the bindings of the interface.")]
            private string packagePath = "";

            public string Interface => @interface;
            public string PackagePath => packagePath;
        }
    }
}
