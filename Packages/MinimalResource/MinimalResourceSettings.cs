using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("Coffee.MinimalResource.Test")]

namespace Coffee.MinimalResource
{
    internal class MinimalResourceSettings : ScriptableSingleton<MinimalResourceSettings>
    {
        private const string k_Path = "ProjectSettings/MinimalResourceSettings.json";
        private static Editor s_Editor;

        [SerializeField]
        public string[] m_OutputDllPaths = Array.Empty<string>();

        private void OnEnable()
        {
            // Load settings.
            if (File.Exists(k_Path))
            {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(k_Path), this);
            }
        }

        private void StartCompileAll()
        {
            foreach (var outPath in m_OutputDllPaths)
            {
                Compiler.Build(outPath);
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Development/Minimal Resource", SettingsScope.Project)
            {
                guiHandler = _ =>
                {
                    // Draw settings.
                    instance.hideFlags &= ~HideFlags.NotEditable;
                    Editor.CreateCachedEditor(instance, null, ref s_Editor);
                    if (s_Editor.DrawDefaultInspector())
                    {
                        // If settings changed, save it.
                        File.WriteAllText(k_Path, EditorJsonUtility.ToJson(instance, true));
                    }

                    // Compile all button.
                    if (GUILayout.Button("Compile All"))
                    {
                        instance.StartCompileAll();
                    }
                }
            };
        }
    }
}
