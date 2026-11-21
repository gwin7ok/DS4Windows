/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Win32;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Utilities
{
    [SuppressUnmanagedCodeSecurity]
    public partial class Util
    {
        // Common helper to normalize action names for comparison (Trim by default)
        public static string NormalizeActionName(string? name)
        {
            if (name == null) return string.Empty;
            return name.Trim();
        }

        /// <summary>
        /// Use admin check to determine if process can be launched normally (normal user)
        /// or launched through Windows Explorer to de-elevate a process
        /// </summary>
        /// <param name="path">Program path or URL</param>
        /// <param name="argument">Extra arguments to pass to the launching program</param>
        public static void StartProcessHelper(string path, string? arguments = null)
        {
            if (!AdminCheck.IsAdministrator())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(path);
                startInfo.UseShellExecute = true;
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                try
                {
                    using (Process? temp = Process.Start(startInfo))
                    {
                    }
                }
                catch (Exception caught) when (caught is System.ComponentModel.Win32Exception or System.InvalidOperationException or System.NotSupportedException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
            }
            else
            {
                StartProcessInExplorer(path, arguments);
            }
        }

        /// <summary>
        /// Launch process in Explorer to de-elevate the process if DS4Windows is running
        /// as under the Admin account
        /// </summary>
        /// <param name="path">Program path or URL</param>
        /// <param name="argument">Extra arguments to pass to the launching program</param>
        public static void StartProcessInExplorer(string path, string? arguments = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            // Need to place Path/URL in double quotes to allow equals sign to not be
            // interpreted as a delimiter
            if (string.IsNullOrEmpty(arguments))
            {
                startInfo.Arguments = $"\"{path}\"";
            }
            else
            {
                startInfo.Arguments = $"\"{path}\" \"{arguments}\"";
            }

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = true;
            try
            {
                using (Process? temp = Process.Start(startInfo)) { }
            }
            catch (Exception caught) when (caught is System.ComponentModel.Win32Exception or System.InvalidOperationException or System.NotSupportedException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
        }

        public static void LogAssistBackgroundTask(Task task)
        {
            task.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    AppLogger.Log(AppLogLevel.Error, t.Exception.ToString(), LogOptions.UserFacing);
                }
            });
        }

        public static string GetOSProductName()
        {
            string productName =
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "")?.ToString() ?? string.Empty;
            return productName;
        }

        public static string GetOSReleaseId()
        {
            string releaseId =
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "")?.ToString() ?? string.Empty;
            return releaseId;
        }

        public static bool IsNet8DesktopRuntimeAvailable()
        {
            bool result = false;
            string archString = Environment.Is64BitProcess ? "x64" : "x86";

            using (RegistryKey? subKey =
                Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\{archString}\sharedfx\Microsoft.WindowsDesktop.App"))
            {
                if (subKey != null)
                {
                    foreach (string valueName in subKey.GetValueNames())
                    {
                        if (!string.IsNullOrEmpty(valueName) && valueName.Contains("8."))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        // TODO(technical debt): 4.0.1 made AppThemeChoice.Default the light theme, so nothing calls this method now
        // (App.ChangeTheme used it to follow the Windows apps theme mode). Kept in case a "Follow Windows" choice is added;
        // delete it if that is not planned (see copilot-instructions 2.4).
        public static bool SystemAppsUsingDarkTheme()
        {
            bool result = false;
            if (int.TryParse(Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "0")?.ToString(), out int lightEnabled))
            {
                result = lightEnabled == 0;
            }

            return result;
        }
    }
}
