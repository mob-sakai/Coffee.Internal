#if UGUI_ENABLE
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if TMP_ENABLE
using TMPro;
#endif

namespace Coffee.Development
{
    internal static class GameObjectFormatter
    {
        private static readonly Func<GameObject, bool>[] s_Formatters = new Func<GameObject, bool>[]
        {
            // TITLE
            // - Change parent name to "{TITLE}"
            go =>
            {
                if (go.name != "TITLE") return false;

                var label = GetLabel(go);
                if (!string.IsNullOrEmpty(label))
                {
                    go.transform.parent.name = label;
                }

                return true;
            },

            // Selectable
            // - Change name to "{SelectableType}" or "{SelectableType} - {LABEL}"
            go =>
            {
                var selectable = go.GetComponent<Selectable>();
                if (!selectable) return false;

                go.name = selectable.GetType().Name;

                var label = GetLabel(selectable.transform.Find("LABEL"));
                if (!string.IsNullOrEmpty(label))
                {
                    go.name += $" - {label}";
                }

                return true;
            },

            // Controls
            // - Change name to "Controls" or "Controls - {LABEL}"
            go =>
            {
                if (!go.name.StartsWith("Controls")) return false;

                go.name = "Controls";

                var label = GetLabel(go.transform.Find("LABEL"));
                if (!string.IsNullOrEmpty(label))
                {
                    go.name += $" - {label}";
                }

                return true;
            },

            // Label
            // - Change name to "LABEL"
            go =>
            {
                if (go.TryGetComponent<Text>(out var _))
                {
                    go.name = "LABEL";
                    return true;
                }

#if TMP_ENABLE
                if (go.TryGetComponent<TextMeshProUGUI>(out var _))
                {
                    go.name = "LABEL";
                    return true;
                }
#endif

                return false;
            },

            // Layout
            // - Change name to "{LayoutType}"
            go =>
            {
                if (go.transform.Find("TITLE")) return false;

                if (go.TryGetComponent<LayoutGroup>(out var layoutGroup))
                {
                    go.name = layoutGroup.GetType().Name;
                }

                return true;
            }
        };

        private static string GetLabel(Component c)
        {
            if (c == null) return null;

            return GetLabel(c.gameObject);
        }

        private static string GetLabel(GameObject go)
        {
            if (go == null) return null;

            if (go.TryGetComponent<Text>(out var text))
            {
                return text.text.Replace('\n', ' ');
            }

#if TMP_ENABLE
            if (go.TryGetComponent<TextMeshProUGUI>(out var tmp))
            {
                return tmp.text.Replace('\n', ' ');
            }
#endif
            return null;
        }

        [MenuItem("Development/Format GameObject Names In Scene", false, 1)]
        private static void FormatGameObjectNames()
        {
            Misc.FindObjectsOfType<GameObject>()
                .ToList()
                .ForEach(go => s_Formatters.FirstOrDefault(p => p.Invoke(go)));
        }
    }
}
#endif
