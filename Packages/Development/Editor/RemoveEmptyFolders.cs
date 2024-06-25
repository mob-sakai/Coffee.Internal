using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Coffee.Development
{
    internal static class DeleteEmptyFolders
    {
        [MenuItem("Development/Delete Empty Folders", false, 1600)]
        private static void Run()
        {
            try
            {
                // Delete empty folders in the project with O(n).
                AssetDatabase.StartAssetEditing();
                var allAssetPaths = AssetDatabase.FindAssets("*")
                    .Select(x => AssetDatabase.GUIDToAssetPath(x))
                    .Where(x => !Regex.IsMatch(Path.GetFullPath(x), @"[/\\]Library[/\\]")) // Ignore Library folders.
                    .OrderBy(x => x) // Sort asset paths for efficient search.
                    .ToList();

                for (var i = allAssetPaths.Count - 1; 0 <= i; i--)
                {
                    // Not a folder.
                    var path = allAssetPaths[i];
                    if (!AssetDatabase.IsValidFolder(path)) continue;

                    // If the folder contains a file or a folder, it remains in the list.
                    if (0 < RemoveAllByStartsWith(allAssetPaths, path + "/", i + 1)) continue;

                    // If the folder is empty, it will be moved to the trash and removed from the list.
                    Debug.Log($"Empty folder will be moved to trash: {path}");
                    RemoveAtFast(allAssetPaths, i);
                    AssetDatabase.MoveAssetToTrash(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static int RemoveAllByStartsWith(IList<string> list, string str, int start)
        {
            var removed = 0;
            for (var i = list.Count - 1; start <= i; i--)
            {
                if (!list[i].StartsWith(str)) continue;

                removed++;
                RemoveAtFast(list, i);
            }

            return removed;
        }

        private static void RemoveAtFast(IList<string> list, int index)
        {
            var lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }
    }
}
