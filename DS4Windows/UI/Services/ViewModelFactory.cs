using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="IViewModelFactory"/> の窓口。ViewModel の生成を、用途別の 3 つのファクトリー
    /// （<see cref="ShellViewModelFactory"/>＝メイン画面の枠、<see cref="ProfileEditViewModelFactory"/>＝プロファイル編集、<see cref="ActionViewModelFactory"/>＝特殊アクション）へ委譲する。
    /// 画面側は従来どおりこの窓口だけを見る。
    /// </summary>
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly ShellViewModelFactory _shell;
        private readonly ProfileEditViewModelFactory _edit;
        private readonly ActionViewModelFactory _actions;

        public ViewModelFactory(ShellViewModelFactory shell, ProfileEditViewModelFactory edit, ActionViewModelFactory actions)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _edit = edit ?? throw new ArgumentNullException(nameof(edit));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public ChangelogViewModel CreateChangelogViewModel()
            => _shell.CreateChangelogViewModel();

        public RenameProfileViewModel CreateRenameProfileViewModel()
            => _shell.CreateRenameProfileViewModel();

        public PresetOptionViewModel CreatePresetOptionViewModel()
            => _edit.CreatePresetOptionViewModel();

        public FirstLauchUtilViewModel CreateFirstLaunchUtilViewModel(ControlServiceDeviceOptions deviceOptions)
            => _shell.CreateFirstLaunchUtilViewModel(deviceOptions);

        public UpdaterWindowViewModel CreateUpdaterWindowViewModel(string newVersion)
            => _shell.CreateUpdaterWindowViewModel(newVersion);

        public ControllerRegDeviceOptsViewModel CreateControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions deviceOptions)
            => _shell.CreateControllerRegDeviceOptsViewModel(deviceOptions);

        public TouchButtonUserControlViewModel CreateTouchButtonViewModel(int deviceIndex)
            => _edit.CreateTouchButtonViewModel(deviceIndex);

        public LanguagePackViewModel CreateLanguagePackViewModel()
            => _shell.CreateLanguagePackViewModel();

        public AxialStickControlViewModel CreateAxialStickControlViewModel(StickDeadZoneInfo deadZoneInfo)
            => _edit.CreateAxialStickControlViewModel(deadZoneInfo);

        public MappingListViewModel CreateMappingListViewModel(int deviceIndex, OutContType contType)
            => _edit.CreateMappingListViewModel(deviceIndex, contType);

        public LogViewModel CreateLogViewModel()
            => _shell.CreateLogViewModel();

        public TrayIconViewModel CreateTrayIconViewModel(ProfileList profileListHolder, System.Windows.Threading.Dispatcher dispatcher)
            => _shell.CreateTrayIconViewModel(profileListHolder, dispatcher);

        public BindingWindowViewModel CreateBindingWindowViewModel(int deviceIndex, DS4ControlSettings settings, int targetDevice)
            => _edit.CreateBindingWindowViewModel(deviceIndex, settings, targetDevice);

        public ControlActionEditorViewModel CreateControlActionEditorViewModel(int deviceIndex, ControlSettingsEditTarget target)
            => _edit.CreateControlActionEditorViewModel(deviceIndex, target);

        public MacroViewModel CreateMacroViewModel()
            => _actions.CreateMacroViewModel();

        public LaunchProgramViewModel CreateLaunchProgramViewModel()
            => _actions.CreateLaunchProgramViewModel();

        public LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList)
            => _actions.CreateLoadProfileViewModel(profileList);

        public PressKeyViewModel CreatePressKeyViewModel(int deviceIndex)
            => _edit.CreatePressKeyViewModel(deviceIndex);

        public SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex)
            => _actions.CreateSpecialActionViewModel(actionIndex);

        public CheckBatteryViewModel CreateCheckBatteryViewModel()
            => _actions.CreateCheckBatteryViewModel();

        public MultiActButtonViewModel CreateMultiActButtonViewModel()
            => _actions.CreateMultiActButtonViewModel();

        public FocusWindowViewModel CreateFocusWindowViewModel()
            => _actions.CreateFocusWindowViewModel();

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1)
            => _edit.CreateProfileSettingsViewModel(device, targetDevice);

        public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool repeatable = false, int targetDevice = -1)
            => _edit.CreateRecordBoxViewModel(device, controlSettings, repeatable, targetDevice);

        public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null)
            => _actions.CreateSpecialActEditorViewModel(device, action);

        public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device)
            => _actions.CreateSpecialActionsListViewModel(device);

        public SettingsViewModel CreateSettingsViewModel()
            => _shell.CreateSettingsViewModel();

        public ControllerListViewModel CreateControllerListViewModel(ProfileList profileListHolder)
            => _shell.CreateControllerListViewModel(profileListHolder);

        public CurrentOutDeviceViewModel CreateCurrentOutDeviceViewModel()
            => _shell.CreateCurrentOutDeviceViewModel();

        public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList)
            => _shell.CreateAutoProfilesViewModel(autoProfileHolder, profileList);
    }
}
