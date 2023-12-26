#pragma warning disable CS0414
using System.Linq;
using Coffee.SimpleSceneNavigator.Internal;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Build;
#endif

namespace Coffee.SimpleSceneNavigator
{
    public class SimpleSceneNavigatorProjectSettings : PreloadedProjectSettings<SimpleSceneNavigatorProjectSettings>
    {
        [SerializeField]
        private bool m_NavigatorEnabled = true;

        [SerializeField]
        private bool m_EnabledInEditor = true;

        [SerializeField]
        private bool m_AlwaysIncludeAssembly = true;

        [SerializeField]
        private bool m_InstantiateOnLoad = true;

        [SerializeField]
        private GameObject m_Prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnAfterSceneLoad()
        {
            if (!instance.m_InstantiateOnLoad) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) return;

            if (!instance.IsValid(out var reason))
            {
                Debug.LogWarning($"[SimpleSceneNavigator] SimpleSceneNavigator does not run on load:\n{reason}");
                return;
            }
#endif

            if (!instance.m_Prefab) return;

            var go = Instantiate(instance.m_Prefab);
            DontDestroyOnLoad(go);
        }

        private bool IsValid(out string invalidReason)
        {
            invalidReason = "";
            if (!m_NavigatorEnabled)
            {
                invalidReason +=
                    " - SimpleSceneNavigator is disabled. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }

            if (!m_Prefab)
            {
                invalidReason +=
                    " - SimpleSceneNavigator prefab is not set. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }
#if UNITY_EDITOR
            if (Application.isPlaying && !m_EnabledInEditor)
            {
                invalidReason +=
                    " - SimpleSceneNavigator is disabled in editor. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }
#endif

            return string.IsNullOrEmpty(invalidReason);
        }

#if UNITY_EDITOR
        protected void Reset()
        {
            m_Prefab = AssetDatabase.FindAssets("t:prefab SimpleSceneNavigator")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => x != "Assets/SceneNavigatorLink/SimpleSceneNavigator.prefab")
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault(x => x.TryGetComponent<SimpleSceneNavigator>(out _));

            if (m_Prefab) return;

            if (!AssetDatabase.IsValidFolder("Assets/ProjectSettings"))
            {
                AssetDatabase.CreateFolder("Assets", "ProjectSettings");
            }

            var assetPath = "Assets/ProjectSettings/SimpleSceneNavigator.prefab";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            FileUtil.CopyFileOrDirectory("Packages/com.coffee.simple-scene-navigator/Prefabs~/SimpleSceneNavigator.prefab", assetPath);
            AssetDatabase.ImportAsset(assetPath);
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private class FilterBuildAssemblies : IFilterBuildAssemblies
        {
            public int callbackOrder => 0;

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                if (instance.m_AlwaysIncludeAssembly || instance.IsValid(out var reason))
                {
                    return assemblies;
                }

                var assemblyName = typeof(SimpleSceneNavigatorProjectSettings).Assembly.GetName().Name + ".dll";
                Debug.LogWarning($"[SimpleSceneNavigator] Assembly '{assemblyName}' will be excluded in build. " +
                                 $"SimpleSceneNavigator will not run on load:\n{reason}");
                return assemblies
                    .Where(x => !x.EndsWith(assemblyName))
                    .ToArray();
            }
        }

        [CustomEditor(typeof(SimpleSceneNavigatorProjectSettings))]
        private class SimpleSceneNavigatorProjectSettingsEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (!instance.IsValid(out var reason))
                {
                    var message = $"SimpleSceneNavigator will be excluded in build: \n{reason}";
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/Development/Scene Navigator");
        }
#endif
    }
}
