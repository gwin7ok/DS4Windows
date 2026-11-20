using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 画面の操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class RenameProfileWindow
    {
        public ICommand CancelBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand ConfirmBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            CancelBtnCommand = CommandGuard.CreateSync(CancelBtn, name: nameof(CancelBtnCommand));
            ConfirmBtnCommand = CommandGuard.CreateSync(ConfirmBtn, name: nameof(ConfirmBtnCommand));
        }
    }
}
