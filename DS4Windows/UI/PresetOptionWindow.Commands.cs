using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 画面の操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class PresetOptionWindow
    {
        public ICommand NoPresetBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand YesPresetBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand PresetCancelBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand PresetApplyBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            NoPresetBtnCommand = CommandGuard.CreateSync(NoPresetBtn, name: nameof(NoPresetBtnCommand));
            YesPresetBtnCommand = CommandGuard.CreateSync(YesPresetBtn, name: nameof(YesPresetBtnCommand));
            PresetCancelBtnCommand = CommandGuard.CreateSync(PresetCancelBtn, name: nameof(PresetCancelBtnCommand));
            PresetApplyBtnCommand = CommandGuard.CreateSync(PresetApplyBtn, name: nameof(PresetApplyBtnCommand));
        }
    }
}
