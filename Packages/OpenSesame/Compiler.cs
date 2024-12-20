using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if !UNITY_2021_2_OR_NEWER
using System.Linq;
using System.Text.RegularExpressions;
#endif

namespace Coffee.OpenSesame
{
    [Flags]
    public enum CompileOptions
    {
        None = 0,
        Release = 1 << 0,
        XmlDoc = 1 << 1,
        RefDll = 1 << 2
    }

    public static class Compiler
    {
#if UNITY_2021_1_OR_NEWER
        private const string k_PackageId = "OpenSesame.Net.Compilers.Toolset.4.0.1";
#else
        private const string k_PackageId = "OpenSesame.Net.Compilers.4.0.1";
#endif

        public static string GetResponseFilePath(string assemblyName)
        {
#if UNITY_2021_1_OR_NEWER
            var target = (int)EditorUserBuildSettings.activeBuildTarget;
            var output = Hash128.Parse("Library/ScriptAssemblies").ToString().Substring(0, 5);
            var buildType = "E";
            var dbg = CompilationPipeline.codeOptimization == CodeOptimization.Debug ? "Dbg" : "";
            return $"Library/Bee/artifacts/{target}{output}{buildType}{dbg}.dag/{assemblyName}.rsp";
#else
            return Directory.GetFiles("Temp", "UnityTempFile-*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetCreationTimeUtc)
                .FirstOrDefault(path =>
                {
                    var outline = File.ReadLines(path)
                        .FirstOrDefault(x => Regex.IsMatch(x, "^[-/]out:"));
                    if (string.IsNullOrEmpty(outline)) return false;

                    var outPath = outline.Substring(6, outline.Length - 7);
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

            var sdkRoot = Directory.GetDirectories(sdkCoreRoot)
                .OrderByDescending(x => Path.GetFileName(x) == "Sdk")
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
            using (var sw = new StreamWriter(dst, false, Encoding.UTF8))
            {
                foreach (var line in File.ReadLines(src))
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    var colon = line.IndexOf(':');
                    switch (0 < colon ? line.Substring(1, colon - 1) : line.Substring(1))
                    {
                        case "out":
                            sw.WriteLine($"-out:\"{outPath}\"");
                            break;
                        case "doc":
                        case "debug":
                        case "analyzer":
                        case "refout":
                        case "optimize":
                        case "optimize+":
                        case "optimize-":
                            break;
                        default:
                            sw.WriteLine(line);
                            break;
                    }
                }

                if ((options & CompileOptions.Release) != 0)
                {
                    sw.WriteLine("-optimize");
                }
                else
                {
                    sw.WriteLine("-debug:portable");
                }

                if ((options & CompileOptions.XmlDoc) != 0)
                {
                    sw.WriteLine($"-doc:\"{Path.ChangeExtension(outPath, ".xml")}\"");
                }

                if ((options & CompileOptions.RefDll) != 0)
                {
                    sw.WriteLine($"-refout:\"{Path.ChangeExtension(outPath, ".ref.dll")}\"");
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

            var tmpOutPath = Path.Combine("Temp", outPath);
            var tmpOutXmlPath = Path.ChangeExtension(tmpOutPath, ".xml");
            var tmpOutPdbPath = Path.ChangeExtension(tmpOutPath, ".pdb");
            var tmpOutRefPath = Path.ChangeExtension(tmpOutPath, ".ref.dll");

            Utils.DeleteFile(tmpOutPath);
            Utils.DeleteFile(tmpOutXmlPath);
            Utils.DeleteFile(tmpOutPdbPath);
            Utils.DeleteFile(tmpOutRefPath);

            var tmpOutDir = Path.GetDirectoryName(tmpOutPath);
            if (!string.IsNullOrEmpty(tmpOutDir) && !Directory.Exists(tmpOutDir))
            {
                Directory.CreateDirectory(tmpOutDir);
            }

            var modRsp = ModifyResponseFile(rsp, tmpOutPath, options);
            Utils.ExecuteCommand(runtime, $"{compilerInfo.path} /noconfig @{modRsp}");

            AssetDatabase.StartAssetEditing();
            Utils.CopyFileIfNeeded(tmpOutPath, outPath);
            Utils.CopyFileIfNeeded(tmpOutXmlPath, Path.ChangeExtension(outPath, ".xml"));
            Utils.CopyFileIfNeeded(tmpOutPdbPath, Path.ChangeExtension(outPath, ".pdb"));
            Utils.CopyFileIfNeeded(tmpOutRefPath, Path.ChangeExtension(outPath, ".ref.dll"));
            AssetDatabase.StopAssetEditing();
        }
    }
}
