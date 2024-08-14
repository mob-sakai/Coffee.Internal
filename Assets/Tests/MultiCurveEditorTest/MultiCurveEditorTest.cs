using UnityEditor;
using UnityEngine;

namespace Coffee.InternalEditor
{
    public class MultiCurveEditorTest : EditorWindow
    {
        [SerializeField] private AnimationCurve m_0 = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve m_1 = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve m_2 = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve m_3 = AnimationCurve.Linear(0, 0, 1, 1);

        private MultiCurveEditor _e;
        private SerializedObject _serializedObject;

        [MenuItem("Development/MultiCurveEditorTest", false, 50)]
        public static void ShowWindow()
        {
            GetWindow<MultiCurveEditorTest>("MultiCurveEditorTest");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _e = new MultiCurveEditor(
                _serializedObject.FindProperty("m_0"),
                _serializedObject.FindProperty("m_1"),
                _serializedObject.FindProperty("m_2"),
                _serializedObject.FindProperty("m_3"));
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            _e.Draw(EditorGUILayout.GetControlRect(false, 200));
            _serializedObject.ApplyModifiedProperties();
        }
    }
}
