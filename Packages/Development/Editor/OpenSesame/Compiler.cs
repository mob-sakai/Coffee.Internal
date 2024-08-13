using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Coffee.OpenSesame
{
    [Flags]
    public enum CompileOptions
    {
        None = 0,
        Release = 1 << 0,
        XmlDoc = 1 << 1,
        RefDll = 1 << 2,
        UseAnalyzer = 1 << 3
    }

    public static class Compiler
    {
        private const string k_CompilerId = "OpenSesame.Net.Compilers.Toolset.4.0.1";
        private const RegexOptions k_RegexOpt = RegexOptions.Multiline | RegexOptions.Compiled;

        private static string GetDagName()
        {
            var target = (int)EditorUserBuildSettings.activeBuildTarget;
            var output = Hash128.Parse("Library/ScriptAssemblies").ToString().Substring(0, 5);
            var buildType = "E";
            var dbg = CompilationPipeline.codeOptimization == CodeOptimization.Debug ? "Dbg" : "";

            return $"{target}{output}{buildType}{dbg}";
        }

        private static void UpdateResponseFile(string src, string dst, string outPath, CompileOptions options)
        {
            var text = File.ReadAllText(src);
            text = Regex.Replace(text, "^[-/]out:.*$", $"-out:\"{outPath}\"", k_RegexOpt);
            text = Regex.Replace(text, "^[-/]additionalfile:.*$", "", k_RegexOpt);

            // Enable reference dll
            if ((options & CompileOptions.RefDll) != 0)
            {
                var refout = Path.ChangeExtension(outPath, ".ref.dll");
                text = Regex.Replace(text, "^[-/]refout:.*$", $"-refout:{refout}", k_RegexOpt);
            }
            else
            {
                text = Regex.Replace(text, "^[-/]refout:.*$", "", k_RegexOpt);
            }

            // Release build
            if ((options & CompileOptions.Release) != 0)
            {
                text = Regex.Replace(text, "^[-/]debug.*$", "", k_RegexOpt);
            }

            // Use Analyzer
            if ((options & CompileOptions.UseAnalyzer) == 0)
            {
                text = Regex.Replace(text, "^[-/]analyzer.*$", "", k_RegexOpt);
            }

            // Export xml doc
            if ((options & CompileOptions.XmlDoc) != 0)
            {
                text += $"{Environment.NewLine}-doc:\"{Path.ChangeExtension(outPath, ".xml")}\"";
            }

            File.WriteAllText(dst, text);
        }

        public static void Build(string assemblyName, string outPath, CompileOptions options)
        {
            var compilerInfo = PackageInfo.GetInstalledInfo(k_CompilerId);
            if (!compilerInfo.isValid)
            {
                Debug.LogError($"Compiler package '{k_CompilerId}' not found.");
                return;
            }

            var dotnet = Type.GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
                ?.GetField("DotNetMuxerPath", BindingFlags.Static | BindingFlags.Public)
                ?.GetValue(null)
                ?.ToString();
            if (string.IsNullOrEmpty(dotnet))
            {
                Debug.LogError("dotnet runtime is not found in UnityEditor.");
                return;
            }

            var responseFile = $"Library/Bee/artifacts/{GetDagName()}.dag/{assemblyName}.rsp";
            var modResponseFile = $"{responseFile}.mod";
            UpdateResponseFile(responseFile, modResponseFile, outPath, options);
            Utils.ExecuteCommand(dotnet, $"{compilerInfo.path} /noconfig @{modResponseFile}");

            // var p = new Process()
            // {
            //     StartInfo = new ProcessStartInfo()
            //     {
            //         Arguments = $"{compilerInfo.path} /noconfig @{modResponseFile}",
            //         CreateNoWindow = true,
            //         FileName = dotnet,
            //         RedirectStandardError = true,
            //         RedirectStandardOutput = true,
            //         WorkingDirectory = Path.GetFullPath(Application.dataPath + "/.."),
            //         UseShellExecute = false
            //     },
            //     EnableRaisingEvents = true
            // };
            //
            // Debug.Log($"<b>[OpenSesame] Start ({compilerInfo.packageId})</b>: {assemblyName} -> {outPath}");
            // p.Exited += (_, __) =>
            // {
            //     Debug.Log($"<b>[OpenSesame] Complete ({p.ExitCode})</b>: {p.StandardOutput.ReadToEnd()}\n" +
            //               $"{p.StandardError.ReadToEnd()}");
            //     if (p.ExitCode == 0)
            //     {
            //         AssetDatabase.Refresh();
            //     }
            // };
            // p.Start();
        }
    }
}
