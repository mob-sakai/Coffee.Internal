using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace Coffee.OpenSesame
{
    internal class OpenSesameSettings : ScriptableSingleton<OpenSesameSettings>
    {
        private const string k_Path = "ProjectSettings/OpenSesame.asset";
        private static Editor s_Editor;

        [SerializeField]
        private bool m_CompileOnLoad;

        [SerializeField]
        private OpenSesameInfo[] m_AssemblyInfos = Array.Empty<OpenSesameInfo>();

        [HideInInspector]
        [SerializeField]
        private bool m_WillCompileOnLoad;

        private void OnEnable()
        {
            if (File.Exists(k_Path))
            {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(k_Path), this);
            }

            hideFlags &= ~HideFlags.NotEditable;
            EditorUtility.SetDirty(this);
        }

        private void StartCompileAll()
        {
            foreach (var info in m_AssemblyInfos)
            {
                info.StartCompile();
            }
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (instance.m_WillCompileOnLoad)
            {
                instance.m_WillCompileOnLoad = false;
                instance.StartCompileAll();
            }

            CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
            {
                if (!instance.m_CompileOnLoad
                    || instance.m_WillCompileOnLoad
                    || BuildPipeline.isBuildingPlayer
                    || Application.isBatchMode
                    || messages.Any(x => x.type == CompilerMessageType.Error))
                {
                    return;
                }

                var name = Path.GetFileNameWithoutExtension(path);
                instance.m_WillCompileOnLoad = instance.m_AssemblyInfos
                    .Any(x => x.GetAssemblyName() == name);
            };
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Development/Open Sesame", SettingsScope.Project)
            {
                guiHandler = _ =>
                {
                    Editor.CreateCachedEditor(instance, null, ref s_Editor);
                    if (s_Editor.DrawDefaultInspector())
                    {
                        File.WriteAllText(k_Path, EditorJsonUtility.ToJson(instance, true));
                    }

                    if (GUILayout.Button("Compile All"))
                    {
                        instance.StartCompileAll();
                    }
                }
            };
        }
    }

    [Serializable]
    internal class OpenSesameInfo
    {
        [SerializeField]
        private AssemblyDefinitionAsset m_SrcAssembly;

        [SerializeField]
        private string m_DstAssemblyPath;

        [SerializeField]
        private CompileOptions m_Options;

        internal void StartCompile()
        {
            var assemblyName = GetAssemblyName(m_SrcAssembly);
            if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(m_DstAssemblyPath)) return;

            Compiler.Build(assemblyName, m_DstAssemblyPath, m_Options);
        }


        internal string GetAssemblyName()
        {
            return GetAssemblyName(m_SrcAssembly);
        }

        internal static string GetAssemblyName(AssemblyDefinitionAsset definition)
        {
            if (!definition) return string.Empty;

            var match = Regex.Match(definition.text, "\"name\":\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }

    [CustomPropertyDrawer(typeof(OpenSesameInfo))]
    public class PublishInfoDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 18 * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var p = new Rect(position.x, position.y, position.width, 16);
            var srcAssembly = property.FindPropertyRelative("m_SrcAssembly");
            var dstPath = property.FindPropertyRelative("m_DstAssemblyPath");

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(p, srcAssembly);
            if (EditorGUI.EndChangeCheck())
            {
                dstPath.stringValue = "";
                if (srcAssembly.objectReferenceValue is AssemblyDefinitionAsset asmdef)
                {
                    var asmdefPath = AssetDatabase.GetAssetPath(asmdef);
                    var asmName = OpenSesameInfo.GetAssemblyName(asmdef);
                    var isSame = Path.GetFileNameWithoutExtension(asmdefPath) == asmName;
                    dstPath.stringValue = Path.ChangeExtension(asmdefPath, isSame ? ".generated.dll" : ".dll");
                }
            }

            p.y += 18;
            EditorGUI.PropertyField(p, dstPath);

            p.y += 18;
            EditorGUI.PropertyField(p, property.FindPropertyRelative("m_Options"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
