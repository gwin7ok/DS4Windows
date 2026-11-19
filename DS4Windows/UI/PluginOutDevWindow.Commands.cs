#nullable enable
using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 画面の操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class PluginOutDevWindow
    {
        public ICommand AcceptBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand CancelBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            AcceptBtnCommand = CommandGuard.CreateSync(AcceptBtn, name: nameof(AcceptBtnCommand));
            CancelBtnCommand = CommandGuard.CreateSync(CancelBtn, name: nameof(CancelBtnCommand));
        }
    }
}
