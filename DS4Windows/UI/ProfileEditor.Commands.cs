using System.Windows.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI
{
    /// <summary>
    /// 編集画面の操作のコマンド（規則 V3）。XAML は <c>Command="{Binding …, RelativeSource={RelativeSource AncestorType=UserControl}}"</c> で結ぶ。
    /// 失敗のログと実行中の再実行の拒否は <see cref="CommandGuard"/> が行う。
    /// </summary>
    public partial class ProfileEditor
    {
        public ICommand SaveCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand CancelCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand ApplyCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand PresetCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand WindowSizeResetCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand ResetAllControlsCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand NewActionCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand EditActionCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand DuplicateActionCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand RemoveActionCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand CommitEnabledActionsCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand ExportActionsCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand EditSelectedControlCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        public ICommand SortColumnCommand { get; private set; } = null!; // InitCommands（コンストラクターの先頭）で必ず設定する

        private void InitCommands()
        {
            SaveCommand = CommandGuard.CreateSync(() => SaveAndClose(), name: nameof(SaveCommand));
            CancelCommand = CommandGuard.CreateSync(() => CancelEdit(), name: nameof(CancelCommand));
            ApplyCommand = CommandGuard.CreateSync(() => ApplyEdit(), name: nameof(ApplyCommand));
            PresetCommand = CommandGuard.CreateSync(() => OpenPresetOptions(), name: nameof(PresetCommand));
            WindowSizeResetCommand = CommandGuard.CreateSync(() => ResetWindowSize(), name: nameof(WindowSizeResetCommand));
            ResetAllControlsCommand = CommandGuard.CreateSync(() => ResetAllControls(), name: nameof(ResetAllControlsCommand));
            NewActionCommand = CommandGuard.CreateSync(() => NewAction(), name: nameof(NewActionCommand));
            EditActionCommand = CommandGuard.CreateSync(() => EditAction(), name: nameof(EditActionCommand));
            DuplicateActionCommand = CommandGuard.CreateSync(() => DuplicateAction(), name: nameof(DuplicateActionCommand));
            RemoveActionCommand = CommandGuard.CreateSync(() => RemoveAction(), name: nameof(RemoveActionCommand));
            CommitEnabledActionsCommand = CommandGuard.CreateSync(() => CommitEnabledActions(), name: nameof(CommitEnabledActionsCommand));
            ExportActionsCommand = CommandGuard.CreateSync(() => ExportActions(), name: nameof(ExportActionsCommand));
            EditSelectedControlCommand = CommandGuard.CreateSync(() => EditSelectedControl(), name: nameof(EditSelectedControlCommand));
            SortColumnCommand = CommandGuard.CreateSync<string?>(column => SortByColumn(column), name: nameof(SortColumnCommand));
        }
    }
}
