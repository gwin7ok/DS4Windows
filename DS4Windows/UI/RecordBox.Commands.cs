using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// マクロ記録画面のボタン操作のコマンド（規則 V3）。失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// 記録中の生のキー・マウスの捕捉、ドラッグ並べ替え、フォーカスの処理は、座標・入力そのものを扱うため画面の部品に残す。
    /// </summary>
    public partial class RecordBox
    {
        public ICommand SaveCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand CancelCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand RecordCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand ClearStepsCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand ChangeLightCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand AddRumbleCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand LoadPresetCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand SavePresetCommand { get; private set; } = null!; // InitCommands で必ず設定する



        public ICommand AddMouseMoveCommand { get; private set; } = null!; // InitCommands で必ず設定する

        public ICommand AddWaitTimeCommand { get; private set; } = null!; // InitCommands で必ず設定する

        private void InitCommands()
        {
            SaveCommand = CommandGuard.CreateSync(SaveMacro, name: nameof(SaveCommand));
            CancelCommand = CommandGuard.CreateSync(CancelMacro, name: nameof(CancelCommand));
            RecordCommand = CommandGuard.CreateSync(ToggleRecording, name: nameof(RecordCommand));
            ClearStepsCommand = CommandGuard.CreateSync(ClearSteps, name: nameof(ClearStepsCommand));
            ChangeLightCommand = CommandGuard.CreateSync(AddLightbarStep, name: nameof(ChangeLightCommand));
            AddRumbleCommand = CommandGuard.CreateSync(AddRumbleStep, name: nameof(AddRumbleCommand));
            LoadPresetCommand = CommandGuard.CreateSync(LoadPreset, name: nameof(LoadPresetCommand));
            SavePresetCommand = CommandGuard.CreateSync(SavePreset, name: nameof(SavePresetCommand));
            AddMouseMoveCommand = CommandGuard.CreateSync(AddMouseMoveStep, name: nameof(AddMouseMoveCommand));
            AddWaitTimeCommand = CommandGuard.CreateSync(AddWaitStep, name: nameof(AddWaitTimeCommand));
        }
    }
}
