using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Tools.Mesh2PNG
{
    [InitializeOnLoad]
    internal static class Mesh2PNGUpdater
    {
        private const string RemotePackageJsonUrl =
            "https://raw.githubusercontent.com/M-A-L-bl-LLl/Mesh2PNG/main/package.json";

        private static UnityEditor.PackageManager.PackageInfo GetPackageInfo()
        {
            foreach (var p in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                if (p.name == "com.mesh2png") return p;
            return null;
        }

        private static string PackageRootPath => GetPackageInfo()?.resolvedPath;

        public static string   LatestVersion    { get; private set; }
        public static bool     IsChecking       { get; private set; }
        public static bool     IsInstalling     { get; private set; }
        public static string   CheckError       { get; private set; }
        public static DateTime CheckCompletedAt { get; private set; }

        public static string CurrentVersion
        {
            get
            {
                return GetPackageInfo()?.version ?? "0.0.0";
            }
        }

        public static bool UpdateAvailable =>
            !string.IsNullOrEmpty(LatestVersion) && IsNewer(LatestVersion, CurrentVersion);

        static Mesh2PNGUpdater()
        {
            EditorApplication.delayCall += CheckForUpdates;
        }

        public static void CheckForUpdates()
        {
            if (IsChecking) return;

            IsChecking    = true;
            LatestVersion = null;
            CheckError    = null;

            RepaintOpenWindows();

            var url     = RemotePackageJsonUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Cache-Control", "no-cache, no-store");
            request.SetRequestHeader("Pragma", "no-cache");
            var op      = request.SendWebRequest();
            op.completed += _ =>
            {
                IsChecking       = false;
                CheckCompletedAt = DateTime.UtcNow;

                if (request.result == UnityWebRequest.Result.Success)
                    LatestVersion = ParseVersion(request.downloadHandler.text);
                else
                    CheckError = request.error;

                request.Dispose();
                RepaintOpenWindows();
            };
        }

        public static void InstallLatest()
        {
            if (IsInstalling) return;

            var info = GetPackageInfo();
            if (info == null)
            {
                UnityEngine.Debug.LogError("[Mesh2PNG] Could not find package info.");
                return;
            }

            IsInstalling = true;
            RepaintOpenWindows();

            // Git packages installed via UPM must be updated through Client.Add
            if (info.source == UnityEditor.PackageManager.PackageSource.Git)
            {
                InstallViaUPM();
            }
            else
            {
                InstallViaGitPull(info.resolvedPath);
            }
        }

        private static void InstallViaUPM()
        {
            const string gitUrl = "https://github.com/M-A-L-bl-LLl/Mesh2PNG.git";
            var addRequest = UnityEditor.PackageManager.Client.Add(gitUrl);

            EditorApplication.update += Poll;
            void Poll()
            {
                if (!addRequest.IsCompleted) return;
                EditorApplication.update -= Poll;
                IsInstalling = false;

                if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    UnityEngine.Debug.Log("[Mesh2PNG] Updated successfully.");
                    LatestVersion = null;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[Mesh2PNG] Update failed: {addRequest.Error?.message}");
                }

                RepaintOpenWindows();
            }
        }

        private static void InstallViaGitPull(string root)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName               = "cmd.exe",
                Arguments              = $"/c git -C \"{root}\" pull",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };

            System.Threading.Tasks.Task.Run(() =>
            {
                string output = null, error = null;
                int exitCode  = -1;

                try
                {
                    using var process = Process.Start(startInfo);
                    var outTask = System.Threading.Tasks.Task.Run(() => process.StandardOutput.ReadToEnd());
                    var errTask = System.Threading.Tasks.Task.Run(() => process.StandardError.ReadToEnd());
                    process.WaitForExit();
                    output   = outTask.Result;
                    error    = errTask.Result;
                    exitCode = process.ExitCode;
                }
                catch (Exception e)
                {
                    error = e.Message;
                }

                EditorApplication.delayCall += () =>
                {
                    IsInstalling = false;

                    if (exitCode == 0)
                    {
                        UnityEngine.Debug.Log($"[Mesh2PNG] Updated successfully.\n{output}");
                        LatestVersion = null;
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[Mesh2PNG] Update failed.\n{error}");
                    }

                    RepaintOpenWindows();
                };
            });
        }

        internal static void RepaintOpenWindows()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<Mesh2PNGWindow>())
                w.Repaint();
        }

        private static string ParseVersion(string json)
        {
            const string key = "\"version\"";
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return null;

            var colonIdx = json.IndexOf(':', idx + key.Length);
            if (colonIdx < 0) return null;

            var start = json.IndexOf('"', colonIdx + 1);
            if (start < 0) return null;
            start++;

            var end = json.IndexOf('"', start);
            if (end < 0) return null;

            return json.Substring(start, end - start);
        }

        private static bool IsNewer(string latest, string current) =>
            Version.TryParse(latest,  out var v1) &&
            Version.TryParse(current, out var v2) &&
            v1 > v2;
    }
}
