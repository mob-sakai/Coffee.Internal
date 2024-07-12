using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
#endif

namespace Coffee.NanoMonitor
{
    [DisallowMultipleComponent]
    public sealed class NanoMonitor : MonoBehaviour
    {
        [SerializeField]
        private RectTransform m_Layout;

        [SerializeField]
        private GameObject m_OpenedObject;

        [SerializeField]
        private GameObject m_ClosedObject;

        [SerializeField]
        private Button m_OpenButton;

        [SerializeField]
        private Button m_CloseButton;

        [SerializeField]
        private Button m_PrevButton;

        [SerializeField]
        private Button m_NextButton;

        [Header("View")]
        [SerializeField]
        private MonitorUI m_Time;

        [SerializeField]
        private MonitorUI m_Fps;

        [SerializeField]
        private MonitorUI m_Gc;

        [SerializeField]
        private MonitorUI m_MonoUsage;

        [SerializeField]
        private MonitorUI m_UnityUsage;

        [SerializeField]
        private MonitorUI m_TargetFps;

        [SerializeField]
        private MonitorUI m_CustomUITemplate;

        private Image.OriginVertical _anchor;
        private CustomMonitorItem[] _customMonitorItems = new CustomMonitorItem[0];

        private double _elapsed;
        private double _fpsElapsed;
        private int _frames;
        private float _interval = 1f;
        private bool _isOpened = true;
        private MonitorUI _switchText;
        private int _width = 600;

        public static float gpuMemory => (Profiler.GetAllocatedMemoryForGraphicsDriver() >> 10) / 1024f;
        public static float unityUsed => (Profiler.GetTotalAllocatedMemoryLong() >> 10) / 1024f;
        public static float unityTotal => (Profiler.GetTotalReservedMemoryLong() >> 10) / 1024f;
        public static float monoUsed => (Profiler.GetMonoUsedSizeLong() >> 10) / 1024f;
        public static float monoTotal => (Profiler.GetMonoHeapSizeLong() >> 10) / 1024f;
        public int fps => (int)(_frames / _fpsElapsed);

        private void Start()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] Start");

            var top = _anchor == Image.OriginVertical.Top;
            if (m_Layout)
            {
                m_Layout.anchorMin = top ? new Vector2(0, 1) : new Vector2(0, 0);
                m_Layout.anchorMax = top ? new Vector2(1, 1) : new Vector2(1, 0);
                m_Layout.pivot = top ? new Vector2(0.5f, 1) : new Vector2(0.5f, 0);
            }

            if (TryGetComponent<CanvasScaler>(out var cs))
            {
                cs.referenceResolution = new Vector2(_width, 1080);
                cs.matchWidthOrHeight = 0;
            }

            if (m_CustomUITemplate)
            {
                m_CustomUITemplate.gameObject.SetActive(false);

                var parent = m_CustomUITemplate.transform.parent;
                foreach (var item in _customMonitorItems)
                {
                    item.ui = Instantiate(m_CustomUITemplate, parent);
                    item.ui.name = "CustomMonitorUI";
                    item.ui.gameObject.SetActive(true);
                }
            }

            m_CloseButton.transform.localScale =
                m_OpenButton.transform.localScale =
                    top ? Vector3.one : new Vector3(1, -1, 1);

            var canMove = 1 < SceneManager.sceneCountInBuildSettings;
            if (m_PrevButton)
            {
                m_PrevButton.gameObject.SetActive(canMove);
            }

            if (m_NextButton)
            {
                m_NextButton.gameObject.SetActive(canMove);
            }

            SetVisibleOpenObject(_isOpened);

            m_TargetFps.SetText("{0}", Application.targetFrameRate);

            Profiler.EndSample();
        }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            _fpsElapsed += Time.unscaledDeltaTime;
            if (_elapsed < _interval) return;

            Profiler.BeginSample("(NM)[NanoMonitor] Update");

            if (m_Time)
            {
                m_Time.SetText("Time:{0,3}", (int)Time.realtimeSinceStartup);
            }

            if (m_Fps)
            {
                m_Fps.SetText("FPS:{0,3}", fps);
            }

            if (m_Gc)
            {
                m_Gc.SetText("GC:{0,3}", GC.CollectionCount(0));
            }

            if (m_MonoUsage)
            {
                m_MonoUsage.SetText("Mono:{0,7:N3}/{1,7:N3}MB", monoUsed, monoTotal);
            }

            if (m_UnityUsage)
            {
                m_UnityUsage.SetText("Unity:{0,7:N3}/{1,7:N3}MB", unityUsed, unityTotal);
            }

            foreach (var item in _customMonitorItems)
            {
                item.UpdateText();
            }

            _frames = 0;
            _elapsed %= _interval;
            _fpsElapsed = 0;
            Profiler.EndSample();
        }


        public void SetVisibleOpenObject(bool isOpen)
        {
            Profiler.BeginSample("(NM)[NanoMonitor] Open");

            _isOpened = isOpen;
            _frames = 0;
            _elapsed = _interval;
            _fpsElapsed = 0;

            m_OpenedObject.SetActive(isOpen);
            m_ClosedObject.SetActive(!isOpen);

            Profiler.EndSample();
        }

        public void MoveScene(int add)
        {
            var count = SceneManager.sceneCountInBuildSettings;
            if (count <= 1) return;

            var current = SceneManager.GetActiveScene().buildIndex;
            var next = (current + add + count) % count;
            SceneManager.LoadScene(next);
        }

        public void Clean()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect(0);
        }

        public void ChangeTargetFps()
        {
            if (Application.targetFrameRate == 60)
            {
                Application.targetFrameRate = 30;
            }
            else if (Application.targetFrameRate == 30)
            {
                Application.targetFrameRate = 15;
            }
            else if (Application.targetFrameRate == 15)
            {
                Application.targetFrameRate = -1;
            }
            else
            {
                Application.targetFrameRate = 60;
            }

            m_TargetFps.SetText("{0}", Application.targetFrameRate);
        }

        public void SetUp(Image.OriginVertical anchor, float interval, CustomMonitorItem[] customs, int width)
        {
            _anchor = anchor;
            _interval = interval;
            _customMonitorItems = customs;
            _width = width;
        }
    }
}
