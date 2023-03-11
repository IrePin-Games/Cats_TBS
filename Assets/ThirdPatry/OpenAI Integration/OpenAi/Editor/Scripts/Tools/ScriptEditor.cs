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
    public class ScriptEditor : EditorWindow
    {
        private string _input = "Explain the issues you're having with this script.";
        Vector2 scrollPos = Vector2.zero;
        private string _output;
        private string recommendations;

        public int max_tokens = 2048;
        public float temperature = 0.2f;
        public float top_p = 0.8f;
        public string stop;
        public float frequency_penalty = 0;
        public float presences_penalty = 0;
        public string model = "text-davinci-003";
        private string instructions = "I want you to act as a senior Unity game developer. Your task is to assist me in the development of a game using the Unity game engine. You will be responsible for providing assistance with game design, coding, and debugging. You should have experience in developing games with Unity, and be able to create a game with a high level of quality using the best clean coding practices. Your first task is to identify any possible bugs and provide solutions in the script I provide you. If you have any recommendations, include the code for them in your response. My request is: ";
        private string scriptFilePath;
        private string scriptText;
        private MonoScript selectedObject;
        private bool showButton;

        [MenuItem("Tools/OpenAi/Script Editor")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ScriptEditor));
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
            
            selectedObject = (MonoScript)EditorGUILayout.ObjectField("File", selectedObject, typeof(MonoScript), false);

            if (selectedObject != null)
            {
                scriptFilePath = AssetDatabase.GetAssetPath(selectedObject);
            }

            EditorGUILayout.LabelField("Bug description: ");
            _input = EditorGUILayout.TextArea(_input, EditorStyles.textField, GUILayout.MinHeight(80));
            EditorStyles.textField.wordWrap = true;

            if (api != null && GUILayout.Button("Identify issues"))
            {
                Debug.Log("Performing Completion in Editor Time using the following input:");
                await DoEditorTask(api);
            }

            if (showButton)
            {
                if (GUILayout.Button("Apply recommendations"))
                {
                    // Send another request with the recommendatiosn and instruct it to rewrite the script with the recommendations in only code
                    recommendations = _output;
                    await UpdateEditorFile(api);
                }
            }

        }

        private async Task DoEditorTask(OpenAiApiV1 api)
        {
            _output = "Analyzing script...";
            TextAsset scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptFilePath);
            scriptText = scriptAsset.text;
            ApiResult<CompletionV1> comp = await api.Engines.Engine(model).Completions.CreateCompletionAsync(
                    new CompletionRequestV1()
                    {
                        prompt = instructions + _input + "Here is the script: \n" + scriptText,
                        max_tokens = max_tokens,
                        temperature = temperature,
                        top_p = top_p,
                        stop = stop,
                        frequency_penalty = frequency_penalty,
                        presence_penalty = presences_penalty

                    }
                );
            Debug.Log(instructions + _input);
            _input = "Explain the issues you're having with this script.";
            if (comp.IsSuccess)
            {
                Debug.Log($"{comp.Result.choices[0].text}");
                _output = $"{comp.Result.choices[0].text}";
                showButton = true;
            }
            else
            {
                _output = $"ERROR: StatusCode={comp.HttpResponse.responseCode} - {comp.HttpResponse.error}";
            }
        }

        private async Task UpdateEditorFile(OpenAiApiV1 api)
        {
            //_output = "Applying changes...";
            TextAsset scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptFilePath);
            scriptText = scriptAsset.text;
            ApiResult<CompletionV1> comp = await api.Engines.Engine(model).Completions.CreateCompletionAsync(
                    new CompletionRequestV1()
                    {
                        prompt = "Original script:\n" + scriptText + "\n" + "Instructions: Rewrite this entire script based on the following recommendations: '" + recommendations + ".' Only include the rewritten code in your response. Do not include any extra explainations or text:\n",
                        max_tokens = max_tokens,
                        temperature = temperature,
                        top_p = top_p,
                        stop = stop,
                        frequency_penalty = frequency_penalty,
                        presence_penalty = presences_penalty

                    }
                );
            _input = "Explain the issues you're having with this script.";
            if (comp.IsSuccess)
            {
                _output = $"{comp.Result.choices[0].text}";
                showButton = false;
                _output = _output.Replace("\\\\", "\n");
                Debug.Log("Rewriting file with the following code: " + _output);
                File.WriteAllText(scriptFilePath, _output);
                _output = "Rewritten code:\n" + _output;
                AssetDatabase.Refresh();
            }
            else
            {
                _output = $"ERROR: StatusCode={comp.HttpResponse.responseCode} - {comp.HttpResponse.error}";
            }
        }
    }
}