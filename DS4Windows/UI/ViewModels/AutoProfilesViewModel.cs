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

#nullable enable

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
    public class AutoProfilesViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private object _colLockobj = new object();
        private ObservableCollection<ProgramItem> programColl;
        private AutoProfileHolder autoProfileHolder;
        private ProfileList profileList;
        private int selectedIndex = -1;
        private ProgramItem? selectedItem;
        private HashSet<string> existingapps;
        private readonly IAppSettingsService _appSettings;
        private readonly IAutoProfileService _autoProfileService;
        private readonly IRuntimeFeatureSwitch _runtimeFeatures;   // 旧 ControlService.CheckHidHidePresence の転送

        public ObservableCollection<ProgramItem> ProgramColl { get => programColl; }

        public AutoProfileHolder AutoProfileHolder { get => autoProfileHolder; }

        public int SelectedIndex { get => selectedIndex; set => selectedIndex = value; }
        public ProgramItem? SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                CurrentItemChange?.Invoke(this, value);
            }
        }

        public ProfileList ProfileList { get => profileList; }

        public delegate void CurrentItemChangeHandler(AutoProfilesViewModel sender, ProgramItem? item);
        public event CurrentItemChangeHandler? CurrentItemChange;

        public event EventHandler? SearchFinished;
        public delegate void AutoProfileStateHandler(AutoProfilesViewModel sender, bool state);
        public event AutoProfileStateHandler? AutoProfileSystemChange;

        public bool RevertDefaultProfileOnUnknown
        {
            get => _appSettings.AutoProfileRevertDefaultProfile;
            set => _appSettings.AutoProfileRevertDefaultProfile = value;
        }

        // 旧 ControlService.USING_MAX_CONTROLLERS（静的シム）を IEnvironmentService.UsingMaxControllers に置き換えた。
        private readonly IEnvironmentService _environmentService;

        public bool UsingExpandedControllers
        {
            get => _environmentService.UsingMaxControllers;
        }

        private List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>> displayProfileSwitchList =
            new List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>>()
            {
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.None, AutoProfileDisplayProfileSwitchChoices.None),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.Log, AutoProfileDisplayProfileSwitchChoices.Log),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.Notification, AutoProfileDisplayProfileSwitchChoices.Notification),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.AutoProfiles_LogAndNotification, AutoProfileDisplayProfileSwitchChoices.LogAndNotification),
            };

        public List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>> DisplayProfileSwitchList => displayProfileSwitchList;

        public AutoProfileDisplayProfileSwitchChoices ProfileSwitchChoice
        {
            get => _autoProfileService.AutoProfileSwitchNotifyChoice;
            set => _autoProfileService.AutoProfileSwitchNotifyChoice = value;
        }

        // 依存はすべて必須引数（Pure DI。決定1＝S9-A と同じ方針）。
        // 置換前は任意引数（= null）と、AppHost/Program.rootHub/Global のフォールバックだった。
        /// <summary>
        /// 一致した自動プロファイルが持つプロファイル名を、各コントローラーの選択位置（0 は none、1 からがプロファイル一覧の位置）へ反映する。
        /// 名前が空・(none) なら 0。一覧に無い名前のときは、位置を変えない。拡張のコントローラー（5〜8）は、使うときだけ。
        /// </summary>
        public void LoadSelectedIndexes(ProgramItem item)
        {
            if (item?.MatchedAutoProfile == null) return;

            int count = UsingExpandedControllers ? 8 : 4;
            for (int i = 0; i < count; i++)
            {
                string profileName = item.MatchedAutoProfile.ProfileNames[i];
                if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                {
                    ProfileEntity? found = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                    if (found != null)
                    {
                        SetSelectedIndex(item, i, profileList.ProfileListCol.IndexOf(found) + 1);
                    }
                }
                else
                {
                    SetSelectedIndex(item, i, 0);
                }
            }
        }

        private static void SetSelectedIndex(ProgramItem item, int controller, int index)
        {
            switch (controller)
            {
                case 0: item.SelectedIndexCon1 = index; break;
                case 1: item.SelectedIndexCon2 = index; break;
                case 2: item.SelectedIndexCon3 = index; break;
                case 3: item.SelectedIndexCon4 = index; break;
                case 4: item.SelectedIndexCon5 = index; break;
                case 5: item.SelectedIndexCon6 = index; break;
                case 6: item.SelectedIndexCon7 = index; break;
                case 7: item.SelectedIndexCon8 = index; break;
            }
        }
        public AutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList,
            IAppSettingsService appSettings,
            IAutoProfileService autoProfileService,
            IRuntimeFeatureSwitch runtimeFeatures,
            IEnvironmentService environmentService,
            DS4Windows.UI.Abstractions.IUiDispatcher uiDispatcher,
            DS4Windows.UI.Abstractions.IDialogService dialogs)
        {
            if (uiDispatcher == null) throw new ArgumentNullException(nameof(uiDispatcher));
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(autoProfileService);
            _autoProfileService = autoProfileService;
            ArgumentNullException.ThrowIfNull(runtimeFeatures);
            _runtimeFeatures = runtimeFeatures;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(dialogs);
            _dialogs = dialogs;
            programColl = new ObservableCollection<ProgramItem>();
            existingapps = new HashSet<string>();
            this.autoProfileHolder = autoProfileHolder;
            this.profileList = profileList;
            PopulateCurrentEntries();

            uiDispatcher.EnableCollectionSynchronization(programColl, _colLockobj);
            CreateCommands();
        }

        // ---- 画面の操作（コマンド）と、その状態 ----

        private readonly DS4Windows.UI.Abstractions.IDialogService _dialogs;
        private string? autoProfilesFile;
        private string? steamGamesDir;
        private bool _isBusy;
        private bool _steamMenuUsed;
        private bool _startMenuUsed;
        private bool _autoDebug;

        /// <summary>保存先のファイル、Steam のゲームのフォルダ（なければ null）を受け取る。画面の初期化のときに 1 回呼ぶ。</summary>
        public void Configure(string autoProfilesFile, string? steamGamesDir)
        {
            ArgumentNullException.ThrowIfNull(autoProfilesFile);
            this.autoProfilesFile = autoProfilesFile;
            this.steamGamesDir = steamGamesDir;
            OnPropertyChanged(nameof(ShowSteamMenu));
        }

        /// <summary>プログラムの検索中（一覧の更新と、画面全体の操作を止める）。</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsIdle));
                }
            }
        }

        public bool IsIdle => !_isBusy;

        /// <summary>「Steam のゲームを追加」を出すか。Steam のフォルダがなければ出さず、追加したあとも（一覧を絞り込むまで）出さない。</summary>
        public bool ShowSteamMenu => steamGamesDir != null && !_steamMenuUsed;

        /// <summary>「スタートメニューのプログラムを追加」を出すか。追加したあとは（一覧を絞り込むまで）出さない。</summary>
        public bool ShowStartMenu => !_startMenuUsed;

        /// <summary>自動プロファイルの切り替えの診断ログを出すか。</summary>
        public bool AutoDebug
        {
            get => _autoDebug;
            set => SetProperty(ref _autoDebug, value);
        }

        public AsyncRelayCommand? AddFromSteamCommand { get; private set; }

        public AsyncRelayCommand? AddFromStartMenuCommand { get; private set; }

        public AsyncRelayCommand? AddFromFolderCommand { get; private set; }

        public AsyncRelayCommand? AddProgramFileCommand { get; private set; }

        public RelayCommand? HideUncheckedCommand { get; private set; }

        public RelayCommand? RemoveAutoCommand { get; private set; }

        public RelayCommand? SaveAutoCommand { get; private set; }

        public RelayCommand? MoveUpCommand { get; private set; }

        public RelayCommand? MoveDownCommand { get; private set; }

        private void CreateCommands()
        {
            AddFromSteamCommand = CommandGuard.Create(async () =>
            {
                await RunSearchAsync(() => AddProgramsFromSteam(steamGamesDir!)).ConfigureAwait(true);   // Steam のメニューは、設定があるときだけ出る
                _steamMenuUsed = true;
                OnPropertyChanged(nameof(ShowSteamMenu));
            }, name: nameof(AddFromSteamCommand));
            AddFromStartMenuCommand = CommandGuard.Create(async () =>
            {
                await RunSearchAsync(AddProgramsFromStartMenu).ConfigureAwait(true);
                _startMenuUsed = true;
                OnPropertyChanged(nameof(ShowStartMenu));
            }, name: nameof(AddFromStartMenuCommand));
            AddFromFolderCommand = CommandGuard.Create(async () =>
            {
                string? dir = _dialogs.PickFolder();
                if (dir != null)
                {
                    await RunSearchAsync(() => AddProgramsFromDir(dir)).ConfigureAwait(true);
                }
            }, name: nameof(AddFromFolderCommand));
            AddProgramFileCommand = CommandGuard.Create(async () =>
            {
                string? file = _dialogs.PickOpenFile(new DS4Windows.UI.Abstractions.FileDialogOptions
                {
                    Title = "Select Program",
                    Filter = "Program (*.exe)|*.exe",
                    DefaultExtension = ".exe",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                });
                if (file != null)
                {
                    await RunSearchAsync(() => AddProgramExeLocation(file)).ConfigureAwait(true);
                }
            }, name: nameof(AddProgramFileCommand));
            HideUncheckedCommand = CommandGuard.CreateSync(() =>
            {
                IsBusy = true;   // 一覧の差し替え中は、画面が一覧を外す
                try
                {
                    RemoveUnchecked();
                }
                finally
                {
                    IsBusy = false;
                }
                _steamMenuUsed = false;
                _startMenuUsed = false;
                OnPropertyChanged(nameof(ShowSteamMenu));
                OnPropertyChanged(nameof(ShowStartMenu));
            }, name: nameof(HideUncheckedCommand));
            RemoveAutoCommand = CommandGuard.CreateSync(RemoveSelectedAuto, name: nameof(RemoveAutoCommand));
            SaveAutoCommand = CommandGuard.CreateSync(SaveSelectedAuto, name: nameof(SaveAutoCommand));
            MoveUpCommand = CommandGuard.CreateSync(() => MoveSelected(-1), name: nameof(MoveUpCommand));
            MoveDownCommand = CommandGuard.CreateSync(() => MoveSelected(1), name: nameof(MoveDownCommand));
        }

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

        private void RemoveSelectedAuto()
        {
            if (SelectedItem == null) return;
            AddExeToHIDHideWhenSaving(SelectedItem, false);
            RemoveAutoProfileEntry(SelectedItem);
            AutoProfileHolder.Save(autoProfilesFile!);   // Configure の後でだけ呼ばれる
            SelectedItem = null;
        }

        private void SaveSelectedAuto()
        {
            if (SelectedItem == null) return;
            if (SelectedItem.MatchedAutoProfile == null)
            {
                CreateAutoProfileEntry(SelectedItem);
            }
            else
            {
                PersistAutoProfileEntry(SelectedItem);
            }

            AddExeToHIDHideWhenSaving(SelectedItem, SelectedItem.Turnoff);
            AutoProfileHolder.Save(autoProfilesFile!);   // Configure の後でだけ呼ばれる
        }

        private void MoveSelected(int direction)
        {
            if (SelectedItem != null && MoveItemUpDown(SelectedItem, direction))
            {
                AutoProfileHolder.Save(autoProfilesFile!);   // Configure の後でだけ呼ばれる
            }
        }

        private void PopulateCurrentEntries()
        {
            foreach (AutoProfileEntity entry in autoProfileHolder.AutoProfileColl)
            {
                ProgramItem item = new ProgramItem(entry.Path, UsingExpandedControllers, entry);

                programColl.Add(item);
                existingapps.Add(entry.Path);
            }
        }

        public void RemoveUnchecked()
        {
            AutoProfileSystemChange?.Invoke(this, false);
            programColl.Clear();
            existingapps.Clear();
            PopulateCurrentEntries();
            AutoProfileSystemChange?.Invoke(this, true);
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

        private void AddAppsFromLocation(string path)
        {
            List<string> exepaths = new List<string>();
            exepaths.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
            ScanApps(exepaths);
        }

        private void AddAppExeLocation(string path)
        {
            List<string> exepaths = new List<string>();
            exepaths.Add(path);
            ScanApps(exepaths, checkexisting: false, skipsetupapps: false);
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

        public void CreateAutoProfileEntry(ProgramItem item)
        {
            if (item.MatchedAutoProfile == null)
            {
                AutoProfileEntity tempEntry = new AutoProfileEntity(item.Path, item.Title ?? string.Empty);
                tempEntry.Turnoff = item.Turnoff;
                int tempindex = item.SelectedIndexCon1;
                tempEntry.ProfileNames[0] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon2;
                tempEntry.ProfileNames[1] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon3;
                tempEntry.ProfileNames[2] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon4;
                tempEntry.ProfileNames[3] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                if (UsingExpandedControllers)
                {
                    tempindex = item.SelectedIndexCon5;
                    tempEntry.ProfileNames[4] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon6;
                    tempEntry.ProfileNames[5] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon7;
                    tempEntry.ProfileNames[6] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon8;
                    tempEntry.ProfileNames[7] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;
                }

                item.MatchedAutoProfile = tempEntry;
                autoProfileHolder.AutoProfileColl.Add(item.MatchedAutoProfile);
            }
        }

        public void PersistAutoProfileEntry(ProgramItem item)
        {
            if (item.MatchedAutoProfile != null)
            {
                AutoProfileEntity tempEntry = item.MatchedAutoProfile;
                int tempindex = item.SelectedIndexCon1;
                tempEntry.ProfileNames[0] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon2;
                tempEntry.ProfileNames[1] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon3;
                tempEntry.ProfileNames[2] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon4;
                tempEntry.ProfileNames[3] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                    AutoProfileEntity.NONE_STRING;

                if (UsingExpandedControllers)
                {
                    tempindex = item.SelectedIndexCon5;
                    tempEntry.ProfileNames[4] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon6;
                    tempEntry.ProfileNames[5] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon7;
                    tempEntry.ProfileNames[6] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon8;
                    tempEntry.ProfileNames[7] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name ?? AutoProfileEntity.NONE_STRING :
                        AutoProfileEntity.NONE_STRING;
                }
            }
        }

        public void RemoveAutoProfileEntry(ProgramItem item)
        {
            if (item.MatchedAutoProfile != null) autoProfileHolder.AutoProfileColl.Remove(item.MatchedAutoProfile);
            item.MatchedAutoProfile = null;
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

        public bool MoveItemUpDown(ProgramItem item, int moveDirection)
        {
            // Move autoprofile item up (-1) or down (1) both in listView (programColl) and in autoProfileHolder data structure (will be written into AutoProfiles.xml file)
            bool itemMoved = true;
            int oldIdx = programColl.IndexOf(item);

            if (moveDirection == -1 && oldIdx > 0 && oldIdx < autoProfileHolder.AutoProfileColl.Count)
            {
                programColl.Move(oldIdx, oldIdx - 1);
                autoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx - 1);
            }
            else if (moveDirection == 1 && oldIdx >= 0 && oldIdx < programColl.Count - 1 && oldIdx < autoProfileHolder.AutoProfileColl.Count - 1)
            {
                programColl.Move(oldIdx, oldIdx + 1);
                autoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx + 1);
            }
            else
                itemMoved = false;

            return itemMoved;
        }

        public void AddExeToHIDHideWhenSaving(ProgramItem autoProf, bool addExe)
        {
            if (autoProf.Path.Substring((autoProf.Path.Length) - 4, 4) == ".exe") //Filter out autoprofiles that do not lead to EXEs.
            {
                _runtimeFeatures.CheckHidHidePresence(autoProf.Path, autoProf.Filename, addExe);
            }
        }
    }
}