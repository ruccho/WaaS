using System.Collections.Generic;
using System.Threading.Tasks;
using MyGame.MySequencer;
using UnityEngine;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using WaaS.Unity;

public class RunSequenceTest : MonoBehaviour
{
    [SerializeField] private ComponentAsset componentAsset;

    private async void Start()
    {
        var component = await componentAsset.LoadComponentAsync();
        var instance = component.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "my-game:my-sequencer/env", IEnv.CreateWaaSInstance(new Env()) }
        });
        using var context = new ExecutionContext();
        var sequence = new ISequence.Wrapper(instance, context);
        await sequence.Play(); // ぼく「こんにちは！」
    }

    private class Env : IEnv
    {
        public ValueTask ShowMessage(string speaker, string message)
        {
            Debug.Log($"{speaker}「{message}」");
            return default;
        }
    }
}