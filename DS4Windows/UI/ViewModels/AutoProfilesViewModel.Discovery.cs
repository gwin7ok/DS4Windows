using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DS4Windows.Core.Platform;

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
                string? target = ShortcutResolver.ResolveTarget(link);
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
    }
}
