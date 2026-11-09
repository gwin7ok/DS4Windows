using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Abstractions
{
    public interface IViewModelFactory
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
        SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null);
        AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList);
        /// <param name="device">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device);

        /// <summary>設定タブの ViewModel。メイン画面が 1 つ作って使う。</summary>
        SettingsViewModel CreateSettingsViewModel();

        /// <summary>機器一覧（コントローラー一覧タブ）の ViewModel。プロファイルの一覧は、メイン画面が持つものを渡す。</summary>
        ControllerListViewModel CreateControllerListViewModel(ProfileList profileListHolder);

        /// <summary>出力スロット管理画面の ViewModel。</summary>
        CurrentOutDeviceViewModel CreateCurrentOutDeviceViewModel();

        // 子画面（ダイアログ・部品）の ViewModel。画面は ViewModel を受け取り、画面を開く側がここから作って渡す。
        ChangelogViewModel CreateChangelogViewModel();
        RenameProfileViewModel CreateRenameProfileViewModel();
        PresetOptionViewModel CreatePresetOptionViewModel();
        FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions);
        UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion);
        ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions);
        TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex);
        LanguagePackViewModel CreateLanguagePackViewModel();
        AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo);
        MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType);
        LogViewModel CreateLogViewModel();
        TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher);
        BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice);

        // 割り当て編集画面（AssignmentEditor）の、種別ごとの編集用 ViewModel
        ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target);
        MacroViewModel CreateMacroViewModel();
        LaunchProgramViewModel CreateLaunchProgramViewModel();
        LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList);
        PressKeyViewModel CreatePressKeyViewModel(int deviceIndex);
        /// <param name="actionIndex">特別な操作の種別の番号（切断＝5、ジャイロの校正＝9）</param>
        SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex);
        CheckBatteryViewModel CreateCheckBatteryViewModel();
        MultiActButtonViewModel CreateMultiActButtonViewModel();
        FocusWindowViewModel CreateFocusWindowViewModel();
    }
}
