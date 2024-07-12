#pragma warning disable CS0414
using System.Linq;
using System.Text.RegularExpressions;
using Coffee.NanoMonitor.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditorInternal;
#endif

namespace Coffee.NanoMonitor
{
    public class NanoMonitorProjectSettings : PreloadedProjectSettings<NanoMonitorProjectSettings>
    {
        [Header("Condition")]
        [SerializeField]
        private bool m_NanoMonitorEnabled = true;

        [SerializeField]
        private string m_BootSceneNameRegex = ".*";

        [SerializeField]
        private bool m_DevelopmentBuildOnly = true;

        [SerializeField]
        private bool m_EnabledInEditor = true;

        [SerializeField]
        private bool m_AlwaysIncludeAssembly = true;

        [SerializeField]
        private bool m_InstantiateOnLoad = true;

        [Header("Settings")]
        [SerializeField]
        private GameObject m_Prefab;

        [SerializeField]
        [Range(0.01f, 2f)]
        private float m_Interval = 0.5f;

        [SerializeField]
        private Image.OriginVertical m_Anchor = Image.OriginVertical.Top;

        [SerializeField]
        [Range(750, 1000)]
        private int m_Width = 750;

        [HideInInspector]
        [SerializeField]
        private CustomMonitorItem[] m_CustomMonitorItems =
        {
            new CustomMonitorItem("Screen:{0}x{1}", (typeof(Screen), "width"), (typeof(Screen), "height"))
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnAfterSceneLoad()
        {
            if (!instance.m_InstantiateOnLoad) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) return;

            var development = EditorUserBuildSettings.development;
            if (!instance.IsValid(SceneManager.GetActiveScene().name, development, out var reason))
            {
                Debug.LogWarning($"[NanoMonitor] NanoMonitor does not run on load:\n{reason}");
                return;
            }
#endif

            instance.InstantiateOnLoad();
        }

        private void InstantiateOnLoad()
        {
            if (!instance.m_Prefab) return;

            var go = Instantiate(instance.m_Prefab);
            go.GetComponent<NanoMonitor>().SetUp(m_Anchor, m_Interval, m_CustomMonitorItems, m_Width);
            DontDestroyOnLoad(go);
        }

        private bool IsValid(string bootSceneName, bool development, out string invalidReason)
        {
            invalidReason = "";
            if (!m_NanoMonitorEnabled)
            {
                invalidReason += " - NanoMonitor is disabled. (See Edit>Project Settings>Nano Monitor)\n";
            }

            if (!m_Prefab)
            {
                invalidReason += " - NanoMonitor prefab is not set. (See Edit>Project Settings>Nano Monitor)\n";
            }

            if (m_DevelopmentBuildOnly && !development)
            {
                invalidReason += " - Development build only.\n";
            }

            if (!Regex.IsMatch(bootSceneName, m_BootSceneNameRegex))
            {
                invalidReason +=
                    $" - Boot scene name '{bootSceneName}' does not match regex '{m_BootSceneNameRegex}'.\n";
            }
#if UNITY_EDITOR
            if (Application.isPlaying && !m_EnabledInEditor)
            {
                invalidReason += " - NanoMonitor is disabled in editor. (See Edit>Project Settings>Nano Monitor)\n";
            }
#endif

            return string.IsNullOrEmpty(invalidReason);
        }

#if UNITY_EDITOR
        protected void Reset()
        {
            m_Prefab = AssetDatabase.FindAssets("t:prefab NanoMonitor")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => x != "Assets/NanoMonitorLink/NanoMonitor.prefab")
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault(x => x.TryGetComponent<NanoMonitor>(out _));

            if (m_Prefab) return;

            if (!AssetDatabase.IsValidFolder("Assets/ProjectSettings"))
            {
                AssetDatabase.CreateFolder("Assets", "ProjectSettings");
            }

            var assetPath = "Assets/ProjectSettings/NanoMonitor.prefab";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            FileUtil.CopyFileOrDirectory("Packages/com.coffee.nano-monitor/Prefabs~/NanoMonitor.prefab", assetPath);
            AssetDatabase.ImportAsset(assetPath);
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            EditorUtility.SetDirty(this);
        }

        private static string GetBootSceneName()
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scene = SceneManager.GetSceneByBuildIndex(i);
                if (!scene.IsValid()) continue;

                return scene.name;
            }

            return "";
        }

        private class FilterBuildAssemblies : IFilterBuildAssemblies
        {
            public int callbackOrder => 0;

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                var development = 0 != (buildOptions & BuildOptions.Development);
                if (instance.m_AlwaysIncludeAssembly ||
                    instance.IsValid(GetBootSceneName(), development, out var reason))
                {
                    return assemblies;
                }

                var assemblyName = typeof(NanoMonitor).Assembly.GetName().Name + ".dll";
                Debug.LogWarning($"[NanoMonitor] Assembly '{assemblyName}' will be excluded in build. " +
                                 $"NanoMonitor will not run on load:\n{reason}");
                return assemblies
                    .Where(x => !x.EndsWith(assemblyName))
                    .ToArray();
            }
        }

        [CustomEditor(typeof(NanoMonitorProjectSettings))]
        private class NanoMonitorProjectSettingsEditor : Editor
        {
            private ReorderableList _itemsRo;

            private void OnEnable()
            {
                var sp = serializedObject.FindProperty("m_CustomMonitorItems");
                _itemsRo = CustomMonitorItemDrawer.CreateReorderableList(sp);
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                _itemsRo.DoLayoutList();
                serializedObject.ApplyModifiedProperties();

                var development = EditorUserBuildSettings.development;
                if (!instance.IsValid(GetBootSceneName(), development, out var reason))
                {
                    var message = $"NanoMonitor will be excluded in build: \n{reason}";
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/Development/Nano Monitor");
        }
#endif
    }
}
