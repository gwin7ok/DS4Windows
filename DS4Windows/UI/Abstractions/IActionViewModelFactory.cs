using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>ViewModel の生成のうち、特殊アクションの編集と、割り当て編集画面の種別ごとの ViewModelを作る窓口。</summary>
    public interface IActionViewModelFactory
    {
        SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null);
        /// <param name="device">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device);
        MacroViewModel CreateMacroViewModel();
        LaunchProgramViewModel CreateLaunchProgramViewModel();
        LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList);
        /// <param name="actionIndex">特別な操作の種別の番号（切断＝5、ジャイロの校正＝9）</param>
        SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex);
        CheckBatteryViewModel CreateCheckBatteryViewModel();
        MultiActButtonViewModel CreateMultiActButtonViewModel();
        FocusWindowViewModel CreateFocusWindowViewModel();
    }
}
