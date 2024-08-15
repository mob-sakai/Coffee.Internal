using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        EnableAnalyzer = 1 << 10,
    }

    public static class Compiler
    {
#if UNITY_2021_1_OR_NEWER
        private const string k_PackageId = "OpenSesame.Net.Compilers.Toolset.4.0.1";
#else
        private const string k_PackageId = "OpenSesame.Net.Compilers.4.0.1";
#endif
        private const RegexOptions k_RegexOpt = RegexOptions.Multiline | RegexOptions.Compiled;

        public static string GetResponseFilePath(string assemblyName)
        {
#if UNITY_2021_1_OR_NEWER
            var target = (int)EditorUserBuildSettings.activeBuildTarget;
            var output = Hash128.Parse("Library/ScriptAssemblies").ToString().Substring(0, 5);
            var buildType = "E";
            var dbg = CompilationPipeline.codeOptimization == CodeOptimization.Debug ? "Dbg" : "";
            return $"Library/Bee/artifacts/{target}{output}{buildType}{dbg}.dag/{assemblyName}.rsp";
#else
Debug.Log($"[GetRsp] Files: {Directory.GetFiles("Temp", "UnityTempFile-*", SearchOption.TopDirectoryOnly).Length}");
            return Directory.GetFiles("Temp", "UnityTempFile-*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetCreationTimeUtc)
                .FirstOrDefault(path =>
                {
                    var outline = File.ReadLines(path)
                        .FirstOrDefault(x => Regex.IsMatch(x, "^[-/]out:"));
                    if (string.IsNullOrEmpty(outline)) return false;

                    var outPath = outline.Substring(6, outline.Length - 7);
                    Debug.Log($"[GetRsp] check: {outline} -> {outPath}, {assemblyName}");
                    return Path.GetFileNameWithoutExtension(outPath) == assemblyName;
                });
#endif
        }

        internal static string GetBuiltinDotNetRuntimePath()
        {
#if UNITY_2021_2_OR_NEWER
            return Type.GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
                ?.GetField("DotNetMuxerPath", BindingFlags.Static | BindingFlags.Public)
                ?.GetValue(null)
                ?.ToString();
#else
            var sdkCoreRoot = Type.GetType("UnityEditor.Scripting.NetCoreProgram, UnityEditor")
                ?.GetMethod("GetNetCoreRoot", BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, Array.Empty<object>()) as string;
            if (string.IsNullOrEmpty(sdkCoreRoot)) return "";

            var sdkRoot = Directory.GetDirectories(sdkCoreRoot, "Sdk*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(sdkRoot)) return "";

            var ext = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
            return Path.Combine(sdkRoot, "dotnet", ext);
#endif
        }

        internal static string GetBuiltinMonoRuntimePath()
        {
            var sdkRoot = Type.GetType("UnityEditor.Utils.MonoInstallationFinder, UnityEditor")
                ?.GetMethod("GetMonoBleedingEdgeInstallation", BindingFlags.Static | BindingFlags.Public)
                ?.Invoke(null, Array.Empty<object>()) as string ?? "";

            var ext = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
            return Path.Combine(sdkRoot, "bin", "mono", ext);
        }

        private static string ModifyResponseFile(string src, string outPath, CompileOptions options)
        {
            var dst = Path.Combine(Path.GetDirectoryName(src), $"mod_{Path.GetFileName(src)}");
            var p = '-';
            using (var sw = new StreamWriter(dst, false, Encoding.UTF8))
            {
                foreach (var line in File.ReadLines(src))
                {
                    var colon = line.IndexOf(':');
                    switch (0 < colon ? line.Substring(1, colon - 1) : string.Empty)
                    {
                        case "out":
                            p = line[0];
                            sw.WriteLine($"{p}out:\"{outPath}\"");
                            break;
                        case "debug":
                            if ((options & CompileOptions.Release) == 0) sw.WriteLine(line);
                            break;
                        case "analyzer":
                            if ((options & CompileOptions.EnableAnalyzer) != 0) sw.WriteLine(line);
                            break;
                        case "additionalfile": // Skip
                            break;
                        default:
                            sw.WriteLine(line);
                            break;
                    }
                }

                if ((options & CompileOptions.XmlDoc) != 0)
                {
                    sw.WriteLine($"{p}doc:\"{Path.ChangeExtension(outPath, ".xml")}\"");
                }

                if ((options & CompileOptions.RefDll) != 0)
                {
                    sw.WriteLine($"{p}refout:\"{Path.ChangeExtension(outPath, ".ref.dll")}\"");
                }
            }

            return dst;
        }

        public static void Build(string assemblyName, string outPath, CompileOptions options)
        {
            var compilerInfo = PackageInfo.GetInstalledInfo(k_PackageId);
            if (!compilerInfo.isValid)
            {
                Debug.LogError($"Compiler package '{k_PackageId}' not found.");
                return;
            }

            var runtime = compilerInfo.isDotNet ? GetBuiltinDotNetRuntimePath() : GetBuiltinMonoRuntimePath();
            if (string.IsNullOrEmpty(runtime))
            {
                Debug.LogError($"Runtime for {k_PackageId} is not found in UnityEditor.");
                return;
            }

            var rsp = GetResponseFilePath(assemblyName);
            if (string.IsNullOrEmpty(rsp))
            {
                Debug.LogError($"Response file for {assemblyName} is not found.");
                return;
            }
            var modRsp = ModifyResponseFile(rsp, outPath, options);
            Utils.ExecuteCommand(runtime, $"{compilerInfo.path} /noconfig @{modRsp}");
        }
    }
}
