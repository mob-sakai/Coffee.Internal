using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using Object = UnityEngine.Object;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Coffee.Internal
{
    [Serializable]
    public class ShaderVariantRegistry
    {
        [Serializable]
        public class StringPair
        {
            public string key;
            public string value;
        }

        private Dictionary<int, string> _cachedAliases = new Dictionary<int, string>();

        [SerializeField]
        private bool m_ErrorWhenFallback = false;

        [SerializeField]
        private List<StringPair> m_ShaderAliases = new List<StringPair>();

        [SerializeField]
        private List<StringPair> m_UnregisteredVariants = new List<StringPair>();

        [SerializeField]
        internal ShaderVariantCollection m_Asset;

        public ShaderVariantCollection shaderVariantCollection => m_Asset;

        public Shader FindAliasShader(Shader shader, string aliasNameFormat, string defaultAliasName)
        {
            if (!shader) return null;

            // Already cached.
            var id = shader.GetInstanceID();
            if (_cachedAliases.TryGetValue(id, out var aliasName))
            {
                return Shader.Find(aliasName);
            }

            // Find alias.
            Shader aliasShader;
            var shaderName = shader.name;
            foreach (var alias in m_ShaderAliases)
            {
                if (alias.key != shaderName) continue;
                aliasShader = Shader.Find(alias.value);
                if (aliasShader)
                {
                    _cachedAliases[id] = alias.value;
                    return aliasShader;
                }
            }

            // Find alias by format.
            aliasName = string.Format(aliasNameFormat, shader.name);
            aliasShader = Shader.Find(aliasName);
            if (aliasShader)
            {
                _cachedAliases[id] = aliasName;
                return aliasShader;
            }

            // Find default alias.
            _cachedAliases[id] = defaultAliasName;
            return Shader.Find(defaultAliasName);
        }

#if UNITY_EDITOR
        public void InitializeIfNeeded(Object owner)
        {
            if (!m_Asset && AssetDatabase.IsMainAsset(owner))
            {
                m_Asset = new ShaderVariantCollection()
                {
                    name = "ShaderVariants"
                };
                AssetDatabase.AddObjectToAsset(m_Asset, owner);
                EditorUtility.SetDirty(owner);
                AssetDatabase.SaveAssets();
            }
        }

        public void InitializeIfNeeded(Object owner, ShaderVariantCollection collection)
        {
            if (!m_Asset && AssetDatabase.IsMainAsset(owner))
            {
                m_Asset = collection;
                EditorUtility.SetDirty(owner);
            }
        }

        internal void RegisterVariant(Material material, string path)
        {
            if (!material || !material.shader || !m_Asset) return;

            var shaderName = material.shader.name;
            var keywords = string.Join("|", material.shaderKeywords);
            var variant = new ShaderVariantCollection.ShaderVariant
            {
                shader = material.shader,
                keywords = material.shaderKeywords
            };
            Predicate<StringPair> match = x => x.key == shaderName && x.value == keywords;

            // Already registered.
            if (m_Asset.Contains(variant))
            {
                m_UnregisteredVariants.RemoveAll(match);
                return;
            }

            // Error when unregistered variant.
            if (m_ErrorWhenFallback)
            {
                if (m_UnregisteredVariants.Find(match) == null)
                {
                    m_UnregisteredVariants.Add(new StringPair() { key = shaderName, value = keywords });
                }

                keywords = string.IsNullOrEmpty(keywords) ? "no keywords" : keywords;
                Debug.LogError($"Shader variant '{shaderName} <{keywords}>' is not registered.\n" +
                               $"Register it in 'ProjectSettings > {path}' to use it in player.",
                    m_Asset);
                return;
            }

            m_Asset.Add(variant);
            m_UnregisteredVariants.RemoveAll(match);
        }
#endif
    }

#if UNITY_EDITOR
    internal class ShaderRegistryEditor
    {
        private static readonly MethodInfo s_MiDrawShaderEntry =
            Type.GetType("UnityEditor.ShaderVariantCollectionInspector, UnityEditor")
                ?.GetMethod("DrawShaderEntry", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly SerializedProperty _errorWhenFallback;
        private readonly SerializedProperty _asset;
        private readonly ReorderableList _rlShaderAliases;
        private readonly ReorderableList _rlUnregisteredVariants;
        private Editor _editor;
        private bool _expand;

        public ShaderRegistryEditor(SerializedProperty property, string optionName)
        {
            var so = property.serializedObject;
            var shaderAliases = property.FindPropertyRelative("m_ShaderAliases");
            var unregisteredVariants = property.FindPropertyRelative("m_UnregisteredVariants");
            _errorWhenFallback = property.FindPropertyRelative("m_ErrorWhenFallback");
            _asset = property.FindPropertyRelative("m_Asset");

            _rlShaderAliases = new ReorderableList(so, shaderAliases, false, true, true, true);
            _rlShaderAliases.drawHeaderCallback = rect =>
            {
                var rLabel = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
                EditorGUI.LabelField(rLabel, $"Optional Shader Aliases {optionName}");

                var rButton = new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height - 4);
                if (GUI.Button(rButton, "Clear All", EditorStyles.miniButton))
                {
                    shaderAliases.ClearArray();
                }
            };
            _rlShaderAliases.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 4;
            _rlShaderAliases.drawElementCallback = (r, index, isActive, isFocused) =>
            {
                if (shaderAliases.arraySize <= index) return;

                var element = shaderAliases.GetArrayElementAtIndex(index);
                if (element == null) return;

                var key = element.FindPropertyRelative("key");
                var value = element.FindPropertyRelative("value");
                var h = EditorGUIUtility.singleLineHeight;
                var rKey = new Rect(r.x, r.y + 2, r.width, h);
                if (GUI.Button(rKey, key.stringValue, EditorStyles.popup))
                {
                    ShowShaderDropdown(r, key, optionName, false);
                }

                var rArrow = new Rect(r.x, r.y + h + 4, 20, h);
                EditorGUI.LabelField(rArrow, "->");

                var rValue = new Rect(r.x + 20, r.y + h + 4, r.width - 20, h);
                if (GUI.Button(rValue, value.stringValue, EditorStyles.popup))
                {
                    ShowShaderDropdown(r, value, optionName, true);
                }
            };

            _rlUnregisteredVariants = new ReorderableList(so, unregisteredVariants, false, true, false, true);
            _rlUnregisteredVariants.drawHeaderCallback = rect =>
            {
                var rLabel = new Rect(rect.x, rect.y, 200, rect.height);
                EditorGUI.LabelField(rLabel, "Unregistered Shader Variants");

                var rWarning = new Rect(rect.x + 170, rect.y, 20, rect.height);
                var icon = EditorGUIUtility.TrIconContent("warning",
                    "These variants are not registered.\nRegister them to use in player.");
                EditorGUI.LabelField(rWarning, icon);

                var rButton = new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height - 4);
                if (GUI.Button(rButton, "Clear All", EditorStyles.miniButton))
                {
                    unregisteredVariants.ClearArray();
                }
            };
            _rlUnregisteredVariants.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 4;
            _rlUnregisteredVariants.drawElementCallback = (r, index, isActive, isFocused) =>
            {
                if (unregisteredVariants.arraySize <= index) return;

                var element = unregisteredVariants.GetArrayElementAtIndex(index);
                if (element == null) return;

                var key = element.FindPropertyRelative("key");
                var value = element.FindPropertyRelative("value");

                var h = EditorGUIUtility.singleLineHeight;
                var rKey = new Rect(r.x, r.y + 2, r.width, h);
                EditorGUI.LabelField(rKey, key.stringValue, EditorStyles.popup);

                var rValue = new Rect(r.x + 20, r.y + h + 5, r.width - 40, 14);
                var keywords = string.IsNullOrEmpty(value.stringValue) ? "<no keywords>" : value.stringValue;
                EditorGUI.TextField(rValue, GUIContent.none, keywords, "LODRenderersText");

                var rButton = new Rect(r.x + r.width - 20, r.y + h + 4, 20, h);
                if (GUI.Button(rButton, EditorGUIUtility.IconContent("icons/toolbar plus.png"), "iconbutton"))
                {
                    var collection = _asset.objectReferenceValue as ShaderVariantCollection;
                    AddVariant(collection, key.stringValue, value.stringValue);
                    unregisteredVariants.DeleteArrayElementAtIndex(index);
                }
            };
        }

        public void Draw()
        {
            _rlShaderAliases.DoLayoutList();
            _expand = DrawRegisteredShaderVariants(_expand, _asset, ref _editor);
            EditorGUILayout.PropertyField(_errorWhenFallback);
            if (0 < _rlUnregisteredVariants.serializedProperty.arraySize)
            {
                _rlUnregisteredVariants.DoLayoutList();
            }
        }

        private static void AddVariant(ShaderVariantCollection collection, string shaderName, string keywords)
        {
            if (collection == null) return;

            var shader = Shader.Find(shaderName);
            if (!shader) return;

            collection.Add(new ShaderVariantCollection.ShaderVariant
            {
                shader = shader,
                keywords = keywords.Split('|')
            });
            EditorUtility.SetDirty(collection);
        }

        private static bool DrawRegisteredShaderVariants(bool expand, SerializedProperty property, ref Editor editor)
        {
            var collection = property.objectReferenceValue as ShaderVariantCollection;
            if (collection == null) return expand;

            EditorGUILayout.Space();
            var r = EditorGUILayout.GetControlRect(false, 20);
            var rLabel = new Rect(r.x, r.y, r.width - 80, r.height);
            expand = EditorGUI.Foldout(rLabel, expand, "Registered Shader Variants");

            var rButton = new Rect(r.x + r.width - 80, r.y + 2, 80, r.height - 4);
            if (GUI.Button(rButton, "Clear All", EditorStyles.miniButton))
            {
                collection.Clear();
            }

            if (expand)
            {
                EditorGUILayout.BeginVertical("RL Background");
                Editor.CreateCachedEditor(collection, null, ref editor);
                editor.serializedObject.Update();
                var shaders = editor.serializedObject.FindProperty("m_Shaders");
                for (var i = 0; i < shaders.arraySize; i++)
                {
                    s_MiDrawShaderEntry.Invoke(editor, new object[] { i });
                }

                EditorGUILayout.EndVertical();
                editor.serializedObject.ApplyModifiedProperties();
            }

            return expand;
        }

        private static void ShowShaderDropdown(Rect rect, SerializedProperty property, string option, bool included)
        {
            var menu = new GenericMenu();
            var current = property.stringValue;
            var allShaderNames = ShaderUtil.GetAllShaderInfo()
                .Where(s => s.name.Contains(option) == included)
                .Select(s => s.name);

            foreach (var shaderName in allShaderNames)
            {
                menu.AddItem(new GUIContent(shaderName), shaderName == current, () =>
                {
                    property.stringValue = shaderName;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(rect);
        }
    }
#endif
}
