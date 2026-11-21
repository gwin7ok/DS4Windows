using System;
using System.IO;
using System.Collections.Generic;

namespace DS4Windows.Core.Utilities
{
    /// <summary>HidHide のクライアントの場所の検索。</summary>
    public partial class Util
    {
        /// <summary>
        /// Use HidHide MSI registry info to try to find HidHideClient
        /// install path. Return found path or string.Empty if path not found.
        /// Good enough for me.
        /// </summary>
        /// <returns></returns>
        public static string GetHidHideClientPath()
        {
            string result = string.Empty;
            // Try known HidHide installer GUIDs for InstallLocation. Some installers/register keys differ
            // across versions; check the known GUIDs in order.
            string[] hidHideInstallerGuids = new string[]
            {
                "{48DD38C8-443E-4474-A249-AB32389E08F6}", // original GUID
                "{01E0AB21-D1CC-42B4-9DFF-84FFE4F26DAF}"  // HidHide v1.5.230 (provided)
            };

            string installLocation = string.Empty;
            foreach (var guid in hidHideInstallerGuids)
            {
                installLocation = Microsoft.Win32.Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}", "InstallLocation", "")?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(installLocation))
                    break;
            }
            if (!string.IsNullOrEmpty(installLocation))
            {
                string[] testPaths = new string[]
                {
                    Path.Combine(installLocation, "HidHideClient.exe"),
                    Path.Combine(installLocation, "x64", "HidHideClient.exe"),
                    Path.Combine(installLocation, "x86", "HidHideClient.exe"),
                };

                foreach (string testPath in testPaths)
                {
                    if (File.Exists(testPath))
                    {
                        result = testPath;
                        break;
                    }
                }
            }

            // If we didn't find an InstallLocation via known GUIDs, try scanning the
            // Uninstall registry keys for any entry whose DisplayName contains "HidHide".
            if (string.IsNullOrEmpty(result))
            {
                string[] uninstallRoots = new string[]
                {
                    @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                    @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                };

                foreach (var root in uninstallRoots)
                {
                    try
                    {
                        using (var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root))
                        {
                            if (rk == null) continue;
                            foreach (var subName in rk.GetSubKeyNames())
                            {
                                try
                                {
                                    using (var sub = rk.OpenSubKey(subName))
                                    {
                                        if (sub == null) continue;
                                        var display = sub.GetValue("DisplayName") as string ?? string.Empty;
                                        if (display.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            // Prefer InstallLocation if present
                                            var loc = sub.GetValue("InstallLocation") as string ?? string.Empty;
                                            if (!string.IsNullOrEmpty(loc))
                                            {
                                                string[] paths = new string[]
                                                {
                                                    Path.Combine(loc, "HidHideClient.exe"),
                                                    Path.Combine(loc, "x64", "HidHideClient.exe"),
                                                    Path.Combine(loc, "x86", "HidHideClient.exe"),
                                                };

                                                foreach (var p in paths)
                                                {
                                                    if (File.Exists(p))
                                                    {
                                                        result = p;
                                                        break;
                                                    }
                                                }
                                            }

                                            // If still not found, try UninstallString directory
                                            if (string.IsNullOrEmpty(result))
                                            {
                                                var uninstallStr = sub.GetValue("UninstallString") as string ?? string.Empty;
                                                if (!string.IsNullOrEmpty(uninstallStr))
                                                {
                                                    // Clean quotes and parameters
                                                    try
                                                    {
                                                        string cleaned = uninstallStr.Trim();
                                                        if (cleaned.StartsWith("\"") && cleaned.IndexOf('\"', 1) > 0)
                                                        {
                                                            cleaned = cleaned.Substring(1, cleaned.IndexOf('\"', 1) - 1);
                                                        }
                                                        // If there are parameters, remove them
                                                        if (cleaned.Contains(" "))
                                                            cleaned = cleaned.Split(' ')[0];

                                                        var dir = Path.GetDirectoryName(cleaned) ?? string.Empty;
                                                        if (!string.IsNullOrEmpty(dir))
                                                        {
                                                            string[] tryDirs = new string[]
                                                            {
                                                                Path.Combine(dir, "HidHideClient.exe"),
                                                                Path.Combine(Directory.GetParent(dir)?.FullName ?? string.Empty, "HidHideClient.exe")
                                                            };

                                                            foreach (var p in tryDirs)
                                                            {
                                                                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                                                                {
                                                                    result = p;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (Exception caught) when (caught is System.Security.SecurityException or System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
                                                }
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(result)) break;
                                    }
                                }
                                catch (Exception caught) when (caught is System.Security.SecurityException or System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }

                                if (!string.IsNullOrEmpty(result)) break;
                            }
                        }
                    }
                    catch { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }

                    if (!string.IsNullOrEmpty(result)) break;
                }
            }

            // As a last resort, search Program Files directories for HidHideClient.exe
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    List<string> roots = new List<string>() { pf };
                    if (!string.IsNullOrEmpty(pf86) && !roots.Contains(pf86)) roots.Add(pf86);

                    foreach (var r in roots)
                    {
                        if (Directory.Exists(r))
                        {
                            foreach (var d in Directory.GetDirectories(r))
                            {
                                try
                                {
                                    if (d.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        var candidate = Path.Combine(d, "HidHideClient.exe");
                                        if (File.Exists(candidate))
                                        {
                                            result = candidate;
                                            break;
                                        }
                                        // check deeper x64/x86 subfolders
                                        var candidate64 = Path.Combine(d, "x64", "HidHideClient.exe");
                                        var candidate86 = Path.Combine(d, "x86", "HidHideClient.exe");
                                        if (File.Exists(candidate64)) { result = candidate64; break; }
                                        if (File.Exists(candidate86)) { result = candidate86; break; }
                                    }
                                }
                                catch (Exception caught) when (caught is System.Security.SecurityException or System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
                            }
                        }

                        if (!string.IsNullOrEmpty(result)) break;
                    }
                }
                catch { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
            }

            return result;
        }
    }
}
