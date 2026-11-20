using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>自動プロファイルの対象のプログラムの検索（スタートメニュー・Steam・フォルダー）とショートカットの解決。</summary>
    public partial class AutoProfilesViewModel
    {
        private async Task RunSearchAsync(Func<Task> add)
        {
            IsBusy = true;
            try
            {
                await add().ConfigureAwait(true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task AddProgramsFromStartMenu()
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddFromStartMenu(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs");
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async Task AddProgramsFromSteam(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppsFromLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async Task AddProgramsFromDir(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppsFromLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async Task AddProgramExeLocation(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppExeLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        private void AddFromStartMenu(string path)
        {
            List<string> lnkpaths = new List<string>();
            lnkpaths.AddRange(Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories));
            lnkpaths.AddRange(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + "\\Programs", "*.lnk", SearchOption.AllDirectories));
            List<string> exepaths = new List<string>();
            foreach (string link in lnkpaths)
            {
                string? target = GetTargetPath(link);
                if (target != null) exepaths.Add(target);
            }

            ScanApps(exepaths);
        }

        private void ScanApps(List<string> exepaths, bool checkexisting = true,
            bool skipsetupapps = true)
        {
            foreach (string target in exepaths)
            {
                bool skip = !File.Exists(target) || Path.GetExtension(target).ToLower() != ".exe";
                skip = skip || (skipsetupapps && (target.Contains("etup") || target.Contains("dotnet") || target.Contains("SETUP")
                    || target.Contains("edist") || target.Contains("nstall") || string.IsNullOrEmpty(target)));
                skip = skip || (checkexisting && existingapps.Contains(target));
                if (!skip)
                {
                    ProgramItem item = new ProgramItem(target, UsingExpandedControllers);
                    /*if (autoProfileHolder.AutoProfileDict.TryGetValue(target, out AutoProfileEntity autoEntity))
                    {
                        item.MatchedAutoProfile = autoEntity;
                    }
                    */

                    programColl.Add(item);
                    existingapps.Add(target);
                }
            }
        }

        private string? GetTargetPath(string filePath)
        {
            string? targetPath = ResolveMsiShortcut(filePath);
            if (targetPath == null)
            {
                targetPath = ResolveShortcut(filePath);
            }

            return targetPath;
        }

        public string? ResolveShortcutAndArgument(string filePath)
        {
            Type? t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t!)!;   // COM のクラスは必ず生成できる
            string? result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = shortcut.TargetPath + " " + shortcut.Arguments;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }

        public string? ResolveMsiShortcut(string file)
        {
            StringBuilder product = new StringBuilder(NativeMethods2.MaxGuidLength + 1);
            StringBuilder feature = new StringBuilder(NativeMethods2.MaxFeatureLength + 1);
            StringBuilder component = new StringBuilder(NativeMethods2.MaxGuidLength + 1);

            NativeMethods2.MsiGetShortcutTarget(file, product, feature, component);

            int pathLength = NativeMethods2.MaxPathLength;
            StringBuilder path = new StringBuilder(pathLength);

            NativeMethods2.InstallState installState = NativeMethods2.MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
            if (installState == NativeMethods2.InstallState.Local)
            {
                return path.ToString();
            }
            else
            {
                return null;
            }
        }

        public string? ResolveShortcut(string filePath)
        {
            Type? t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t!)!;   // COM のクラスは必ず生成できる
            string? result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = shortcut.TargetPath;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }
    }
}
