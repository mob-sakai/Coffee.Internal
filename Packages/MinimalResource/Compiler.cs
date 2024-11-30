using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if !UNITY_2021_2_OR_NEWER
using System.Linq;
#endif

namespace Coffee.MinimalResource
{
    public static class Compiler
    {
        public const string k_ResourceDir = "Packages/com.coffee.minimal-resource/R~";

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

        public static string GetBuiltinCscPath()
        {
            var cscPath = Path.Combine(EditorApplication.applicationContentsPath, "DotNetSdkRoslyn", "csc.dll");
            if (File.Exists(cscPath)) return cscPath;

            cscPath = Path.Combine(EditorApplication.applicationContentsPath, "Tools", "Roslyn", "csc.dll");
            if (File.Exists(cscPath)) return cscPath;

            throw new Exception("Builtin C# compiler (csc) not found.");
        }

        public static string FindFile(string filename, string dir)
        {
            foreach (var file in Directory.GetFiles(dir, filename, SearchOption.AllDirectories))
            {
                return file;
            }

            throw new Exception($"File not found: {filename}");
        }

        public static string FindCoreLib()
        {
            return FindFile("UnityEngine.CoreModule.dll",
                Path.Combine(EditorApplication.applicationContentsPath, "Managed/UnityEngine"));
        }

        public static string FindStandardLib()
        {
            return FindFile("netstandard.dll",
                Path.Combine(EditorApplication.applicationContentsPath, "NetStandard"));
        }

        public static string FindMscorlib()
        {
            return FindFile("mscorlib.dll",
                Path.Combine(EditorApplication.applicationContentsPath, "NetStandard"));
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
                $"{csc} @rsp -out:\"{Path.GetFullPath(outPath)}\" -r:\"{FindCoreLib()}\" -r:\"{FindStandardLib()}\" -r:\"{FindMscorlib()}\"",
                k_ResourceDir);
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
