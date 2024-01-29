using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Coffee.Internal
{
    /// <summary>
    /// Provides additional callbacks related to canvas and UI system.
    /// </summary>
    internal static class UIExtraCallbacks
    {
        private static bool s_IsInitializedAfterCanvasRebuild;
        private static readonly FastAction s_AfterCanvasRebuildAction = new FastAction();
        private static readonly FastAction s_LateAfterCanvasRebuildAction = new FastAction();
        private static readonly FastAction s_BeforeCanvasRebuildAction = new FastAction();

        static UIExtraCallbacks()
        {
            Canvas.willRenderCanvases += OnBeforeCanvasRebuild;
            Logging.LogMulticast(typeof(Canvas), "willRenderCanvases", message: "ctor");
        }

        /// <summary>
        /// Event that occurs after canvas rebuilds.
        /// </summary>
        public static event Action onLateAfterCanvasRebuild
        {
            add => s_LateAfterCanvasRebuildAction.Add(value);
            remove => s_LateAfterCanvasRebuildAction.Remove(value);
        }

        /// <summary>
        /// Event that occurs before canvas rebuilds.
        /// </summary>
        public static event Action onBeforeCanvasRebuild
        {
            add => s_BeforeCanvasRebuildAction.Add(value);
            remove => s_BeforeCanvasRebuildAction.Remove(value);
        }

        /// <summary>
        /// Event that occurs after canvas rebuilds.
        /// </summary>
        public static event Action onAfterCanvasRebuild
        {
            add => s_AfterCanvasRebuildAction.Add(value);
            remove => s_AfterCanvasRebuildAction.Remove(value);
        }

        /// <summary>
        /// Initializes the UIExtraCallbacks to ensure proper event handling.
        /// </summary>
        private static void InitializeAfterCanvasRebuild()
        {
            if (s_IsInitializedAfterCanvasRebuild) return;
            s_IsInitializedAfterCanvasRebuild = true;

            CanvasUpdateRegistry.IsRebuildingLayout();
            Canvas.willRenderCanvases += OnAfterCanvasRebuild;
            Logging.LogMulticast(typeof(Canvas), "willRenderCanvases",
                message: "InitializeAfterCanvasRebuild");
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void InitializeOnLoad()
        {
        }

        /// <summary>
        /// Callback method called before canvas rebuilds.
        /// </summary>
        private static void OnBeforeCanvasRebuild()
        {
            s_BeforeCanvasRebuildAction.Invoke();
            InitializeAfterCanvasRebuild();
        }

        /// <summary>
        /// Callback method called after canvas rebuilds.
        /// </summary>
        private static void OnAfterCanvasRebuild()
        {
            s_AfterCanvasRebuildAction.Invoke();
            s_LateAfterCanvasRebuildAction.Invoke();
        }
    }
}
