using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WrapMode = UnityEngine.WrapMode;

namespace Coffee.InternalEditor
{
    public class MultiCurveEditor : IDisposable
    {
        private static AnimationCurve s_DefaultCurve;
        private static GUIContent s_ResetContent = new GUIContent("", "Reset current channel");

        private static readonly MethodInfo s_MiMoveCurveToFront =
            typeof(CurveEditor).GetMethod("MoveCurveToFront", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Color[] s_Colors = new[]
        {
            new Color(1.0f, 0.5f, 0.5f),
            new Color(0.4f, 0.8f, 0.4f),
            new Color(0.6f, 0.6f, 1.0f),
            new Color(1.0f, 1.0f, 1.0f)
        };

        private readonly SerializedProperty[] _props;
        private CurveEditor _curveEditor;
        private int _currentIndex;

        public MultiCurveEditor(params SerializedProperty[] props)
        {
            _props = props;
        }

        public void Dispose()
        {
            if (_curveEditor != null)
            {
                _curveEditor.OnDisable();
                _curveEditor.OnDestroy();
                _curveEditor.curvesUpdated = null;
                _curveEditor = null;
                Undo.undoRedoPerformed -= Flush;
            }
        }

        public void Flush()
        {
            if (_curveEditor == null) return;
            foreach (var wrapper in _curveEditor.animationCurves)
            {
                wrapper.renderer.FlushCache();
            }
        }

        public void DrawChannelSelector(Rect pos, GUIContent[] labels = null, GUIStyle style = null)
        {
            InitializeIfNeeded();

            var wrappers = _curveEditor.animationCurves;
            var prevColor = GUI.color;
            pos.xMax -= pos.height;
            pos.width /= wrappers.Length;

            for (var i = 0; i < wrappers.Length; i++)
            {
                var wrapper = wrappers[i];
                var isCurrent = _currentIndex == i;
                var color = wrapper.color;
                color.a = isCurrent ? 1f : 0.4f;
                GUI.color = color;
                var label = labels != null && labels.Length > i ? labels[i] : new GUIContent($"CH {i}");
                if (GUI.Toggle(pos, isCurrent, label, style ?? "MiniButton") != isCurrent)
                {
                    SetCurrentIndex(i);
                }

                pos.x += pos.width;
            }

            pos.width = pos.height;
            GUI.color = prevColor;
            if (GUI.Button(pos, s_ResetContent, "SearchCancelButton"))
            {
                _props[_currentIndex].animationCurveValue = GetDefaultCurve();
                _curveEditor.animationCurves[_currentIndex].renderer.FlushCache();
            }
        }

        public void Draw(Rect pos)
        {
            InitializeIfNeeded();

            EditorGUI.LabelField(pos, GUIContent.none, "CurveEditorBackground");
            _curveEditor.rect = new Rect(pos.x + 15, pos.y, pos.width - 15, pos.height - 5);
            var wrappers = _curveEditor.animationCurves;
            for (var i = 0; i < wrappers.Length; i++)
            {
                var isCurrent = _currentIndex == i;
                wrappers[i].color.a = isCurrent ? 1.0f : 0.6f;
                wrappers[i].readOnly = !isCurrent;
                wrappers[i].curve.keys = _props[i].animationCurveValue.keys;
            }

            _curveEditor.OnGUI();
            ApplyChanges();
        }

        private void InitializeIfNeeded()
        {
            if (_curveEditor != null) return;

            var cws = _props.Select((prop, id) =>
            {
                var cw = new CurveWrapper
                {
                    id = id,
                    groupId = -1,
                    color = s_Colors[id],
                    wrapColorMultiplier = Color.clear,
                    hidden = false,
                    readOnly = false,
                    renderer = new NormalCurveRenderer(prop.animationCurveValue)
                };
                cw.renderer.SetCustomRange(0f, 1f);
                cw.renderer.SetWrap(WrapMode.Clamp, WrapMode.Clamp);
                return cw;
            }).ToArray();

            _curveEditor = new CurveEditor(new Rect(), cws, false)
            {
                margin = 2,
                settings = new CurveEditorSettings
                {
                    hRangeMin = 0,
                    hRangeMax = 1,
                    vRangeMin = 0,
                    vRangeMax = 1,
                    hRangeLocked = true,
                    vRangeLocked = true,
                    hSlider = false,
                    vSlider = false,
                    useFocusColors = false,
                    scaleWithWindow = false,
                    undoRedoSelection = true
                }
            };

            _curveEditor.curvesUpdated += () => ApplyChanges(true);
            Undo.undoRedoPerformed += Flush;
            SetCurrentIndex(0);
        }

        private void SetCurrentIndex(int index)
        {
            if (_curveEditor == null || index < 0 || _props.Length <= index) return;

            _currentIndex = index;
            s_MiMoveCurveToFront?.Invoke(_curveEditor, new object[] { index });
            _curveEditor.SelectNone();
            _curveEditor.animationCurves[index].selected = CurveWrapper.SelectionMode.Selected;
        }

        private static AnimationCurve GetDefaultCurve()
        {
            if (s_DefaultCurve != null) return s_DefaultCurve;

            s_DefaultCurve = AnimationCurve.Linear(0, 0, 1, 1);
            for (var i = 0; i < s_DefaultCurve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(s_DefaultCurve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(s_DefaultCurve, i, AnimationUtility.TangentMode.Linear);
            }

            return s_DefaultCurve;
        }

        private void ApplyChanges(bool force = false)
        {
            if (_curveEditor == null) return;

            var wrappers = _curveEditor.animationCurves;
            for (var i = 0; i < wrappers.Length; i++)
            {
                if (!wrappers[i].changed && !force) continue;
                wrappers[i].changed = false;
                _props[i].animationCurveValue = wrappers[i].curve;
            }
        }
    }
}
