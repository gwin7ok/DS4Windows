using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI
{
    public partial class MainWindow
    {
        /// <summary>
        /// ViewModel の頼みで、メイン画面を親（Owner）にして子画面を開く。子画面の生成に必要な部品（画面の ViewModel の工場・設定）は、
        /// メイン画面が持っているものをそのまま使う。
        /// </summary>
        private sealed class MainWindowNavigator : IShellNavigator
        {
            private readonly MainWindow window;

            public MainWindowNavigator(MainWindow window)
            {
                this.window = window;
            }

            public void ShowAbout()
            {
                var aboutWin = new About(window.mainWinVM.CreateAboutViewModel()) { Owner = window };
                aboutWin.ShowDialog();
            }

            public void ShowChangelog()
            {
                var changelogWin = new ChangelogWindow(window._viewModelFactory.CreateChangelogViewModel());
                changelogWin.ShowDialog();
            }

            public void ShowDeviceOptions()
            {
                var optsWindow = new ControllerRegisterOptionsWindow(window.mainWinVM.CreateDeviceOptionsViewModel(window._viewModelFactory))
                {
                    Owner = window,
                };
                optsWindow.Show();
            }

            public void ShowUpdater(string version)
            {
                var updaterWin = new UpdaterWindow(version, window.mainWinVM.IsAdministrator, window._viewModelFactory.CreateUpdaterWindowViewModel(version));
                updaterWin.ShowDialog();
            }

            public void NewProfile() => window.NewProfile();

            public void EditProfile() => window.EditProfile();

            public void DeleteProfile() => window.DeleteProfile();

            public void RenameProfile() => window.RenameProfile();

            public void DuplicateProfile() => window.DuplicateProfile();

            public void ImportProfile() => window.ImportProfile();

            public void ExportProfile() => window.ExportProfile();

            public void EditDeviceProfile(int deviceIndex) => window.EditDeviceProfile(deviceIndex);

            public void NewDeviceProfile(int deviceIndex) => window.NewDeviceProfile(deviceIndex);

            public void SetHotkeysActive(bool active) => window.ChangeHotkeysStatus(active);

            public void SetRunStartPanelVisible(bool visible) => window.settingsWrapVM.ShowRunStartPanel = visible;

            public void ExportLog() => window.ExportLog();

            public void ClearLog() => window.ClearLog();

            public void RefreshDriverCheck() => window.settingsWrapVM.DriverCheckRefresh();

            public void ShowLogMessage() => window.ShowSelectedLogMessage();
        }
    }
}
