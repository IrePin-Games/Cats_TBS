using OpenAi.Api.V1;
using OpenAi.Unity.V1;

using System.Threading.Tasks;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace OpenAi.Examples
{
    public class PromptGenerator : EditorWindow
    {
        private string _input = "Senior Unity Developer";
        Vector2 scrollPos = Vector2.zero;
        private string _output;

        public int max_tokens = 512;
        public float temperature = 0.7f;
        public float top_p = 1.0f;
        public string stop;
        public float frequency_penalty = 0;
        public float presences_penalty = 0;
        public string model = "text-davinci-003";
        private string instructions = "I want you to act as a prompt generator. Firstly, I will give you a title like this: “Act as a Senior Unity Game Developer”. Then you give me a prompt like this: “I want you to act as a senior Unity game developer. Your task is to assist me in the development of a game using the Unity game engine. You will be responsible for providing assistance with game design, coding, and debugging. You should have experience in developing games with Unity, and be able to create a game with a high level of quality using the best clean coding practices. Here is your first task: ” (You should adapt the sample prompt according to the title I gave. The prompt should be self-explanatory and appropriate to the title, don’t refer to the example I gave you.). ";
        private bool _showInstructionPrompt;

        [MenuItem("Tools/OpenAi/Prompt Generator")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PromptGenerator));
        }

        async void OnGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Code:", MonoScript.FromScriptableObject(this), typeof(ScriptableObject), false);
            GUI.enabled = true;

            SOAuthArgsV1 auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>("Assets/OpenAI Integration/OpenAi/Runtime/Config/DefaultAuthArgsV1.asset");
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

            if (!string.IsNullOrEmpty(_output))
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
                _output = EditorGUILayout.TextArea(_output.Replace("\\\\", "\n"), GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();                
            }
            
            EditorGUILayout.LabelField("Act as a... ");
            _input = EditorGUILayout.TextField(_input, EditorStyles.textField);
            EditorStyles.textField.wordWrap = true;

            if (api != null && GUILayout.Button("Generate Instruction Prompt"))
            {
                Debug.Log("Performing Completion in Editor Time using the following input:");
                await DoEditorTask(api);
            }

        }

        private async Task DoEditorTask(OpenAiApiV1 api)
        {
            _output = "Generating your prompt...";

            ApiResult<CompletionV1> comp = await api.Engines.Engine(model).Completions.CreateCompletionAsync(
                    new CompletionRequestV1()
                    {
                        prompt = instructions + "Act as a " + _input + "(Give me prompt only):",
                        max_tokens = max_tokens,
                        temperature = temperature,
                        top_p = top_p,
                        stop = stop,
                        frequency_penalty = frequency_penalty,
                        presence_penalty = presences_penalty

                    }
                );
            Debug.Log(instructions + _input);
            _input = "Senior Unity Developer";
            if (comp.IsSuccess)
            {
                Debug.Log($"{comp.Result.choices[0].text}" + " Respond only as if you were this character.");
                _output = $"{comp.Result.choices[0].text}" + " Respond only as if you were this character.";
            }
            else
            {
                _output = $"ERROR: StatusCode={comp.HttpResponse.responseCode} - {comp.HttpResponse.error}";
            }
        }
    }
}