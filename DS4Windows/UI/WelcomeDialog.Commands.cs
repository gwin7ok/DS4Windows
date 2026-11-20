using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 初回の導入画面の操作のコマンド（規則 V3）。導入（ダウンロード・起動）は非同期で、
    /// <see cref="CommandGuard"/> が失敗のログと実行中の再実行の拒否を行う。
    /// </summary>
    public partial class WelcomeDialog
    {
        public ICommand InstallViGEmCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand InstallHidHideCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand InstallFakerInputCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand OpenXboxDriverPageCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand OpenBluetoothSettingsCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand FinishCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            InstallViGEmCommand = CommandGuard.Create(InstallViGEm, name: nameof(InstallViGEmCommand));
            InstallHidHideCommand = CommandGuard.Create(InstallHidHide, name: nameof(InstallHidHideCommand));
            InstallFakerInputCommand = CommandGuard.Create(InstallFakerInput, name: nameof(InstallFakerInputCommand));
            OpenXboxDriverPageCommand = CommandGuard.CreateSync(OpenXboxDriverPage, name: nameof(OpenXboxDriverPageCommand));
            OpenBluetoothSettingsCommand = CommandGuard.CreateSync(OpenBluetoothSettings, name: nameof(OpenBluetoothSettingsCommand));
            FinishCommand = CommandGuard.CreateSync(FinishDialog, name: nameof(FinishCommand));
        }
    }
}
