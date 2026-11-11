using System;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>ViewModel の生成のうち、メイン画面の枠（設定・機器一覧・出力スロット・トレイ・ログ・更新・自動プロファイルなど）を作る窓口。</summary>
    public interface IShellViewModelFactory
    {
        /// <summary>設定タブの ViewModel。メイン画面が 1 つ作って使う。</summary>
        SettingsViewModel CreateSettingsViewModel();
        /// <summary>機器一覧（コントローラー一覧タブ）の ViewModel。プロファイルの一覧は、メイン画面が持つものを渡す。</summary>
        ControllerListViewModel CreateControllerListViewModel(ProfileList profileListHolder);
        /// <summary>出力スロット管理画面の ViewModel。</summary>
        CurrentOutDeviceViewModel CreateCurrentOutDeviceViewModel();
        // 子画面（ダイアログ・部品）の ViewModel。画面は ViewModel を受け取り、画面を開く側がここから作って渡す。
        ChangelogViewModel CreateChangelogViewModel();
        RenameProfileViewModel CreateRenameProfileViewModel();
        FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions);
        UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion);
        ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions);
        LanguagePackViewModel CreateLanguagePackViewModel();
        LogViewModel CreateLogViewModel();
        TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher);
        AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList);
    }
}
