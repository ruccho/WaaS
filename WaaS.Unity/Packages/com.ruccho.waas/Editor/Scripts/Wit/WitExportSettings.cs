using UnityEngine;

namespace WaaS.Unity.Editor.Wit
{
    /// <summary>
    ///     Settings to export WIT files generated from C# interfaces.
    ///     Each package is written to &lt;output directory&gt;/&lt;package&gt;/&lt;package&gt;.wit.
    /// </summary>
    [CreateAssetMenu(menuName = "WaaS/WIT Export Settings", fileName = "WitExportSettings")]
    public class WitExportSettings : ScriptableObject
    {
        [SerializeField] [Tooltip("Directory relative to the project root.")]
        private string outputDirectory = "wit";

        [SerializeField] [Tooltip("Packages to export, such as \"my-namespace:my-package\".")]
        private string[] packages = { };

        public string OutputDirectory => outputDirectory;
        public string[] Packages => packages;
    }
}
