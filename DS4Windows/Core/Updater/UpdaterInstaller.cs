using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Updater
{
    // Installer helper invoked by UI flow. Attempts non-elevated move first;
    // on access denied it will relaunch the current EXE elevated with
    // --complete-install and --complete-install-target arguments and wait for result.
    public static class UpdaterInstaller
    {

        // Try to install; returns true on success.
        // If allowElevation is false, do not attempt to relaunch elevated and simply return false on permission errors.
        public static bool TryInstall(string sourcePath, string targetPath, bool allowElevation = true)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                {
                    AppLogger.LogError("[UpdaterInstall] TryInstall called with empty source/target");
                    return false;
                }

                var targetDir = Path.GetDirectoryName(targetPath);

                // Attempt non-elevated copy/move first. Ensure directory creation is part of
                // the non-elevated attempt so permission errors can fallthrough to elevation.
                try
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Attempting non-elevated file install: {sourcePath} -> {targetPath}");
                    if (!Directory.Exists(targetDir))
                    {
                        AppLogger.LogDebug($"[UpdaterInstall] Creating target directory: {targetDir}");
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(sourcePath, targetPath, true);
                    try { File.Delete(sourcePath); } catch { }
                    AppLogger.LogInfo($"[UpdaterInstall] Installed without elevation: {targetPath}");
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    AppLogger.LogWarn($"[UpdaterInstall] Non-elevated install failed due to permissions: {ex.Message}");
                    if (!allowElevation)
                    {
                        // Caller requested no elevation attempt
                        AppLogger.LogDebug("[UpdaterInstall] Caller requested no elevation; returning false");
                        return false;
                    }
                    // fallthrough to elevation attempt
                }

                // Build elevated start info
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string args = $"--complete-install \"{sourcePath}\" --complete-install-target \"{targetPath}\"";
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory
                };

                AppLogger.LogDebug($"[UpdaterInstall] Launching elevated install helper: {exePath} {args}");
                AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo: FileName={psi.FileName} Arguments={psi.Arguments} UseShellExecute={psi.UseShellExecute} Verb={psi.Verb} WorkingDirectory={psi.WorkingDirectory}");

                try
                {
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        AppLogger.LogWarn("[UpdaterInstall] Failed to start elevated process: null returned");
                        return false;
                    }

                    proc.WaitForExit();
                    int code = proc.ExitCode;
                    AppLogger.LogInfo($"[UpdaterInstall] Elevated installer exited with code {code}");
                    return code == 0;
                }
                catch (Win32Exception ex)
                {
                    // This is thrown when user cancels UAC or process cannot be started
                    AppLogger.LogError($"[UpdaterInstall] Elevation start failure: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UpdaterInstall] TryInstall failed: {ex.Message}");
                return false;
            }
        }

        // Try to install a directory (copy all files/subdirs under sourceDir into targetDir)
        public static bool TryInstallDirectory(string sourceDir, string targetDir, bool allowElevation = true)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
                {
                    AppLogger.LogError("[UpdaterInstall] TryInstallDirectory called with empty source/target");
                    return false;
                }

                if (!Directory.Exists(sourceDir))
                {
                    AppLogger.LogError($"[UpdaterInstall] Source directory missing: {sourceDir}");
                    return false;
                }

                // Attempt non-elevated directory install first. Directory creation is part of
                // the non-elevated attempt so permission errors allow falling through to elevation.
                try
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Attempting non-elevated directory install: {sourceDir} -> {targetDir}");
                    if (!Directory.Exists(targetDir))
                    {
                        AppLogger.LogDebug($"[UpdaterInstall] Creating target directory: {targetDir}");
                        Directory.CreateDirectory(targetDir);
                    }
                    // Copy recursively
                    CopyDirectoryRecursive(sourceDir, targetDir);
                    AppLogger.LogInfo($"[UpdaterInstall] Installed directory without elevation: {targetDir}");
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    AppLogger.LogWarn($"[UpdaterInstall] Non-elevated directory install failed due to permissions: {ex.Message}");
                    if (!allowElevation)
                    {
                        AppLogger.LogDebug("[UpdaterInstall] Caller requested no elevation; returning false from TryInstallDirectory");
                        return false;
                    }
                    // else fallthrough to elevation attempt
                }

                // Elevation: start elevated process to perform complete install
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                // Pass source directory and target directory as arguments
                string args = $"--complete-install \"{sourceDir}\" --complete-install-target \"{targetDir}\"";
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory
                };

                AppLogger.LogDebug($"[UpdaterInstall] Launching elevated directory installer: {exePath} {args}");
                AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo: FileName={psi.FileName} Arguments={psi.Arguments} UseShellExecute={psi.UseShellExecute} Verb={psi.Verb} WorkingDirectory={psi.WorkingDirectory}");

                try
                {
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        AppLogger.LogError("[UpdaterInstall] Failed to start elevated process (null)");
                        return false;
                    }

                    proc.WaitForExit();
                    int code = proc.ExitCode;
                    AppLogger.LogInfo($"[UpdaterInstall] Elevated installer exited with code {code}");
                    return code == 0;
                }
                catch (Win32Exception ex)
                {
                    AppLogger.LogError($"[UpdaterInstall] Elevation cancelled or failed: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UpdaterInstall] TryInstallDirectory failed: {ex.Message}");
                return false;
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            // Create all directories
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            // Copy all files
            foreach (var newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destinationDir), true);
            }
        }
    }
}
