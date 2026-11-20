using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 画面の操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class OutputSlotManagerControl
    {
        public ICommand PluginBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand UnplugBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand SlotChangeAcceptBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            PluginBtnCommand = CommandGuard.CreateSync(PluginBtn, name: nameof(PluginBtnCommand));
            UnplugBtnCommand = CommandGuard.CreateSync(UnplugBtn, name: nameof(UnplugBtnCommand));
            SlotChangeAcceptBtnCommand = CommandGuard.CreateSync(SlotChangeAcceptBtn, name: nameof(SlotChangeAcceptBtnCommand));
        }
    }
}
