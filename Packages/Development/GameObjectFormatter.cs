using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Coffee.Development
{
    internal static class GameObjectFormatter
    {
        private static readonly Func<GameObject, bool>[] s_Formatters = new Func<GameObject, bool>[]
        {
            // TITLE/SUBTITLE
            go =>
            {
                if (go.name != "TITLE" && go.name != "SUBTITLE") return false;

                var text = go.GetComponent<Text>();
                if (text)
                {
                    text.fontSize = 26;
                    var rt = text.transform as RectTransform;
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 30);

                    if (go.name == "TITLE")
                    {
                        go.transform.parent.name = text.text;
                    }
                }

                return true;
            },

            // Selectable
            go =>
            {
                var selectable = go.GetComponent<Selectable>();
                if (!selectable) return false;

                go.name = selectable.GetType().Name;
                var text = selectable.GetComponentInChildren<Text>(true);
                if (text)
                {
                    go.name += $" - {text.text}";
                }

                return true;
            },

            // Mask
            go =>
            {
                var mask = go.GetComponent<Mask>();
                if (!mask) return false;

                go.name = mask.GetType().Name;
                return true;
            },

            // Controls
            go =>
            {
                if (!go.name.StartsWith("Controls")) return false;

                go.name = "Controls";
                var text = go.transform.Find("Label")?.GetComponent<Text>();
                if (text)
                {
                    go.name += $" - {text.text}";
                }

                foreach (var childText in go.GetComponentsInChildren<Text>(true))
                {
                    childText.fontSize = 20;
                    var outline = childText.GetComponent<Outline>() ?? childText.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(1, -1);
                    outline.effectColor = new Color(0, 0, 0, 0.5f);
                }

                return true;
            },

            // Label
            go =>
            {
                if (go.name.StartsWith("Label")) return false;

                var text = go.GetComponent<Text>();
                if (text)
                {
                    go.name = "Label";
                }

                return true;
            }
        };

        [MenuItem("Development/Format GameObject Names", false, 1)]
        private static void FormatGameObjectNames()
        {
            Object.FindObjectsOfType<GameObject>()
                .ToList()
                .ForEach(go => s_Formatters.FirstOrDefault(p => p.Invoke(go)));
        }
    }
}
