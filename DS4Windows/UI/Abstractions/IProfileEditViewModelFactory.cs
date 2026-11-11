using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>ViewModel の生成のうち、プロファイル編集画面と、その子画面（割り当ての出力ピッカー・記録・軸のコントロールなど）を作る窓口。</summary>
    public interface IProfileEditViewModelFactory
    {
        /// <param name="device">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="targetDevice">
        /// Phase6-Step7b: ランブルテスト・ライトバーのプレビュー・校正などで使う実機のスロット番号。-1 は実機なし。
        /// </param>
        ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1);
        /// <param name="targetDevice">
        /// Phase6-Step7b: ライトバーのプレビューと記録中のタッチパッド Passthru で使う実機のスロット番号。-1 は実機なし。
        /// </param>
        RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool repeatable = false, int targetDevice = -1);
        PresetOptionViewModel CreatePresetOptionViewModel();
        TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex);
        AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo);
        MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType);
        BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice);
        // 割り当て編集画面（AssignmentEditor）の、種別ごとの編集用 ViewModel
        ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target);
        PressKeyViewModel CreatePressKeyViewModel(int deviceIndex);
    }
}
