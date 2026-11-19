#nullable enable
using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 画面の操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class UpdaterWindow
    {
        public ICommand SkipVersionBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand YesBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand NoBtnCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            SkipVersionBtnCommand = CommandGuard.CreateSync(SkipVersionBtn, name: nameof(SkipVersionBtnCommand));
            YesBtnCommand = CommandGuard.Create(YesBtn, name: nameof(YesBtnCommand));
            NoBtnCommand = CommandGuard.CreateSync(NoBtn, name: nameof(NoBtnCommand));
        }
    }
}
