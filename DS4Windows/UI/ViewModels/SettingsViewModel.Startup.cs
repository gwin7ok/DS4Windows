using System;
using System.IO;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>起動時の実行・偽の exe 名・更新の確認の設定。</summary>
    public partial class SettingsViewModel
    {
        private bool runAtStartup;
        public bool RunAtStartup
        {
            get => runAtStartup;
            set
            {
                runAtStartup = value;
                OnPropertyChanged(nameof(RunAtStartup));
            }
        }
        private bool runStartProg;
        public bool RunStartProg
        {
            get => runStartProg;
            set
            {
                runStartProg = value;
                OnPropertyChanged(nameof(RunStartProg));
            }
        }
        private bool runStartTask;
        public bool RunStartTask
        {
            get => runStartTask;
            set
            {
                runStartTask = value;
                OnPropertyChanged(nameof(RunStartTask));
            }
        }
        private bool canWriteTask;
        public bool CanWriteTask { get => canWriteTask; }

        private bool showRunStartPanel;
        public bool ShowRunStartPanel
        {
            get => showRunStartPanel;
            set
            {
                if (showRunStartPanel == value) return;
                showRunStartPanel = value;
                OnPropertyChanged(nameof(ShowRunStartPanel));
            }
        }

        private bool viewEnabled = true;
        public bool ViewEnabled
        {
            get => viewEnabled;
            set
            {
                viewEnabled = value;
                OnPropertyChanged(nameof(ViewEnabled));
            }
        }
        public string FakeExeName
        {
            get => _appSettings.FakeExeName;
            set
            {
                string temp = _appSettings.FakeExeName;
                if (temp == value) return;
                _appSettings.FakeExeName = value;
                // 古いファイルの削除（Compare）を、新しいファイルの作成（Changed）より先に行う。
                // 作成が例外になっても、古いファイルが残らないようにする（以前は作成が先で、例外で削除が実行されなかった）。
                FakeExeNameChangeCompare?.Invoke(this, temp, value);
                OnPropertyChanged(nameof(FakeExeName));
            }
        }
        public event FakeExeNameChangeHandler FakeExeNameChangeCompare;
        public delegate void FakeExeNameChangeHandler(SettingsViewModel sender,
            string oldvalue, string newvalue);

        public bool HidHideClientFound
        {
            get
            {
                bool result = _environmentService.HidHideInstalled &&
                    !string.IsNullOrEmpty(DS4Windows.Core.Utilities.Util.GetHidHideClientPath());

                return result;
            }
        }
        public int ProcessPriorityIndex
        {
            get => _appSettings.ProcessPriority;
            set
            {
                _appSettings.ProcessPriority = value;
                OnPropertyChanged(nameof(ProcessPriorityIndex));
            }

        }

        /// <summary>
        /// 偽装 exe 名の古いファイル（<c>名前.exe</c>／<c>.runtimeconfig.json</c>／<c>.deps.json</c>）を削除する。
        /// 名前が空の場合は何もしない。
        /// </summary>
        public static void DeleteFakeExeFiles(string programDirectory, string fakeName)
        {
            if (string.IsNullOrEmpty(fakeName))
            {
                return;
            }

            string old_exefile = Path.Combine(programDirectory, $"{fakeName}.exe");
            string old_conf_file = Path.Combine(programDirectory, $"{fakeName}.runtimeconfig.json");
            string old_deps_file = Path.Combine(programDirectory, $"{fakeName}.deps.json");

            if (File.Exists(old_exefile))
            {
                File.Delete(old_exefile);
            }

            if (File.Exists(old_conf_file))
            {
                File.Delete(old_conf_file);
            }

            if (File.Exists(old_deps_file))
            {
                File.Delete(old_deps_file);
            }
        }

        private void SettingsViewModel_FakeExeNameChanged(object? sender, EventArgs e)
        {
            string temp = FakeExeName;
            if (!string.IsNullOrEmpty(temp))
            {
                CreateFakeExe(FakeExeName);
            }
        }

        private void SettingsViewModel_RunStartTaskChanged(object? sender, EventArgs e)
        {
            if (runStartTask)
            {
                _startupEntries.WriteTaskEntry(_pathService.ProgramDirectory,
                        System.IO.Path.GetFileName(_pathService.ExecutablePath));
            }
            else
            {
                _startupEntries.DeleteTaskEntry();
            }
        }

        private void SettingsViewModel_RunStartProgChanged(object? sender, EventArgs e)
        {
            if (runStartProg)
            {
                _startupEntries.WriteStartProgEntry(_pathService.ExecutablePath, _pathService.ProgramDirectory);
            }
            else
            {
                _startupEntries.DeleteStartProgEntry();
            }
        }

        private void SettingsViewModel_RunAtStartupChanged(object? sender, EventArgs e)
        {
            if (runAtStartup)
            {
                RunStartProg = true;
                RunStartTask = false;
            }
            else
            {
                _startupEntries.DeleteStartProgEntry();
                _startupEntries.DeleteTaskEntry();
            }
        }

        private void CheckStartupOptions()
        {
            bool lnkExists = File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\DS4Windows.lnk");
            if (lnkExists)
            {
                runAtStartup = true;
            }
            else
            {
                runAtStartup = false;
            }
        }

        private void CheckForNoUpdatesWhen()
        {
            // If disabled, ensure UI shows sensible units. 0 means "every startup",
            // so only treat -1 as the explicit disabled sentinel.
            if (!_appSettings.CheckUpdateStartupEnabled)
            {
                // keep unit in sync with persisted value when disabled
                checkEveryUnitIdx = _appSettings.CheckEveryUnit;
            }

            OnPropertyChanged(nameof(CheckForUpdates));
            OnPropertyChanged(nameof(CheckEvery));
            OnPropertyChanged(nameof(CheckEveryUnit));
        }

        /// <summary>
        /// 偽装名の exe と、その設定ファイル（<c>.runtimeconfig.json</c>／<c>.deps.json</c>）を作る。
        /// 設定ファイルの元は、<c>実行ファイル名.exe.runtimeconfig.json</c>（実在しない）ではなく、
        /// 拡張子を除いた <c>実行ファイル名.runtimeconfig.json</c>／<c>.deps.json</c>。元が無い場合は、コピーを省略する。
        /// </summary>
        public static void CreateFakeExeFiles(string programDirectory, string exeLocation, string fakeName)
        {
            string exeBase = Path.Combine(Path.GetDirectoryName(exeLocation) ?? string.Empty,
                Path.GetFileNameWithoutExtension(exeLocation));
            string current_conf_file_path = $"{exeBase}.runtimeconfig.json";
            string current_deps_file_path = $"{exeBase}.deps.json";

            string exefile = Path.Combine(programDirectory, $"{fakeName}.exe");
            string fake_conf_file = Path.Combine(programDirectory, $"{fakeName}.runtimeconfig.json");
            string fake_deps_file = Path.Combine(programDirectory, $"{fakeName}.deps.json");

            File.Copy(exeLocation, exefile, true); // Copy exe

            // Copy needed app config and deps files
            if (File.Exists(current_conf_file_path))
            {
                File.Copy(current_conf_file_path, fake_conf_file, true);
            }

            if (File.Exists(current_deps_file_path))
            {
                File.Copy(current_deps_file_path, fake_deps_file, true);
            }
        }
    }
}
