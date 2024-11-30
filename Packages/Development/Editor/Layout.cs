using UnityEditor;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Coffee.Development
{
    internal static class Layout
    {
        [MenuItem("Development/Enable All LayoutGroups", false, -21)]
        private static void EnableAllLayoutGroups()
        {
            EnableAllLayoutGroups(true);
        }

        [MenuItem("Development/Disable All LayoutGroups", false, -20)]
        private static void DisableAllLayoutGroups()
        {
            EnableAllLayoutGroups(false);
        }

        private static void EnableAllLayoutGroups(bool enabled)
        {
            foreach (var l in Misc.FindObjectsOfType<LayoutGroup>())
            {
                l.enabled = enabled;
            }
        }
    }
}
