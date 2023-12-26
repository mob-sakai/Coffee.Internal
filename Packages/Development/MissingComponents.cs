using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Coffee.Development
{
    internal static class MissingComponents
    {
        /// <summary>
        /// Remove missing components from active scene.
        /// </summary>
        [MenuItem("Development/Remove Missing Components")]
        private static void RemoveMissingComponentsOnScenes()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                RemoveMissingComponents(go);
            }
        }

        private static void RemoveMissingComponents(GameObject go)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (0 < count)
            {
                Debug.Log($"{count} missing scripts in '{go}' is removed.");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

            foreach (Transform child in go.transform)
            {
                RemoveMissingComponents(child.gameObject);
            }
        }
    }
}
