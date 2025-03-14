using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Coffee.SymbolicLinker
{
    [Serializable]
    internal class SymbolicLinkInfo
    {
        public string m_From;
        public string m_To;
        public string m_Define;
    }

    internal class SymbolicLinkerSettings : ScriptableSingleton<SymbolicLinkerSettings>
    {
        private const string k_Path = "ProjectSettings/SymbolicLinkerSettings.json";
        private static Editor s_Editor;
        private static HashSet<string> s_Defines = null;

        [SerializeField]
        public SymbolicLinkInfo[] m_SymbolicLinks = Array.Empty<SymbolicLinkInfo>();

        private void OnEnable()
        {
            // Load settings.
            if (File.Exists(k_Path))
            {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(k_Path), this);
            }
        }

        private static bool IsConditional(string define)
        {
            if (string.IsNullOrEmpty(define)) return true;

            if (s_Defines == null)
            {
                var assemblyName = typeof(SymbolicLinkerSettings).Assembly.GetName().Name;
                s_Defines = new HashSet<string>(CompilationPipeline.GetDefinesFromAssemblyName(assemblyName));
            }

            return define[0] != '!'
                ? s_Defines.Contains(define)
                : !s_Defines.Contains(define.Substring(1));
        }

        [InitializeOnLoadMethod]
        private static void Apply()
        {
            foreach (var info in instance.m_SymbolicLinks)
            {
                if (IsConditional(info.m_Define))
                {
                    if (!Directory.Exists(info.m_From))
                    {
                        CreateSymbolicLink(info.m_From, info.m_To);
                    }
                }
                else
                {
                    var meta = info.m_From + ".meta";
                    if (File.Exists(meta))
                    {
                        File.Delete(meta);
                    }

                    if (Directory.Exists(info.m_From))
                    {
                        Debug.Log($"[SymbolicLink] Delete: {info.m_From}");
                        Directory.Delete(info.m_From);
                    }
                }
            }
        }

        private static void CreateSymbolicLink(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;

#if UNITY_EDITOR_WIN
            var bash = @"C:\Program Files\Git\bin\bash.exe";
            if (!File.Exists(bash))
            {
                bash = @"C:\Program Files (x86)\Git\bin\bash.exe";
            }
#else
            var bash = "/bin/bash";
#endif

            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    Arguments = $"-c \"ln -s -f '{to}' Temp && mv 'Temp/{Path.GetFileName(to)}' '{from}'\"",
                    CreateNoWindow = true,
                    FileName = bash,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = $"{Application.dataPath}/..",
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            p.Exited += (_, __) =>
            {
                var result = new StringBuilder();
                var stdout = p.StandardOutput.ReadToEnd();
                result.AppendLine($"code: {p.ExitCode}");
                result.Append("stdout: ");
                result.Append(stdout);
                result.AppendLine();
                result.Append("stderr: ");
                result.Append(p.StandardError.ReadToEnd());
                Debug.Log(result);

                if (p.ExitCode == 0)
                {
                    Debug.Log($"[SymbolicLink] Create: {to} -> {from}");
                    AssetDatabase.ImportAsset(from, ImportAssetOptions.ImportRecursive);
                }
            };

            p.Start();
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Development/Symbolic Linker", SettingsScope.Project)
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

                    if (GUILayout.Button("Apply"))
                    {
                        Apply();
                    }
                }
            };
        }
    }

    [CustomPropertyDrawer(typeof(SymbolicLinkInfo))]
    internal class SymbolicLinkInfoDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 18 * 3;
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            EditorGUI.BeginProperty(pos, label, property);
            var p = new Rect(pos.x, pos.y, pos.width, 16);
            EditorGUI.PropertyField(p, property.FindPropertyRelative("m_From"));

            p.y += 18;
            EditorGUI.PropertyField(p, property.FindPropertyRelative("m_To"));

            p.y += 18;
            EditorGUI.PropertyField(p, property.FindPropertyRelative("m_Define"));
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
