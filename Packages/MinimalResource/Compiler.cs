using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
#if !UNITY_2021_2_OR_NEWER
using System.Linq;
#endif

namespace Coffee.MinimalResource
{
    public static class Compiler
    {
        public const string k_ResourceDir = "Packages/com.coffee.minimal-resource/R~";
        private static string s_CscPath;
        private static string s_NetstandardPath;
        private static string s_MscorlibPath;
        private static bool s_Initialized;

        public static string GetBuiltinDotNetRuntimePath()
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

        private static void Initialize()
        {
            if (s_Initialized) return;

            s_Initialized = true;
            var contentsDir = EditorApplication.applicationContentsPath;
            var resourcesDir = Path.Combine(contentsDir, "Resources");
            s_CscPath = FindFile("csc.dll",
                Path.Combine(contentsDir, "DotNetSdkRoslyn"),
                Path.Combine(contentsDir, "Tools", "Roslyn"),
                Path.Combine(resourcesDir, "Scripting", "DotNetSdk"),
                resourcesDir,
                contentsDir);
            s_NetstandardPath = FindFile("netstandard.dll",
                Path.Combine(contentsDir, "NetStandard"),
                Path.Combine(resourcesDir, "Scripting", "NetStandard"),
                resourcesDir);
            s_MscorlibPath = FindFile("mscorlib.dll",
                Path.Combine(contentsDir, "NetStandard"),
                Path.Combine(resourcesDir, "Scripting", "NetStandard"),
                resourcesDir);
        }

        private static string FindFile(string filename, params string[] dirs)
        {
#if UNITY_2021_2_OR_NEWER
            var options = new EnumerationOptions() { RecurseSubdirectories = true, IgnoreInaccessible = true };
#else
            var options = SearchOption.AllDirectories;
#endif
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir, filename, options))
                {
                    return file;
                }
            }

            return null;
        }

        public static string GetBuiltinCscPath()
        {
            Initialize();
            if (string.IsNullOrEmpty(s_CscPath)) throw new Exception("Builtin C# compiler (csc) not found.");

            return s_CscPath;
        }

        public static string FindLibForAlwaysLinkAssembly()
        {
            return typeof(AlwaysLinkAssemblyAttribute).Assembly.Location;
        }

        public static string FindLibForPreserveAttribute()
        {
            return typeof(PreserveAttribute).Assembly.Location;
        }

        public static string FindStandardLib()
        {
            Initialize();
            if (string.IsNullOrEmpty(s_NetstandardPath))
            {
                throw new Exception("Builtin 'netstandard.dll' not found.");
            }

            return s_NetstandardPath;
        }

        public static string FindMscorlib()
        {
            Initialize();
            if (string.IsNullOrEmpty(s_MscorlibPath))
            {
                throw new Exception("Builtin 'mscorlib.dll' not found.");
            }

            return s_MscorlibPath;
        }

        public static void Build(string outPath)
        {
            var runtime = GetBuiltinDotNetRuntimePath();
            var csc = GetBuiltinCscPath();
            var outDir = Path.GetDirectoryName(outPath);

            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }

            ExecuteCommand(runtime,
                $"{csc} @rsp -out:\"{Path.GetFullPath(outPath)}\""
                + $" -r:\"{FindLibForPreserveAttribute()}\""
                + $" -r:\"{FindLibForAlwaysLinkAssembly()}\""
                + $" -r:\"{FindStandardLib()}\""
                + $" -r:\"{FindMscorlib()}\""
                , k_ResourceDir);
        }

        /// <summary>
        /// Execute command.
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="dir">Working directory</param>
        public static (int code, string stdout) ExecuteCommand(
            string filename,
            string arguments,
            string dir = null)
        {
            dir = Path.GetFullPath(dir ?? Application.dataPath + "/..");
            Debug.Log($"Execute command: [{dir}] {filename} {arguments}");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = dir,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            // Don't consume 100% of CPU while waiting for process to exit
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                while (!p.HasExited)
                {
                    Thread.Sleep(100);
                }
            }
            else
            {
                p.WaitForExit();
            }

            if (p.ExitCode != 0)
            {
                var ex = new Exception(p.StandardError.ReadToEnd() + "\n\n" + p.StandardOutput.ReadToEnd());
                Debug.LogException(ex);
                throw ex;
            }

            return (p.ExitCode, p.StandardOutput.ReadToEnd());
        }
    }
}
