using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using Wukuro.Dialog;
using CoreTypes = Wukuro.Core.ITypes;
using DialogTypes = Wukuro.Dialog.ITypes;

namespace Wukuro.Samples
{
    /// <summary>
    ///     A minimal implementation of <see cref="IDialogModule" /> drawn with IMGUI.
    ///     Messages proceed with a click or the Return key, and options are shown as buttons.
    /// </summary>
    public class ImGuiDialogModule : MonoBehaviour, IDialogModule
    {
        private bool isOpen;
        private string speaker = "";
        private string text = "";
        private string[] options;
        private UniTaskCompletionSource<int> waiting;
        private float fade;

        public ValueTask OpenDialog(Option<string> provider)
        {
            isOpen = true;
            return default;
        }

        public ValueTask CloseDialog()
        {
            isOpen = false;
            text = "";
            return default;
        }

        public ValueTask ShowMessage(DialogTypes.MessageInfo message)
        {
            speaker = message.Speaker;
            text = Resolve(message.Text);
            options = null;
            waiting = new UniTaskCompletionSource<int>();
            if (message.Auto) WaitAndProceed(waiting).Forget();
            return new ValueTask(waiting.Task.AsTask());
        }

        public ValueTask<int> ShowOptions(ReadOnlyMemory<CoreTypes.Text> options)
        {
            this.options = options.ToArray().Select(Resolve).ToArray();
            waiting = new UniTaskCompletionSource<int>();
            return new ValueTask<int>(waiting.Task.AsTask());
        }

        public ValueTask FadeIn(float duration)
        {
            fade = 1;
            return default;
        }

        public ValueTask FadeOut(float duration)
        {
            fade = 0;
            return default;
        }

        /// <summary>
        ///     Shows a plain text as it is. A localized text is shown as its table and key; resolve it with the
        ///     localization system of the game in an actual implementation.
        /// </summary>
        private static string Resolve(CoreTypes.Text text)
        {
            return text.Localized is { } id ? $"{{{id.Table}/{id.Key}}}" : text.Plain;
        }

        private static async UniTaskVoid WaitAndProceed(UniTaskCompletionSource<int> source)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.5));
            source.TrySetResult(0);
        }

        private void OnGUI()
        {
            if (fade > 0)
            {
                GUI.color = new Color(0, 0, 0, fade);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (!isOpen) return;

            var area = new Rect(20, Screen.height - 180, Screen.width - 40, 160);
            GUILayout.BeginArea(area, GUI.skin.box);
            if (options != null)
            {
                for (var i = 0; i < options.Length; i++)
                    if (GUILayout.Button(options[i]))
                    {
                        options = null;
                        waiting?.TrySetResult(i);
                    }
            }
            else if (!string.IsNullOrEmpty(text))
            {
                if (!string.IsNullOrEmpty(speaker)) GUILayout.Label($"<b>{speaker}</b>");
                GUILayout.Label(text);
                var e = Event.current;
                if (e.type == EventType.MouseDown ||
                    (e.type == EventType.KeyDown && e.keyCode is KeyCode.Return or KeyCode.KeypadEnter))
                    waiting?.TrySetResult(0);
            }

            GUILayout.EndArea();
        }
    }
}
