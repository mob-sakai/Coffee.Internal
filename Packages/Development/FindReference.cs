using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Coffee.Development
{
    internal static class FindReferencesByGitGrep
    {
        [MenuItem("Assets/Find References With Git-Grep %&R", false, 1500)]
        private static void Run()
        {
            // get guid of selected asset,
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("Selected asset is not found in the database.");
                return;
            }

            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    Arguments =
                        $"grep -I --name-only --recurse-submodules {guid} -- ':!*.meta' Assets/ Packages/ ProjectSettings/",
                    CreateNoWindow = true,
                    FileName = "git",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Application.dataPath + "/..",
                    UseShellExecute = false
                },
                EnableRaisingEvents = true,
            };

            p.Exited += (_, __) =>
            {
                var result = new StringBuilder();
                var outputs = p.StandardOutput.ReadToEnd().Split('\n');
                var count = outputs.Length - 1;
                if (count == 0)
                {
                    result.Append($"<b><color=green>No references</color></b> ");
                }
                else
                {
                    result.Append($"<b><color=orange>{count} references</color></b> ");
                }

                result.Append($"found for <b>{path}</b> ({guid})   ");
                Array.ForEach(outputs, r =>
                {
                    if (string.IsNullOrEmpty(r)) return;
                    result.AppendLine($"-> {r}");
                });
                Debug.Log(result);
            };

            p.Start();
        }
    }
}
