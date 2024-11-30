using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

[assembly: InternalsVisibleTo("Coffee.OpenSesame.Test")]

namespace Coffee.OpenSesame
{
    internal class AutoExplicitly : AssetPostprocessor
    {
        private static readonly PropertyInfo s_PiIsExplicitlyReferenced = typeof(PluginImporter)
            .GetProperty("IsExplicitlyReferenced", BindingFlags.Instance | BindingFlags.NonPublic);

        private void OnPreprocessAsset()
        {
            if (assetImporter is PluginImporter pluginImporter
                && OpenSesameSettings.instance.m_AssemblyInfos
                    .Any(x => x.m_DstAssemblyPath == assetImporter.assetPath))
            {
                s_PiIsExplicitlyReferenced.SetValue(pluginImporter, true);
            }
        }
    }

    internal class AutoCompilation : ScriptableSingleton<AutoCompilation>
    {
        public bool m_EnableForThisSession;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
            {
                // Skip if not in auto compile mode.
                //  - EnableForThisSession is false.
                //  - Building player.
                //  - In batch mode.
                //  - Compilation has errors.
                if (!instance.m_EnableForThisSession
                    || BuildPipeline.isBuildingPlayer
                    || Application.isBatchMode
                    || messages.Any(x => x.type == CompilerMessageType.Error))
                {
                    return;
                }

                // Find the assembly info by the assembly name.
                var assemblyName = Path.GetFileNameWithoutExtension(path);
                var info = OpenSesameSettings.instance.m_AssemblyInfos
                    .FirstOrDefault(x => x.GetAssemblyName() == assemblyName);

                // Start compile.
                info?.StartCompile();
            };
        }
    }

    internal class OpenSesameSettings : ScriptableSingleton<OpenSesameSettings>
    {
        private const string k_Path = "ProjectSettings/OpenSesameSettings.json";
        private static Editor s_Editor;

        [SerializeField]
        public OpenSesameInfo[] m_AssemblyInfos = Array.Empty<OpenSesameInfo>();

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
            foreach (var info in m_AssemblyInfos)
            {
                info.StartCompile();
            }
        }

        internal void StartCompile(AssemblyDefinitionAsset asmdef)
        {
            if (!asmdef) return;
            var assemblyName = OpenSesameInfo.GetAssemblyName(asmdef);
            var info = m_AssemblyInfos.FirstOrDefault(x => x.GetAssemblyName() == assemblyName);
            info?.StartCompile();
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Development/Open Sesame", SettingsScope.Project)
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

                    // Auto compile for this session.
                    AutoCompilation.instance.m_EnableForThisSession =
                        EditorGUILayout.ToggleLeft("Enable Auto Compilation For This Session",
                            AutoCompilation.instance.m_EnableForThisSession);

                    // Compile all button.
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
        public string m_DstAssemblyPath;

        [SerializeField]
        private CompileOptions m_Options = CompileOptions.Release;

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
            var srcAssembly = property.FindPropertyRelative("m_SrcAssembly");
            return srcAssembly.objectReferenceValue is AssemblyDefinitionAsset
                ? 18 * 3
                : 18;
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
            }

            var asmdef = srcAssembly.objectReferenceValue as AssemblyDefinitionAsset;
            if (!asmdef) return;

            p.y += 18;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(p, dstPath);
            if (EditorGUI.EndChangeCheck() || string.IsNullOrEmpty(dstPath.stringValue))
            {
                var asmdefPath = AssetDatabase.GetAssetPath(asmdef);
                var asmName = OpenSesameInfo.GetAssemblyName(asmdef);
                if (string.IsNullOrEmpty(dstPath.stringValue))
                {
                    var isSame = Path.GetFileNameWithoutExtension(asmdefPath) == asmName;
                    dstPath.stringValue = Path.ChangeExtension(asmdefPath, isSame ? ".mod.dll" : ".dll");
                }
                else
                {
                    var isSame = Path.GetFileNameWithoutExtension(dstPath.stringValue) == asmName;
                    if (isSame)
                    {
                        dstPath.stringValue = Path.ChangeExtension(dstPath.stringValue, ".mod.dll");
                    }
                }
            }

            p.y += 18;
            p.width -= 72;
            EditorGUI.PropertyField(p, property.FindPropertyRelative("m_Options"));

            p.x += p.width + 2;
            p.width = 70;
            if (GUI.Button(p, "Compile", EditorStyles.miniButton))
            {
                OpenSesameSettings.instance.StartCompile(asmdef);
            }
        }
    }
}
