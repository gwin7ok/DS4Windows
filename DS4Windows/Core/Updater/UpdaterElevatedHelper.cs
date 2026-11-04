using System;
using System.IO;
using DS4Windows;

namespace DS4WinWPF
{
    // Helper used when process is started elevated to complete file move/install
    public static class UpdaterElevatedHelper
    {

        // Return 0 on success, non-zero error codes otherwise
        public static int CompleteInstall(string sourcePath, string targetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                {
                    AppLogger.LogError("[UpdaterInstall] CompleteInstall called with empty paths");
                    return 4; // cannot_save_download / invalid args
                }

                if (Directory.Exists(sourcePath))
                {
                    // Copy directory recursively into targetPath (targetPath is destination directory)
                    try
                    {
                        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
                        CopyDirectoryRecursive(sourcePath, targetPath);
                        try { Directory.Delete(sourcePath, true); } catch { }
                        AppLogger.LogInfo($"[UpdaterInstall] CompleteInstall succeeded (dir): {sourcePath} -> {targetPath}");
                        return 0;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        AppLogger.LogError($"[UpdaterInstall] Unauthorized while completing directory install: {ex.Message}");
                        return 3; // admin_required
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"[UpdaterInstall] Failed to complete directory install: {ex.Message}");
                        return 5;
                    }
                }
                else if (File.Exists(sourcePath))
                {
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    // Use copy then delete to allow overwrite semantics
                    File.Copy(sourcePath, targetPath, true);
                    try
                    {
                        File.Delete(sourcePath);
                    }
                    catch (Exception ex)
                    {
                        // not fatal; log and continue
                        AppLogger.LogWarn($"[UpdaterInstall] Failed to delete temp file {sourcePath}: {ex.Message}");
                    }

                    AppLogger.LogInfo($"[UpdaterInstall] CompleteInstall succeeded: {sourcePath} -> {targetPath}");
                    return 0;
                }
                else
                {
                    AppLogger.LogError($"[UpdaterInstall] Source path not found: {sourcePath}");
                    return 2; // missing source
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.LogError($"[UpdaterInstall] Unauthorized while completing install {sourcePath} -> {targetPath}: {ex.Message}");
                return 3; // admin_required
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UpdaterInstall] Failed to complete install {sourcePath} -> {targetPath}: {ex.Message}");
                return 5; // replace_failed / general failure
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            // Create directories
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            // Copy files
            foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                var destPath = filePath.Replace(sourceDir, destinationDir);
                File.Copy(filePath, destPath, true);
            }
        }
    }
}
