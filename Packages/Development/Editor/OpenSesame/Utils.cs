using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Coffee.OpenSesame
{
    internal static class Utils
    {
        /// <summary>
        /// Install NuGet package.
        /// </summary>
        /// <param name="packageId">Package Id</param>
        /// <returns>Installed directory path</returns>
        public static string InstallNugetPackage(string packageId)
        {
            return InstallPackage(packageId, $"https://globalcdn.nuget.org/packages/{packageId.ToLower()}.nupkg");
        }

        /// <summary>
        /// Install package from url.
        /// </summary>
        /// <param name="packageId">Package Id</param>
        /// <param name="url">Package url</param>
        /// <returns>Installed directory path</returns>
        public static string InstallPackage(string packageId, string url)
        {
            var installPath = Path.Combine("Library", "InstalledPackages", packageId);
            if (Directory.Exists(installPath)) return installPath;

            try
            {
                Debug.Log($"Install package: {packageId}");
                EditorUtility.DisplayProgressBar("Package Installer", $"Download {packageId} from {url}", 0.5f);
                var downloadPath = DownloadFile(url);
                EditorUtility.DisplayProgressBar("Package Installer", $"Extract to {installPath}",
                    0.7f);
                ExtractArchive(downloadPath, installPath);
                Debug.Log($"Package '{packageId}' has been installed at {installPath}.");
            }
            catch
            {
                throw new Exception($"Package '{packageId}' installation failed.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (Directory.Exists(installPath))
            {
                return installPath;
            }

            throw new FileNotFoundException($"Package '{packageId}' is not found at {installPath}");
        }

        /// <summary>
        /// Download the file by specifying the URL.
        /// NOTE: In .Net Framework 3.5, TSL1.2 is not supported. So, download the file on command line instead.
        /// </summary>
        /// <param name="url">File url</param>
        /// <returns>Downloaded file path.</returns>
        private static string DownloadFile(string url)
        {
            var downloadPath = Path.Combine("Temp", "DownloadedPackages", Path.GetFileName(url));
            Debug.Log($"Download {url} to {downloadPath}");
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));

            // Clear cache.
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }

            // Disable SSL certificate verification
            var cb = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, downloadPath);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = cb;
            }

            return downloadPath;
        }

        // Extract archive file.
        private static void ExtractArchive(string archivePath, string extractTo)
        {
            Debug.Log($"Extract archive {archivePath} to {extractTo}");
            var args = GetExtractArchiveCommand(archivePath, extractTo, Application.platform);
            ExecuteCommand(args[0], args[1]);
        }

        private static string[] GetExtractArchiveCommand(string archivePath, string extractTo, RuntimePlatform platform)
        {
            var contentsPath = EditorApplication.applicationContentsPath;
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Directory.CreateDirectory(Path.GetDirectoryName(extractTo));
                    return new[]
                    {
                        Path.Combine(contentsPath, "Tools", "7z.exe"),
                        $"x {archivePath} -o{extractTo}"
                    };
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    if (archivePath.EndsWith("tar.gz"))
                    {
                        Directory.CreateDirectory(extractTo);
                        return new[] { "tar", $"-pzxf {archivePath} -C {extractTo}" };
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(extractTo));
                    return new[]
                    {
                        Path.Combine(contentsPath, "Tools", "7za"), $"x {archivePath} -o{extractTo}"
                    };
                default:
                    throw new NotSupportedException($"{Application.platform} is not supported");
            }
        }

        /// <summary>
        /// Execute command.
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="arguments">Arguments</param>
        public static (int code, string stdout) ExecuteCommand(string filename, string arguments)
        {
            Debug.Log($"Execute command: {filename} {arguments}");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetFullPath(Application.dataPath + "/.."),
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
