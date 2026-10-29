using Cysharp.Threading.Tasks;
using UnityEngine;
using Wukuro.Dialog;

namespace Wukuro.Samples
{
    /// <summary>
    ///     Runs an entry point of a script with <see cref="ImGuiDialogModule" /> when the scene starts.
    /// </summary>
    public class SampleEventRunner : MonoBehaviour
    {
        [SerializeField] private WukuroEntryReference entry = new();
        [SerializeField] private ImGuiDialogModule dialog;

        // where the variables of the script are stored
        private readonly MemoryVariables variables = new();

        private void Start()
        {
            // registers the implementation of wukuro:dialog; wukuro:core is registered automatically
            DialogImports.Register();
            PlayAsync().Forget();
        }

        private async UniTaskVoid PlayAsync()
        {
            var runner = new WukuroRunner();
            runner.AddModule(dialog);
            await runner.RunAsync(entry, variables);
            Debug.Log("[Wukuro sample] finished");
        }
    }
}
