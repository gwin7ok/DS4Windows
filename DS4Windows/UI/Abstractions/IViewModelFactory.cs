using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

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
    }
}
